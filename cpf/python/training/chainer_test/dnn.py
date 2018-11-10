"""ディープラーニング関係ヘルパ、テンソルのCPU↔GPU変換処理など.

使用例）
	import dnn
	from dnn import chainer
	from dnn import F
	from dnn import L
	from dnn import Variable

	dnn.startup(-1) # 0番のGPU使用
	xp = dnn.xp # numpy または cupy

	def split(x, out):
		shape = x.shape
		x = x.reshape((2, shape[0], shape[1] // 2))
		return out[0](x[0]), out[1](x[1])

	def concat(x, out):
		h = F.concat((x[0], x[1]), axis=1)
		return out[0](h)

	m = dnn.Model(chainer.optimizers.Adam()) # とりあえず Model 作る
	g = m.gate(split, m.model_child('sub').dense(32, 32).relu().dense(32, 32).relu().owner) # サブモデル内に全結合層生成して relu 活性化関数セットしたものを入力し、それを２つに分ける Gate 作る
	g = m.gate(concat, g.dense(16, 16), g.dense(16, 16)) # ２つの入力を１つに結合する Gate 作る
	g = m.gate((lambda x, out: out[0](x * 2)), g.dense(32, 32)) # ２倍
	g.dense(32, 32)
	m.build()

	with open("dnn_test.dot", mode='w') as f:
		f.write(m.dot_code)
	with open("dnn_test_prediction.py", mode='w') as f:
		f.write(m.code)

	# 所定のデバイスメモリに転送
	m = dnn.to_xp(m)

	# とりあえず入力値を単純に10倍にするネットを目標とする
	for i in range(10):
		m.zerograds()
		x = Variable(xp.random.uniform(0, 1, (1, 32)).astype(xp.float32))
		y = m(x)
		t = x * 10
		loss = F.mean_squared_error(y, t)
		loss.backward()
		print(loss)
		m.optimizer.update()

	# 保存時はCPUメモリにしないとだめ
	m = dnn.to_cpu(m)
	dnn.save("modeldata", m)
"""
import types
import inspect
import numpy as np, chainer, chainer.functions as F, chainer.links as L
from chainer import Variable
from chainer import Chain
from chainer.link import Link

xp = None
cp = None
cuda = None
test = False


class FuncsHolder:
	"""関数を生成し保持する機能を提供するクラス.
	"""

	def __init__(self):
		self.funcs = []

	def relu(self):
		self.funcs.append(F.relu)
		return self

	def leaky_relu(self):
		self.funcs.append(F.leaky_relu)
		return self

	def sigmoid(self):
		self.funcs.append(F.sigmoid)
		return self

	def dropout(self, ratio=.5, **kwargs):

		def dropout(x):
			return F.dropout(x, ratio, **kwargs)

		self.funcs.append(dropout)
		return self

	def unpool2d(self, ksize, stride=None, pad=0, outsize=None, cover_all=True):

		def unpool2d(x):
			return F.unpooling_2d(x, ksize, stride, pad, outsize, cover_all)

		self.funcs.append(unpool2d)
		return self


