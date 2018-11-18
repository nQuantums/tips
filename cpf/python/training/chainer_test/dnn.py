"""ディープラーニング関係ヘルパ、テンソルのCPU↔GPU変換処理など.

使用例）
	import dnn
	from dnn import chainer
	from dnn import F
	from dnn import L
	from dnn import Variable
	# import chainer.computational_graph as ccg

	dnn.startup(-1) # 0番のGPU使用
	xp = dnn.xp # numpy または cupy

	# x を出力先ノード数分に分割する
	def split(x, out):
		n = len(out)
		shape = x.shape
		x = x.reshape((n, shape[0], shape[1] // n))
		return tuple(e for e in x)

	# 分割されている x を１つに結合する
	def concat(x, out):
		return F.concat(tuple(e for e in x), axis=1)

	# 入力を２つに分割して dense 通して１つに結合するモデルを生成する
	def diamond(self, ch):
		with self.model('diamond') as m: 
			g = m.gate(split)
			g = m.gate(concat, g.dense(ch // 2, ch // 2).relu(), g.dense(ch // 2, ch // 2).relu())
			return m

	# モデル構築
	dnn.Node.diamond = diamond # 呼び出しが面倒なので Node に diamond 機能をもたせる
	with dnn.Model(chainer.optimizers.Adam()) as m: # とりあえず Model 作る
		h = m.dense(32, 8).gate(split) # dense 通して分割用 Gate に通る様に
		h = m.gate(concat, h.diamond(4), h.diamond(4)).dense(8, 32) # 分割用 Gate で２つの diamond モデル通す様に分割してそれを結合する Gate を通る様にする
		m.build('dnn_build_test.py', 'predmodule')

	# モデルデータが保存済みなら読み込み
	dnn.load_if_exists("modeldata", m)

	# 所定のデバイスメモリに転送
	m = dnn.to_xp(m)

	# とりあえず入力値を単純に10倍にするネットを目標とする
	for i in range(100):
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
import os
import types
import inspect
from importlib.machinery import SourceFileLoader
import numpy as np, chainer, chainer.functions as F, chainer.links as L
from chainer import Variable
from chainer import Chain
from chainer.link import Link

xp = None
cp = None
cuda = None
test = False
dtype = np.float32


class Base:
	"""全ノードに提供される共通メソッドの定義.
	"""

	def funcs(self, kind_name, funcs):
		"""自分を入力とする出力 Funcs を生成する.

		Args:
			kind_name: 種類名.
			funcs: 関数列、各関数は def func(x): return x * 2 の様な形式.

		Returns:
			Node.
		"""

	def relu(self):
		return self.funcs('relu', F.relu)

	def leaky_relu(self):
		return self.funcs('leaky_relu', F.leaky_relu)

	def tanh(self):
		return self.funcs('tanh', F.tanh)

	def sigmoid(self):
		return self.funcs('sigmoid', F.sigmoid)

	def dropout(self, ratio=.5, **kwargs):

		def dropout(x):
			return F.dropout(x, ratio, **kwargs)

		return self.funcs('dropout', dropout)

	def unpool2d(self, ksize, stride=None, pad=0, outsize=None, cover_all=True):

		def unpool2d(x):
			return F.unpooling_2d(x, ksize, stride, pad, outsize, cover_all)

		return self.funcs('unpool2d', unpool2d)


class FuncsHolder:
	"""関数を生成し保持する機能を提供するクラス.
	"""

	def __init__(self):
		self.func_list = [] # 関数リスト、先頭から順に実行される
		self.func_kind_names = {} # 関数をキーにした種類名マップ

	def funcs(self, kind_name, funcs):
		"""自分を入力とする出力 Funcs を生成する.

		Args:
			kind_name: 種類名.
			funcs: 関数列、各関数は def func(x): return x * 2 の様な形式.

		Returns:
			Node.
		"""
		if not isinstance(funcs, list):
			if not callable(funcs):
				raise TypeError("Invalid argument. 'funcs' must be callable or callable list.")
			funcs = [funcs]
		for f in funcs:
			if not callable(f):
				raise TypeError("Invalid argument. 'funcs' must be callable or callable list.")
		self.func_list.extend(funcs)
		for f in funcs:
			self.func_kind_names[f] = kind_name if f.__name__ == '<lambda>' else f.__name__
		return self


class Node(Base):
	"""モデルを構成するノード、レイヤを生成する機能を提供するクラス.

	Args:
		owner: このノードの所有者となる Node.
		kind_name: 種類名.
	"""

	def __init__(self, owner, kind_name):
		self.owner = owner # このノードの所有者となる Node.
		self.kind_name = kind_name # 種類名
		self.name = None # 所有者 Node 内での自身の名前
		self.inputs = [] # 入力元ノード列
		self.outputs = [] # 出力先ノード列
		self.dot_param = '' # グラフ内に表示するパラメータ文字列
		self.output_variable_ids = {} # 出力ノードをキーとしたローカル変数ID列、None がキーにする場合全てのレイヤーに優先する
		self.is_built = False # 既にビルドされたかどうか
		self.is_uner_build = False # ビルド途中かどうか
		self.allow_multi_inputs = True # 複数の入力元ノードを許可するかどうか
		self.allow_multi_outputs = True # 複数の出力先ノードを許可するかどうか
		self.output_same_value = False # 全出力先ノードに同じ値を出力するかどうか
		self.tmp_code_node_name = None # None 以外が指定されると get_code_node_name() がこの値を返す

		# ルート Node の取得
		node = self
		while True:
			p = node.owner
			if p is None:
				break
			node = p
		self.root = node # ルート Node

	def __str__(self):
		return self.get_full_name()

	def __repr__(self):
		return self.get_full_name()

	def get_full_name(self):
		"""ルート Node からのフル名称を取得する.
		"""
		root = self.root
		if self == root:
			return self.name

		names = [self.name]
		node = self.owner
		while node != root:
			nm = node.name
			if nm is not None and len(nm) != 0:
				names.append(nm)
			node = node.owner
		return '.'.join(reversed(names))

	def get_code_node_name(self):
		"""生成される推論関数内でのこの Node の名称を取得する.
		"""
		if self.tmp_code_node_name is not None:
			return self.tmp_code_node_name

		root = self.root
		if self == root:
			return 'self'

		return '{}.{}'.format(self.owner.get_code_node_name(), self.name)

	def get_dot_node_name(self):
		"""dot 内でのノード名の取得.

		Returns:
			ノード名.
		"""
		return self.get_full_name().replace('.', '_')

	def get_dot_node_label(self):
		"""dot 内でのノードのラベルの取得.

		Returns:
			ノードのラベル.
		"""
		return '{}{}'.format(self.get_full_name(), self.dot_param)

	def get_inputs(self):
		"""入力元ノード集合を取得する.

		Returns:
			入力元 Node の set.
		"""
		return set(self.inputs)

	def get_outputs(self):
		"""出力先ノード集合を取得する.

		Returns:
			出力先 Node の set.
		"""
		return set(self.outputs)

	def add_input(self, node):
		"""入力元ノードを追加する.

		Args:
			node: 入力元 Node.
		"""
		if not self.allow_multi_inputs and 1 <= len(self.inputs):
			raise Exception("Node '{}' does not support multiple inputs.".format(self.get_full_name()))
		self.inputs.append(node)

	def add_ouput(self, node):
		"""出力先ノードを追加する.

		Args:
			node: 出力先 Node.
		"""
		if not self.allow_multi_outputs and 1 <= len(self.outputs):
			raise Exception("Node '{}' does not support multiple outputs.".format(self.get_full_name()))
		self.outputs.append(node)

	def set_inputs(self, inputs):
		"""入力ノード集合を設定する.

		Args:
			inputs: 入力 Node 集合.
		"""
		if not self.allow_multi_inputs and 2 <= len(inputs):
			raise Exception("Node '{}' does not support multiple inputs.".format(self.get_full_name()))
		self.inputs = list(inputs)

	def set_outputs(self, outputs):
		"""入力ノード集合を設定する.

		Args:
			inputs: 入力 Node 集合.
		"""
		if not self.allow_multi_outputs and 2 <= len(outputs):
			raise Exception("Node '{}' does not support multiple outputs.".format(self.get_full_name()))
		self.outputs = list(outputs)

	def replace_input(self, before, after):
		"""入力元ノード一覧内の before を after に置き換える.

		Args:
			before: 置き換えられる Node.
			after: 置き換え後の Node.
		"""
		nodes = self.inputs
		for index, node in enumerate(nodes):
			if node == before:
				nodes[index] = after
				break

	def replace_output(self, before, after):
		"""出力先ノード一覧内の before を after に置き換える.

		Args:
			before: 置き換えられる Node.
			after: 置き換え後の Node.
		"""
		nodes = self.outputs
		for index, node in enumerate(nodes):
			if node == before:
				nodes[index] = after
				break

	def model(self, kind_name):
		"""自分を入力とする出力 SubModel を生成する.

		Args:
			kind_name: 種類名.

		Returns:
			SubModel.
		"""
		nodes_owner = self._get_nodes_owner()
		return nodes_owner._on_new_node(kind_name, SubModel(nodes_owner, kind_name, self))

	def nop(self):
		"""自分を入力とする出力 NoOp を生成する.

		Returns:
			NoOp.
		"""
		nodes_owner = self._get_nodes_owner()
		return nodes_owner._on_new_node('nop', NoOp(nodes_owner, 'nop', self))

	def funcs(self, kind_name, funcs):
		"""自分を入力とする出力 Funcs を生成する.

		Args:
			kind_name: 種類名.
			funcs: 関数列、各関数は def func(x): return x * 2 の様な形式.

		Returns:
			Funcs.
		"""
		nodes_owner = self._get_nodes_owner()
		return nodes_owner._on_new_node(kind_name, Funcs(nodes_owner, kind_name, funcs, self))

	def layer(self, kind_name, link):
		"""自分を入力とする出力 Layer を生成する.

		Args:
			kind_name: 種類名.
			link: レイヤーの計算のメインとなる Link.

		Returns:
			Layer.
		"""
		nodes_owner = self._get_nodes_owner()
		return nodes_owner._on_new_node(kind_name, Layer(nodes_owner, kind_name, link, self))

	def gate(self, func, *inputs):
		"""レイヤ同士を結合する Gate を作成する.

		Args:
			func: 入力値を出力レイヤーに通す処理、 def func(gate, x, output_layers).
			inputs: 入力レイヤー列.

		Returns:
			Gate.
		"""
		if len(inputs) == 0:
			inputs = (self,)
		nodes_owner = self._get_nodes_owner()
		return nodes_owner._on_new_node('gate', Gate(nodes_owner, func, *inputs))

	def named_gate(self, kind_name, func, *inputs):
		"""レイヤ同士を結合する Gate を作成する.

		Args:
			kind_name: 種類名.
			func: 入力値を出力レイヤーに通す処理、 def func(gate, x, output_layers).
			inputs: 入力レイヤー列.

		Returns:
			Gate.
		"""
		if len(inputs) == 0:
			inputs = (self,)
		nodes_owner = self._get_nodes_owner()
		return nodes_owner._on_new_node(kind_name, Gate(nodes_owner, func, *inputs))

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

	def prelu(self, shape=(), init=0.25):
		l = self.layer('prelu', L.PReLU(shape, init))
		l.dot_param = '(shape:{})'.format(shape)
		return l

	def _get_nodes_owner(self):
		"""新規 Node 生成時に親となる Node の取得.

		Returns:
			Node.
		"""
		return self if self.owner is None else self.owner

	def _on_new_node(self, kind_name, node):
		"""新規 Node 生成後の処理、指定 Node は適切な Node に所有される.

		Args:
			kind_name: 種類名.
			node: 所有する Node.

		Returns:
			指定された Node.
		"""

	def _build_begin(self):
		"""コード生成開始.

		Returns:
			ビルド開始成功したら True それ以外は False.
		"""
		if self.is_built:
			return False
		if self.is_uner_build:
			raise Exception("Node '{}' has already built.".format(self.get_full_name()))
		self.is_uner_build = True
		return True

	def _build_end(self):
		"""コード生成終了.
		"""
		self.is_built = True
		self.is_uner_build = False

	def _build(self, depth):
		"""コード生成を行う.

		Args:
			depth: 関数呼び出し深さ.
		"""
		if not self._build_begin():
			return

		if len(self.outputs) == 0:
			raise Exception("Node '{}' must have one or more outputs.".format(self.get_full_name()))

		root = self.root
		depth += 1

		# 入力側にある複数出力の Node を収集し、呼び出しが早い方からビルドしていく
		gate_set = set()
		for i in self.inputs:
			i._collect_multi_output_nodes(self, depth, gate_set)
		gate_list = [g for g in gate_set]
		gate_list.sort(key=lambda g: g[1])
		for g in reversed(gate_list):
			g[0]._build(g[1])

		# 入力側を先にビルドし、入力値を保持している変数名取得して用済みになったので削除
		in_vars = []
		for i in self.inputs:
			i._build(depth)
			in_vars.append(root.var_mgr.get_var((i, self), False))
		for v in in_vars:
			root.var_mgr.release_var(v)

		# 出力値の代入先変数確保
		out_vars = [root.var_mgr.get_var((self, o)) for o in self.outputs]

		# コードの追加
		in_tuple_str = ', '.join(in_vars) if len(in_vars) != 0 else 'x'
		if len(out_vars) != 0:
			out_tuple_str = out_vars[0] if self.output_same_value else ', '.join(out_vars)
		else:
			out_tuple_str = root.var_mgr.get_var((self, None))
		self._add_code(out_tuple_str, '({})'.format(in_tuple_str) if self.allow_multi_inputs else in_tuple_str)

		# グラフ用コードの追加
		dot_name = self.get_dot_node_name()
		self.root.dot_code.append('{} [label="{}"];'.format(dot_name, self.get_dot_node_label()))
		for i, var in zip(self.inputs, in_vars):
			self.root.dot_code.append('{} -> {} [label="{}"];'.format(i.get_dot_node_name(), dot_name, var))

		self._build_end()

	def _add_code(self, out_tuple_str, in_tuple_str):
		"""推論関数用コードを追加する.

		Args:
			out_tuple_str: 計算結果を格納する変数名.
			in_tuple_str: 入力値を格納している変数名.
		"""
		self.root.code.append('\t{} = {}({})'.format(out_tuple_str, self.get_code_node_name(), in_tuple_str))

	def _collect_multi_output_nodes(self, output, depth, node_set):
		"""入力側の直近の複数出力 Node を集める.

		Args:
			output: 呼び出し元出力側レイヤ.
			depth: 関数呼び出し深さ.
			node_set: Node 収集先 set.
		"""
		if 2 <= len(self.outputs):
			node_set.add((self, depth))
		else:
			depth += 1
			for i in self.inputs:
				i._collect_multi_output_nodes(self, depth, node_set)

	def _get_input_connected(self, input):
		"""指定の入力元 Node に実際に繋がる Node の取得.
		"""
		return self

	def _get_output_connected(self, output):
		"""指定の出力先 Node に実際に繋がる Node の取得.
		"""
		return self


class NoOp(Node):
	"""何もしないノード、入力値 x をそのまま出力する.

	Args:
		owner: 所有者となる Node.
		kind_name: 種類名.
		input: 入力 Node または None.
	"""

	def __init__(self, owner, kind_name, input=None):
		if input == owner:
			input = None

		if input is not None and not isinstance(input, Node):
			raise TypeError("Invalid argument. 'input' must be Node.")

		Node.__init__(self, owner, kind_name)

		self.allow_multi_inputs = False
		self.output_same_value = True

		if input is not None:
			input.add_ouput(self)
			self.add_input(input)

	def __call__(self, x=None):
		"""指定値をレイヤーで変換する.

		Args:
			x: 入力値.

		Returns:
			変換後の値.
		"""
		return x

	def _add_code(self, out_tuple_str, in_tuple_str):
		"""推論関数用コードを追加する.

		Args:
			out_tuple_str: 計算結果を格納する変数名.
			in_tuple_str: 入力値を格納している変数名.
		"""
		if out_tuple_str != in_tuple_str:
			self.root.code.append('\t{} = {}'.format(out_tuple_str, in_tuple_str))


class Funcs(FuncsHolder, Node):
	"""関数列.

	Args:
		owner: 所有者となる Node.
		kind_name: 種類名.
		funcs: 関数列、各関数は def func(x): return x * 2 の様な形式.
		input: 入力 Node または None.
	"""

	def __init__(self, owner, kind_name, funcs, input=None):
		if input == owner:
			input = None

		if input is not None and not isinstance(input, Node):
			raise TypeError("Invalid argument. 'input' must be Node.")
		if not isinstance(funcs, list):
			if not callable(funcs):
				raise TypeError("Invalid argument. 'funcs' must be callable or callable list.")
			funcs = [funcs]
		for f in funcs:
			if not callable(f):
				raise TypeError("Invalid argument. 'funcs' must be callable or callable list.")

		Node.__init__(self, owner, kind_name)
		FuncsHolder.__init__(self)

		self.allow_multi_inputs = False
		self.output_same_value = True

		if input is not None:
			input.add_ouput(self)
			self.add_input(input)

		self.func_list.extend(funcs)
		for f in funcs:
			self.func_kind_names[f] = kind_name if f.__name__ == '<lambda>' else f.__name__

	def get_dot_node_label(self):
		"""dot 内でのノードのラベルの取得.

		Returns:
			ノードのラベル.
		"""
		return '{}{}\\n{}'.format(self.get_full_name(), self.dot_param, ', '.join(self.func_kind_names[f] for f in self.func_list))

	def __call__(self, x=None):
		"""指定値をレイヤーで変換する.

		Args:
			x: 入力値.

		Returns:
			変換後の値.
		"""
		for f in self.func_list:
			x = f(x)
		return x


class Layer(Chain, FuncsHolder, Node):
	"""レイヤ、学習対象の重み、活性化関数.

	Args:
		owner: 所有者となる Node.
		kind_name: 種類名.
		link: 学習対象の重みを持つ chainer.links.Convolution2D など chainer.link.Link を継承するもの.
		input: 入力 Node または None.
	"""

	def __init__(self, owner, kind_name, link, input=None):
		if input == owner:
			input = None

		if input is not None and not isinstance(input, Node):
			raise TypeError("Invalid argument. 'input' must be Node.")
		if not isinstance(link, Link):
			raise TypeError('Cannot register a non-link object as a child')

		Chain.__init__(self)
		Node.__init__(self, owner, kind_name)
		FuncsHolder.__init__(self)

		self.allow_multi_inputs = False
		self.output_same_value = True

		if input is not None:
			input.add_ouput(self)
			self.add_input(input)

		with self.init_scope():
			self.link = link

	def get_dot_node_label(self):
		"""dot 内でのノードのラベルの取得.

		Returns:
			ノードのラベル.
		"""
		return '{}{}\\n{}'.format(self.get_full_name(), self.dot_param, ', '.join(self.func_kind_names[f] for f in self.func_list))

	def __call__(self, x=None):
		"""指定値をレイヤーで変換する.

		Args:
			x: 入力値.

		Returns:
			変換後の値.
		"""
		x = self.link(x)
		for f in self.func_list:
			x = f(x)
		return x


class Gate(Node):
	"""指定値を１～複数の出力レイヤーで変換する、複数対複数のレイヤーをくっつける役割を持つ.

	Args:
		owner: 所有者となる Node.
		func: 引数を入力レイヤーを通しさらに出力レイヤーを通す処理、 def func(x, output_layers).
		inputs: 入力レイヤー列.
	"""

	def __init__(self, owner, func, *inputs):
		inputs = tuple([(i if i != owner else None) for i in inputs])

		for i in inputs:
			if i is not None and not isinstance(i, Node):
				raise TypeError("Invalid argument. 'inputs' element must be Node.")

		Node.__init__(self, owner, 'Gate')

		for input in inputs:
			if input is not None:
				input.add_ouput(self)
				self.add_input(input)

		self.func = func

	def get_dot_node_label(self):
		"""dot 内でのノードのラベルの取得.

		Returns:
			ノードのラベル.
		"""
		return '{}\\n{}'.format(self.get_full_name(), self.func.__name__)

	def __call__(self, x):
		"""指定値をユーザー指定処理で変換する.

		Args:
			x: 入力値.

		Returns:
			変換後の値.
		"""
		return self.func(x, self.outputs)


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
		Node.__init__(self, owner, kind_name)

		self.allow_multi_inputs = False
		self.output_same_value = True

		if input is not None:
			input.add_ouput(self)
			self.add_input(input)

		self.nodes = [] # 子ノード列
		self.kindwise_count = {} # 種類毎の子ノード数
		self.firsts = None # 最初のノード列
		self.lasts = None # 最後のノード列
		self.assembly_depth = 0 # これが 0 以外ならノード生成時に子ノードとして登録される、0 なら self.owner の子ノードとして登録される

	def __enter__(self):
		self.assembly_depth += 1
		return self

	def __exit__(self, type, value, traceback):
		self.assembly_depth -= 1

	def get_dot_node_name(self):
		"""dot 内でのノード名の取得.

		Returns:
			ノード名.
		"""
		return self.outputs[0].get_dot_node_name()

	def _get_nodes_owner(self):
		"""新規 Node 生成時に親となる Node の取得.

		Returns:
			Node.
		"""
		return self if self.assembly_depth != 0 or self.owner is None else self.owner

	def _on_new_node(self, kind_name, node):
		"""新規 Node 生成後の処理、指定 Node は適切な Node に所有される.

		Args:
			kind_name: 種類名.
			node: 所有する Node.

		Returns:
			指定された Node.
		"""
		count = self.kindwise_count[kind_name] + 1 if kind_name in self.kindwise_count else 1
		name = kind_name + str(count)
		if isinstance(node, Link):
			self.add_link(name, node)
		else:
			setattr(self, name, node)
			node.name = name
		self.nodes.append(node)
		self.kindwise_count[kind_name] = count
		return node

	def _search_ends(self):
		"""最初と最後のノードを探す.
		"""
		if self.firsts is not None:
			return

		all_nodes = set(self.nodes)
		nodewise_inputs = {n: n.get_inputs() for n in all_nodes}
		all_inputs = {i for inputs in nodewise_inputs.values() for i in inputs}

		# モデル外ノードを入力としていたらエラーとする
		for n, inputs in nodewise_inputs.items():
			dif = inputs.difference(all_nodes)
			if len(dif) != 0:
				raise Exception("Child nodes of '{}' can not input external nodes. {}".format(self.get_full_name(), ', '.join(i.get_full_name() for i in dif)))

		# 最初のノードを集める
		firsts = [n for n, firsts in nodewise_inputs.items() if len(firsts) == 0]
		if len(firsts) == 0:
			raise Exception("Node '{}' must have a one input.".format(self.get_full_name()))
		if len(firsts) != 1:
			raise Exception("Node '{}' does not support multiple inputs. {}".format(self.get_full_name(), ', '.join(i.get_full_name() for i in firsts)))

		# 最後のノード（入力とされていないノード）を集める
		lasts = [n for n in all_nodes if n not in all_inputs]
		if len(lasts) == 0:
			raise Exception("Node '{}' must have a one output.".format(self.get_full_name()))
		if len(lasts) != 1:
			raise Exception("Node '{}' does not support multiple outputs. {}".format(self.get_full_name(), ', '.join(i.get_full_name() for i in lasts)))

		self.firsts = firsts
		self.lasts = lasts

	def _build(self, depth):
		"""コード生成を行う.

		Args:
			depth: 関数呼び出し深さ.
		"""
		if not self._build_begin():
			return

		self._search_ends()

		# モデル外ノードとの接続を付け替え
		if len(self.inputs) != 0 and self.inputs[0] is not None:
			self.inputs[0].replace_output(self, self.firsts[0])
		if len(self.outputs) != 0 and self.outputs[0] is not None:
			self.outputs[0].replace_input(self, self.lasts[0])
		self.firsts[0].set_inputs(self.get_inputs())
		self.lasts[0].set_outputs(self.get_outputs())

		# ビルド
		tmp_var = None
		if self != self.root:
			tmp_var_mgr = self.root.tmp_var_mgr
			tmp_var = tmp_var_mgr.get_var((self, self))
			self.root.code.append('\t{} = {}'.format(tmp_var, self.get_code_node_name()))
			self.tmp_code_node_name = tmp_var

		self.lasts[0]._build(depth)

		if tmp_var is not None:
			self.tmp_code_node_name = None
			tmp_var_mgr.release_var(tmp_var)

		# 使用ノードをサブグラフとする
		if self.owner is not None:
			dot_node_name = Node.get_dot_node_name(self)
			self.root.dot_code.append('subgraph cluster_{} {{ label="{}"'.format(dot_node_name, dot_node_name))
			for node in self.nodes:
				self.root.dot_code.append('{};'.format(node.get_dot_node_name()))
			self.root.dot_code.append('}')

		self._build_end()

	def _get_input_connected(self, input):
		"""指定の入力元 Node に実際に繋がる Node の取得.
		"""
		self._search_ends()
		return self.firsts[0]

	def _get_output_connected(self, output):
		"""指定の出力先 Node に実際に繋がる Node の取得.
		"""
		self._search_ends()
		return self.lasts[0]


class VarMgr:
	"""一時変数名管理クラス.

	Args:
		prefix: 一時変数名の先頭に付与される文字列.
	"""

	def __init__(self, prefix):
		self.prefix = prefix
		self.vars = {}

	def get_var(self, var_key, increment_refcount=True):
		"""モデルの推論用関数内で使用されるローカル変数名を取得または生成する.

		Args:
			var_key: 入力元 Node と出力先 Node のタプル.
			increment_refcount: 変数への参照カウントをインクリメントするかどうか.

		Returns:
			ローカル変数名.
		"""

		class Var:

			def __init__(self, id, prefix):
				self.id = id
				self.prefix = prefix
				self.refcount = 1

			def get_var_name(self):
				return self.prefix + str(self.id)

		# 実際に接続されるノード用の変数を作成する
		i = var_key[0]
		o = var_key[1]
		var_key = (i._get_output_connected(o) if i is not None else i), (o._get_input_connected(i) if o is not None else o)
		# 入力側からの出力が全て同じ値になるノードなら、出力先毎に変数変える必要は無い
		if var_key[0] is not None and var_key[0].output_same_value:
			var_key = var_key[0], var_key[0]

		vars = self.vars
		if var_key not in vars:
			if not increment_refcount:
				raise Exception("Internal error. Local variable for {} is not exists.".format(var_key))
			id = 1
			ids = [var.id for var in vars.values()]
			while id in ids:
				id += 1
			var = Var(id, self.prefix)
			vars[var_key] = var
		else:
			var = vars[var_key]
			if increment_refcount:
				var.refcount += 1
		return var.get_var_name()

	def release_var(self, var_name):
		"""モデルの推論用関数内で使用されるローカル変数名を解放する.

		Args:
			var_name: 変数名.
		"""
		vars = self.vars
		for k, v in vars.items():
			if v.get_var_name() == var_name:
				v.refcount -= 1
				if v.refcount <= 0:
					del vars[k]
					break


class Model(SubModel):
	"""モデル、 input() 、 layer() 、 output() により複数のレイヤを保持する事が可能.

	Args:
		optimizer: 重み最適化処理方法、 chainer.optimizer.Optimizer を継承するもの.
	"""

	def __init__(self, optimizer):
		SubModel.__init__(self, None, 'root')

		self.name = 'root'
		self.optimizer = optimizer
		self.var_mgr = VarMgr('x')
		self.tmp_var_mgr = VarMgr('m')
		self.code = []
		self.dot_code = []
		self.prediction = None

		self.add_ouput(None)

	def build(self, output_file_name=None, module_name=None):
		"""モデルの推論用関数をビルドする.

		Args:
			output_file_name: 推論関数出力ソースファイル名、None 以外が指定されたらこのファイル作成してインポートされる、デバッグ用.
			module_name: output_file_name ロード時に付与するモジュール名.
		"""
		self.code.append('def prediction(self, x):')
		self.dot_code.insert(0, 'digraph {\n')
		self.dot_code.insert(1, 'node [shape=box]\n')

		SubModel._build(self, 0)

		if len(self.var_mgr.vars) == 0:
			raise Exception("Internal error. No output exists after '{}' built.".format(self.get_full_name()))
		if 2 <= len(self.var_mgr.vars):
			raise Exception("Internal error. Multiple output exists after '{}' built.".format(self.get_full_name()))
		for v in self.var_mgr.vars.values():
			var_name = v.get_var_name()

		self.dot_code.append('}')
		self.dot_code = '\n'.join(self.dot_code)
		self.code.append('\treturn {}\n'.format(var_name))
		self.code = '\n'.join(self.code)

		if output_file_name is None:
			l = {}
			exec(self.code, globals(), l)
			self.prediction = types.MethodType(l['prediction'], self)
		else:
			output_file_name = os.path.abspath(output_file_name)
			with open(output_file_name, mode='w') as f:
				f.write(self.code)
			module = SourceFileLoader(module_name, output_file_name).load_module()
			func = module.prediction
			self.prediction = lambda x: func(self, x)

		self.optimizer.setup(self)

	def __call__(self, x):
		"""計算を実行する.

		Args:
			x: 入力値.

		Returns:
			結果.
		"""
		return self.prediction(x)


class Models:
	"""複数の Model を所有するクラス.
	"""

	def __str__(self):
		return self.get_all_model_names()

	def __repr__(self):
		return self.get_all_model_names()

	def get_all_model_names(self):
		return '\n'.join(v for k, v in vars(self).items() if isinstance(v, Model))

	def build(self, output_file_name=None):
		"""モデルの推論用関数をビルドする.

		Args:
			output_file_name: 推論関数出力ソースファイル名、None 以外が指定されたらこのファイル作成してインポートされる、デバッグ用.
		"""
		for k, v in vars(self).items():
			if isinstance(v, Model):
				if output_file_name is None:
					v.build()
				else:
					v.build(output_file_name + '.' + k + '.py', k)


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
		if isinstance(x, Models):
			d = vars(x)
			for k, v in d.items():
				if isinstance(v, Model):
					d[k] = to_gpu(v)
			return x
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
		if isinstance(x, Models):
			d = vars(x)
			for k, v in d.items():
				if isinstance(v, Model):
					d[k] = to_cpu(v)
			return x
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


def get_model_file_names(file_name, model):
	d = {}

	def get_file_name(fn, m):
		if isinstance(m, Model):
			d[m] = fn + '.mdl'
			d[m.optimizer] = fn + '.opt'
		elif isinstance(m, Models):
			for k, v in vars(m).items():
				if isinstance(v, Model):
					get_file_name(fn + '.' + k, v)
		else:
			raise TypeError("Invalid argument. 'm' must be Model or Models type.")

	get_file_name(file_name, model)

	return d


def save(file_name, model):
	model = to_cpu(model)

	d = get_model_file_names(file_name, model)

	def save_internal(fn, m):
		if isinstance(m, Model):
			chainer.serializers.save_npz(d[m], m)
			chainer.serializers.save_npz(d[m.optimizer], m.optimizer)
		elif isinstance(m, Models):
			for k, v in vars(m).items():
				if isinstance(v, Model):
					save(fn + '.' + k, v)
		else:
			raise TypeError("Invalid argument. 'm' must be Model or Models type.")

	save_internal(file_name, model)


def load(file_name, model):
	model = to_cpu(model)

	d = get_model_file_names(file_name, model)

	def load_internal(fn, m):
		if isinstance(m, Model):
			chainer.serializers.load_npz(d[m], m)
			chainer.serializers.load_npz(d[m.optimizer], m.optimizer)
			return m
		elif isinstance(m, Models):
			for k, v in vars(m).items():
				if isinstance(v, Model):
					load(fn + '.' + k, v)
			return m
		else:
			raise TypeError("Invalid argument. 'm' must be Model or Models type.")

	return load_internal(file_name, model)


def load_if_exists(file_name, model):
	for k, v in get_model_file_names(file_name, model).items():
		if not os.path.exists(v):
			return model
	load(file_name, model)


if __name__ == '__main__':
	startup(0)