class Node:
	"""モデルを構成するノード、レイヤを生成する機能を提供するクラス.

	Args:
		owner: このノードの所有者となる Node.
		node_generator: このノードが生成するノードの所有者となる Node.
		kind_name: 種類名.
	"""

	def __init__(self, owner, node_generator, kind_name):
		self.owner = owner # このノードの所有者となる Node.
		self.node_generator = node_generator # このノードが生成するノードの所有者となる Node.
		self.kind_name = kind_name # 種類名
		self.name = None # 所有者 Node 内での自身の名前
		self.dot_param = '' # グラフ内に表示するパラメータ文字列
		self.output_variable_ids = {} # 出力ノードをキーとしたローカル変数ID列、None がキーにする場合全てのレイヤーに優先する
		self.is_built = False # 既にビルドされたかどうか
		self.is_uner_build = False # ビルド途中かどうか

		# ルート Node の取得
		node = self
		while True:
			p = node.owner
			if p is None:
				break
			node = p
		self.root_node = node # ルート Node

	def __str__(self):
		return self.name

	def __repr__(self):
		return self.name

	def get_full_name(self):
		"""ルート Node からのフル名称を取得する.
		"""
		names = [self.name]
		node = self.owner
		while node is not None:
			nm = node.name
			if nm is not None and len(nm) != 0:
				names.append(nm)
			node = node.owner
		return '.'.join(reversed(names))

	def get_dot_node_name(self):
		"""dot 内でのノード名の取得.

		Returns:
			ノード名.
		"""
		return self.get_full_name().replace('.', '_')

	def _own_node(self, kind_name, node):
		"""指定 Node を所有する.

		Args:
			kind_name: 種類名.
			node: 所有する Node.

		Returns:
			指定された Node.
		"""
		return self.node_generator._own_node(kind_name, node)

	def model(self, kind_name, input=None):
		"""自分を入力とする出力 SubModel を生成する.

		Args:
			kind_name: 種類名.
			input: None.

		Returns:
			SubModel.
		"""
		return self._own_node(kind_name, SubModel(self.node_generator, kind_name, self))

	def layer(self, kind_name, link, input=None):
		"""自分を入力とする出力 Layer を生成する.

		Args:
			kind_name: 種類名.
			link: レイヤーの計算のメインとなる Link.
			input: None.

		Returns:
			Layer.
		"""
		return self._own_node(kind_name, Layer(self.node_generator, kind_name, link, self))

	def gate(self, func, *inputs):
		"""レイヤ同士を結合する Gate を作成する.

		Args:
			func: 入力値を出力レイヤーに通す処理、 def func(gate, x, output_layers).
			inputs: 入力レイヤー列.

		Returns:
			Gate.
		"""
		return self._own_node('Gate', Gate(self.node_generator, func, *inputs))

	def get_output_variable_id(self, node):
		"""このノードから指定ノードへの出力を一時的に保持するローカル変数IDを取得する.

		Args:
			node: 出力先 Node.

		Returns:
			ローカル変数ID.
		"""
		if None in self.output_variable_ids:
			return self.output_variable_ids[None]
		if node in self.output_variable_ids:
			return self.output_variable_ids[node]
		else:
			id = self.root_node.new_local_variable_id()
			self.output_variable_ids[node] = id
			return id

	def get_output_variable_name(self, node):
		"""このノードから指定ノードへの出力を一時的に保持するローカル変数名を取得する.

		Args:
			node: 出力先 Node.

		Returns:
			ローカル変数名.
		"""
		return self.root_node.get_local_variable_name(self.get_output_variable_id(node))

	def _build_begin(self):
		"""コード生成開始.

		Returns:
			ビルド開始成功したら True それ以外は False.
		"""
		if self.is_built:
			return False
		if self.is_uner_build:
			raise Exception()
		self.is_uner_build = True
		return True

	def _build_end(self):
		"""コード生成終了.
		"""
		self.is_built = True
		self.is_uner_build = False

	def build(self, depth):
		"""コード生成を行う.

		Args:
			depth: 関数呼び出し深さ.
		"""

	def add_ouput(self, node):
		"""出力ノードを追加する.

		Args:
			node: 出力 Node.

		Returns:
			入力 Node の set.
		"""

	def get_inputs(self):
		"""入力ノード集合を取得する.

		Returns:
			入力 Node の set.
		"""

	def set_inputs(self, inputs):
		"""入力ノード集合を設定する.

		Args:
			inputs: 入力 Node 集合
		"""

	def collect_gate(self, output, depth, gate_set):
		"""入力側の直近の複数出力 Gate を集める.

		Args:
			output: 呼び出し元出力側レイヤ.
			depth: 関数呼び出し深さ.
			gate_set: Gate 収集先 set.
		"""

	def dense(self, in_size, out_size=None, nobias=False, initialW=None, initial_bias=None):
		l = self.layer('dense', L.Linear(in_size, out_size, nobias, initialW, initial_bias))
		l.dot_param = '(in:{}, out:{})'.format(in_size, out_size)
		return l

	def conv2d(self, in_channels, out_channels, ksize=None, stride=1, pad=0, nobias=False, initialW=None, initial_bias=None, **kwargs):
		l = self.layer('conv2d', L.Convolution2D(in_channels, out_channels, ksize, stride, pad, nobias, initialW, initial_bias, **kwargs))
		l.dot_param = '(in:{}, out:{})'.format(in_channels, out_channels)
		return l

	def deconv2d(self, in_channels, out_channels, ksize=None, stride=1, pad=0, nobias=False, outsize=None, initialW=None, initial_bias=None, **kwargs):
		l = self.layer('deconv2d', L.Deconvolution2D(in_channels, out_channels, ksize, stride, pad, nobias, outsize, initialW, initial_bias, **kwargs))
		l.dot_param = '(in:{}, out:{})'.format(in_channels, out_channels)
		return l

	def batchnorm(self, size=None, decay=0.9, eps=2e-5, dtype=None, use_gamma=True, use_beta=True, initial_gamma=None, initial_beta=None, axis=None, initial_avg_mean=None, initial_avg_var=None):
		l = self.layer('batchnorm', L.BatchNormalization(size, decay, eps, dtype, use_gamma, use_beta, initial_gamma, initial_beta, axis, initial_avg_mean, initial_avg_var))
		l.dot_param = '(size:{})'.format(size)
		return l


class Layer(Chain, Node, FuncsHolder):
	"""レイヤ、学習対象の重み、活性化関数、入力レイヤへの参照を持つ.

	Args:
		owner: 所有者となる Node.
		kind_name: 種類名.
		link: 学習対象の重みを持つ chainer.links.Convolution2D など chainer.link.Link を継承するもの.
		input: 入力レイヤ、Layer を継承するものまたは None.
	"""

	def __init__(self, owner, kind_name, link, input=None):
		if input == owner:
			input = None

		if input is not None and not isinstance(input, Node):
			raise TypeError("Invalid argument. 'input' must be Node.")
		if not isinstance(link, Link):
			raise TypeError('Cannot register a non-link object as a child')

		Chain.__init__(self)
		Node.__init__(self, owner, owner, kind_name)
		FuncsHolder.__init__(self)

		if input is not None:
			input.add_ouput(self)

		self.input = input
		self.output = None
		with self.init_scope():
			self.link = link

	def __call__(self, x=None):
		"""指定値をレイヤーで変換する.

		Args:
			x: 入力値.

		Returns:
			変換後の値.
		"""
		x = self.link(x)
		for f in self.funcs:
			x = f(x)
		return x

	def build(self, depth):
		"""コード生成を行う.

		Args:
			depth: 関数呼び出し深さ.
		"""
		if not self._build_begin():
			return

		# 入力側を先にビルドと入力値を保持している変数ID取得
		if self.input is None:
			in_id = 0
		else:
			self.input.build(depth + 1)
			in_id = self.input.get_output_variable_id(self)
		in_name = self.root_node.get_local_variable_name(in_id)
		self.root_node.del_local_variable_id(in_id) # 名前取得済みなので削除

		# 出力値の代入先変数確保
		out_id = self.root_node.new_local_variable_id()
		out_name = self.root_node.get_local_variable_name(out_id)
		self.output_variable_ids[None] = out_id

		# コードの追加
		self_name = self.get_full_name()
		self.root_node.code.append('\t{} = self.{}({})'.format(out_name, self_name, in_name))

		# グラフ用コードの追加
		dot_id = self.get_dot_node_name()
		if len(self.funcs) != 0:
			self.root_node.dot_code.append('{} [label="{}{}\\n{}"];'.format(dot_id, self_name, self.dot_param, ', '.join([f.__name__ for f in self.funcs])))
		else:
			self.root_node.dot_code.append('{} [label="{}{}"];'.format(dot_id, self_name, self.dot_param))
		if self.input is not None:
			self.root_node.dot_code.append('{} -> {} [label="{}"];'.format(self.input.get_dot_node_name(), dot_id, in_name))

		self._build_end()

	def add_ouput(self, node):
		"""出力ノードを追加する.

		Args:
			node: 出力 Node.

		Returns:
			入力 Node の set.
		"""
		if self.output is not None:
			raise Exception('Layer can not have multiple outputs.')
		self.output = node

	def get_inputs(self):
		"""入力ノード集合を取得する.

		Returns:
			入力 Node の set.
		"""
		return set([self.input]) if self.input is not None else set()

	def set_inputs(self, inputs):
		"""入力ノード集合を設定する.

		Args:
			inputs: 入力 Node 集合
		"""
		self.input = None
		if inputs is not None:
			for i in inputs:
				self.input = i
				break

	def collect_gate(self, output, depth, gate_set):
		"""入力側の直近の複数出力 Gate を集める.

		Args:
			output: 呼び出し元出力側レイヤ.
			depth: 関数呼び出し深さ.
			gate_set: Gate 収集先 set.
		"""
		if self.input is None:
			return
		self.input.collect_gate(self, depth + 1, gate_set)


class Gate(Node):
	"""指定値を１～複数の出力レイヤーで変換する、複数対複数のレイヤーをくっつける役割を持つ.

	Args:
		owner: 所有者となる Node.
		func: 引数を入力レイヤーを通しさらに出力レイヤーを通す処理、 def func(x, output_layers).
		inputs: 入力レイヤー列.
	"""

	def __init__(self, owner, func, *inputs):
		for i in inputs:
			if not isinstance(i, Node):
				raise TypeError("Invalid argument. 'inputs' element must be Node.")

		Node.__init__(self, owner, owner, 'Gate')

		for input in inputs:
			input.add_ouput(self)

		self.inputs = inputs
		self.func = func
		self.outputs = []

	def __call__(self, x):
		"""指定値をユーザー指定処理で変換する.

		Args:
			x: 入力値.

		Returns:
			変換後の値.
		"""
		return self.func(x, self.outputs)

	def build(self, depth):
		"""コード生成を行う.

		Args:
			depth: 関数呼び出し深さ.
		"""
		if not self._build_begin():
			return

		if len(self.outputs) == 0:
			raise Exception('Gate must have one or more outputs.')

		# 入力側にある複数出力の Gate を収集し、呼び出しが早い方からビルドしていく
		depth += 1

		gate_set = set()
		for i in self.inputs:
			i.collect_gate(self, depth, gate_set)
		gate_list = [g for g in gate_set]
		gate_list.sort(key=lambda g: g[1])
		for g in reversed(gate_list):
			g[0].build(g[1])

		# 入力側を先にビルドと入力値を保持している変数ID取得
		in_ids = []
		for i in self.inputs:
			i.build(depth)
			in_ids.append(i.get_output_variable_id(self))

		# 出力値の代入先変数確保
		out_ids = [self.get_output_variable_id(o) for o in self.outputs]

		# コードの追加
		self_name = self.get_full_name()
		in_names = ', '.join([self.root_node.get_local_variable_name(id) for id in in_ids])
		out_names = ', '.join([self.root_node.get_local_variable_name(id) for id in out_ids])
		self.root_node.code.append('\t{} = self.{}(({}))'.format(out_names, self_name, in_names))

		# グラフ用コードの追加
		dot_id = self.get_dot_node_name()
		self.root_node.dot_code.append('{} [label="{}\\n{}"];'.format(dot_id, self_name, self.func.__name__))
		for i in self.inputs:
			self.root_node.dot_code.append('{} -> {} [label="{}"];'.format(i.get_dot_node_name(), dot_id, i.get_output_variable_name(self)))

		# func_code = inspect.getsource(self.func)
		# func_code = func_code.replace('\n', '\\n')
		# func_code = func_code.replace('\t', '\\t')
		# self.root_node.dot_code.append('{} [label="{}"];'.format(dot_id, func_code))

		for id in in_ids:
			self.root_node.del_local_variable_id(id) # 名前取得済みなので削除

		self._build_end()

	def add_ouput(self, node):
		"""出力ノードを追加する.

		Args:
			node: 出力 Node.

		Returns:
			入力 Node の set.
		"""
		if node not in self.outputs:
			self.outputs.append(node)

	def get_inputs(self):
		"""入力ノード集合を取得する.

		Returns:
			入力 Node の set.
		"""
		return set(self.inputs)

	def set_inputs(self, inputs):
		"""入力ノード集合を設定する.

		Args:
			inputs: 入力 Node 集合
		"""
		if inputs is not None:
			self.inputs = list(inputs)
		else:
			self.inputs = []

	def collect_gate(self, output, depth, gate_set):
		"""入力側の直近の複数出力 Gate を集める.

		Args:
			output: 呼び出し元出力側レイヤ.
			depth: 関数呼び出し深さ.
			gate_set: Gate 収集先 set.
		"""
		if 2 <= len(self.outputs):
			gate_set.add((self, depth))


class SubModel(Chain, Node):
	"""モデルのサブセット、layer()、gate() によりレイヤを生成し保持する.

	Args:
		owner: 親となる Model.
		kind_name: 種類名.
		input: 入力 Node または None.
	"""

	def __init__(self, owner, kind_name, input=None):
		if input == owner:
			input = None

		if input is not None and not isinstance(input, Node):
			raise TypeError("Invalid argument. 'input' must be Node.")

		Chain.__init__(self)
		Node.__init__(self, owner, self, kind_name)

		if input is not None:
			input.add_ouput(self)

		self.input = input
		self.output = None
		self.nodes = []
		self.kindwise_count = {}

	def _own_node(self, kind_name, node):
		"""指定 Node を所有する.

		Args:
			kind_name: 種類名.
			node: 所有する Node.

		Returns:
			指定された Node.
		"""
		if kind_name in self.kindwise_count:
			count = self.kindwise_count[kind_name]
			count += 1
		else:
			count = 1

		name = kind_name + str(count)
		if isinstance(node, Link):
			self.add_link(name, node)
		else:
			setattr(self, name, node)
			node.name = name
		self.nodes.append(node)
		self.kindwise_count[kind_name] = count

		return node

	def get_dot_node_name(self):
		"""dot 内でのノード名の取得.

		Returns:
			ノード名.
		"""
		return self.output.get_dot_node_name()

	def model(self, kind_name, input=None):
		"""自分を入力とする出力 SubModel を生成する.

		Args:
			kind_name: 種類名.
			input: 入力 Node.

		Returns:
			SubModel.
		"""
		if self.owner is None:
			return self.model_child(kind_name, input)
		else:
			return self.owner._own_node(kind_name, SubModel(self.owner, kind_name, self))

	def model_child(self, kind_name, input=None):
		"""子 SubModel を生成する.

		Args:
			kind_name: 種類名.
			input: 入力 Node.

		Returns:
			SubModel.
		"""
		return self._own_node(kind_name, SubModel(self.node_generator, kind_name, input))

	def get_output_variable_id(self, node):
		"""このノードから指定ノードへの出力を一時的に保持するローカル変数IDを取得する.

		Args:
			node: 出力先 Node.

		Returns:
			ローカル変数ID.
		"""
		return self.output.get_output_variable_id(node)

	def build(self, depth):
		"""コード生成を行う.

		Args:
			depth: 関数呼び出し深さ.
		"""
		if not self._build_begin():
			return

		all_nodes = set(self.nodes)
		nodewise_inputs = {}
		for n in all_nodes:
			nodewise_inputs[n] = n.get_inputs()

		all_inputs = set()
		for n, inputs in nodewise_inputs.items():
			for i in inputs:
				all_inputs.add(i)

		# モデル外ノードを入力としていたらエラーとする
		for n, inputs in nodewise_inputs.items():
			if len(inputs.difference(all_nodes)) != 0:
				raise Exception('Child nodes of SubModel can not input external nodes.')

		# 入力ノードを集める
		inputs = [n for n, inputs in nodewise_inputs.items() if len(inputs) == 0]
		if len(inputs) == 0:
			raise Exception('SubModel must have a one input.')
		if len(inputs) != 1:
			raise Exception('SubModel does not support multiple inputs.')

		# 出力ノード（入力とされていないノード）を集める
		outputs = [n for n in all_nodes if n not in all_inputs]
		if len(outputs) == 0:
			raise Exception('SubModel must have a one output.')
		if len(outputs) != 1:
			raise Exception('SubModel does not support multiple outputs.')

		# ビルド
		inputs[0].set_inputs(set([self.input]))
		self.output = outputs[0]
		self.output.build(depth)

		# 使用ノードをサブグラフとする
		if self.owner is not None:
			dot_node_name = Node.get_dot_node_name(self)
			self.root_node.dot_code.append('subgraph cluster_{} {{ label="{}"'.format(dot_node_name, dot_node_name))
			for node in self.nodes:
				self.root_node.dot_code.append('{};'.format(node.get_dot_node_name()))
			self.root_node.dot_code.append('}')

		self._build_end()

	def add_ouput(self, node):
		"""出力ノードを追加する.

		Args:
			node: 出力 Node.

		Returns:
			入力 Node の set.
		"""
		if self.output is not None:
			raise Exception('SubModel can not have multiple outputs.')
		self.output = node

	def get_inputs(self):
		"""入力ノード集合を取得する.

		Returns:
			入力 Node の set.
		"""
		return set([self.input]) if self.input is not None else set()

	def set_inputs(self, inputs):
		"""入力ノード集合を設定する.

		Args:
			inputs: 入力 Node 集合
		"""
		self.input = None
		if inputs is not None:
			for i in inputs:
				self.input = i
				break

	def collect_gate(self, output, depth, gate_set):
		"""入力側の直近の複数出力 Gate を集める.

		Args:
			output: 呼び出し元出力側レイヤ.
			depth: 関数呼び出し深さ.
			gate_set: Gate 収集先 set.
		"""
		if self.input is None:
			return
		self.input.collect_gate(self, depth + 1, gate_set)


class Model(SubModel):
	"""モデル、 input() 、 layer() 、 output() により複数のレイヤを保持する事が可能.

	Args:
		optimizer: 重み最適化処理方法、 chainer.optimizer.Optimizerr を継承するもの.
	"""

	def __init__(self, optimizer):
		SubModel.__init__(self, None, 'root')

		self.optimizer = optimizer
		self.local_variable_ids = set()
		self.code = []
		self.dot_code = []
		self.prediction = None

	def build(self):
		"""モデルの推論用関数をビルドする.
		"""
		self.code.append('def prediction(self, x):')
		self.dot_code.insert(0, 'digraph {\n')
		self.dot_code.insert(1, 'node [shape=box]\n')

		SubModel.build(self, 0)

		self.dot_code.append('}')
		self.dot_code = '\n'.join(self.dot_code)
		self.code.append('\treturn {}\n'.format(self.output.get_output_variable_name(None)))
		self.code = '\n'.join(self.code)

		l = {}
		exec(self.code, globals(), l)
		self.prediction = types.MethodType(l['prediction'], self)

		self.optimizer.setup(self)

	def __call__(self, x):
		"""計算を実行する.

		Args:
			x: 入力値.

		Returns:
			結果.
		"""
		return self.prediction(x)

	def new_local_variable_id(self):
		"""モデルの推論用関数内で使用されるローカル変数のIDを生成する.

		Returns:
			ローカル変数ID.
		"""
		ids = self.local_variable_ids
		id = 1
		while id in ids:
			id += 1
		ids.add(id)
		return id

	def del_local_variable_id(self, id):
		"""モデルの推論用関数内で使用されるローカル変数を削除する.

		Args:
			id: ローカル変数ID.
		"""
		if id == 0:
			return
		self.local_variable_ids.remove(id)

	def get_local_variable_name(self, id):
		"""モデルの推論用関数内で使用されるローカル変数名を取得する.

		Args:
			id: ローカル変数ID.

		Returns:
			ローカル変数名.
		"""
		if id == 0:
			return 'x'
		if id in self.local_variable_ids:
			return 'x' + str(id)
		else:
			raise LookupError('There is no variable with the specified ID ' + str(id) + '.')


def startup(gpu, train=True):
	"""環境を初期化する.

	Args:
		gpu: 使用するGPUインデックス、負数ならGPU未使用となる.
	"""
	global test, xp, cp, cuda
	if xp is not None:
		return
	test = not train
	chainer.config.train = train
	if not train:
		chainer.config.no_backprop_mode()
	if 0 <= gpu:
		print(('Using cuda device {}.').format(gpu))
		import cupy as _cp
		from chainer import cuda as _cuda
		cp = _cp
		cuda = _cuda
		cuda.get_device(gpu).use()
		xp = cp
	else:
		print('Using numpy.')
		xp = np


def to_gpu(x):
	"""GPU利用可能状態ならGPUメモリオブジェクトに変換する.

	Args:
		x: 変換対象オブジェクト.
	Returns:
		変換後のオブジェクト.
	"""
	if xp is cp:
		if isinstance(x, Model):
			m = x.to_gpu()
			m.optimizer = x.optimizer
			return m
		if isinstance(x, chainer.Link):
			return x.to_gpu()
		if isinstance(x, chainer.Optimizer):
			return x
		if isinstance(x, chainer.Variable):
			return x.to_gpu()
		if isinstance(x, tuple):
			return tuple([to_gpu(e) for e in x])
		if isinstance(x, list):
			return [to_gpu(e) for e in x]
		return cuda.to_gpu(x)
	else:
		return x


def to_cpu(x):
	"""GPU利用可能状態ならCPUメモリオブジェクトに変換する.

	Args:
		x: 変換対象オブジェクト.
	Returns:
		変換後のオブジェクト.
	"""
	if xp is cp:
		if isinstance(x, Model):
			m = x.to_cpu()
			m.optimizer = x.optimizer
			return m
		if isinstance(x, chainer.Link):
			return x.to_cpu()
		if isinstance(x, chainer.Optimizer):
			return x
		if isinstance(x, chainer.Variable):
			return x.to_cpu()
		if isinstance(x, tuple):
			return tuple([to_cpu(e) for e in x])
		if isinstance(x, list):
			return [to_cpu(e) for e in x]
		return cuda.to_cpu(x)
	else:
		return x


def to_xp(x):
	"""オブジェクトを startup() 関数に指定したデバイスメモリに変換する.

	Args:
		x: 変換対象オブジェクト.
	Returns:
		変換後のオブジェクト.
	"""
	return to_gpu(x) if xp is cp else to_cpu(x)


def save(file_name, model):
	model = to_cpu(model)
	chainer.serializers.save_npz(file_name + '.mdl', model)
	chainer.serializers.save_npz(file_name + '.opt', model.optimizer)


def load(file_name, model):
	model = to_cpu(model)
	chainer.serializers.load_npz(file_name + '.mdl', model)
	chainer.serializers.load_npz(file_name + '.opt', model.optimizer)
	return model


if __name__ == '__main__':
	startup(0)
