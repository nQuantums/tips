"""ディープラーニング関係ヘルパ、テンソルのCPU↔GPU変換処理など.

使用例）
	import dnn
	from dnn import chainer
	from dnn import F
	from dnn import L
	from dnn import Variable
	import chainer.computational_graph as ccg

	dnn.startup(-1) # 0番のGPU使用
	xp = dnn.xp # numpy または cupy

	def separate(x, out):
		shape = x.shape
		x = x.reshape((2, shape[0], shape[1] // 2))
		return out[0](x[0]), out[1](x[1])

	def concat(x, out):
		h = F.concat((x[0], x[1]), axis=1)
		return out[0](h)

	m = dnn.Model(chainer.optimizers.Adam()) # とりあえず Model 作る
	g = m.gate(separate, m.linear(32, 32).relu().linear(32, 32).relu()) # 全結合層生成して relu 活性化関数セットしたものを入力し、それを２つに分ける Gate 作る
	g = m.gate(concat, g.linear(16, 16), g.linear(16, 16)) # ２つの入力を１つに結合する Gate 作る
	m.assign_output(g.linear(32, 32)) # 全結合層を１つ作ってそれを出力とする
	m.build()

	# 所定のデバイスメモリに転送
	m = dnn.to_xp(m)

	# とりあえず入力値を単純に10倍にするネットを目標とする
	for i in range(10000):
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


class ModelNode:
	"""モデルを構成するノード、レイヤを生成する機能を提供するクラス.

	Args:
		parent_model: 所有者となる SubModel.
	"""

	def __init__(self, parent_model):
		self.parent_model = parent_model # 親 SubModel
		self.name = None # 所有者 SubModel 内での自身の名前
		self.output_variable_ids = {} # 出力レイヤーをキーとしたローカル変数ID列、None がキーにする場合全てのレイヤーに優先する
		self.is_built = False # 既にビルドされたかどうか
		self.is_uner_build = False # ビルド途中かどうか

		# ルート SubModel の取得
		node = self
		while True:
			p = node.parent_model
			if p is None:
				break
			node = p
		self.root_model = node # ルート SubModel

	def __str__(self):
		return self.name

	def __repr__(self):
		return self.name

	def get_full_name(self):
		"""ルート SubModel からのフル名称を取得する.
		"""
		names = [self.name]
		node = self.parent_model
		while node is not None:
			nm = node.name
			if nm is not None and len(nm) != 0:
				names.append(nm)
			node = node.parent_model
		return '.'.join(reversed(names))

	def get_dot_name(self):
		"""dot 内での名称(エンティティ名)を取得する.
		"""
		names = [self.name]
		node = self.parent_model
		while node is not None:
			nm = node.name
			if nm is not None and len(nm) != 0:
				names.append(nm)
			node = node.parent_model
		return '_'.join(reversed(names))

	def layer(self, link, input=None):
		"""自分を入力とする出力 Layer を生成する.

		Args:
			link: レイヤーの計算のメインとなる Link.
			input: None.

		Returns:
			Layer.
		"""
		return self.parent_model.layer(link, self)

	def model(self, input=None):
		"""自分を入力とする出力 SubModel を生成する.

		Args:
			input: None.

		Returns:
			SubModel.
		"""
		return self.parent_model.model(self)

	def get_output_variable_id(self, layer):
		"""このレイヤから指定レイヤへの出力を一時的に保持するローカル変数IDを取得する.

		Args:
			layer: 出力先レイヤ.

		Returns:
			ローカル変数ID.
		"""
		if None in self.output_variable_ids:
			return self.output_variable_ids[None]
		if layer in self.output_variable_ids:
			return self.output_variable_ids[layer]
		else:
			id = self.root_model.new_local_variable_id()
			self.output_variable_ids[layer] = id
			return id

	def build(self, depth):
		"""コード生成を行う.

		Args:
			depth: 関数呼び出し深さ.
		"""

	def collect_gate(self, output, depth, gate_set):
		"""入力側の直近の複数出力 Gate を集める.

		Args:
			output: 呼び出し元出力側レイヤ.
			depth: 関数呼び出し深さ.
			gate_set: Gate 収集先 set.
		"""

	def linear(self, in_size, out_size=None, nobias=False, initialW=None, initial_bias=None):
		return self.layer(L.Linear(in_size, out_size, nobias, initialW, initial_bias))

	def conv2d(self, in_channels, out_channels, ksize=None, stride=1, pad=0, nobias=False, initialW=None, initial_bias=None, **kwargs):
		return self.layer(L.Convolution2D(in_channels, out_channels, ksize, stride, pad, nobias, initialW, initial_bias, **kwargs))

	def deconv2d(self, in_channels, out_channels, ksize=None, stride=1, pad=0, nobias=False, outsize=None, initialW=None, initial_bias=None, **kwargs):
		return self.layer(L.Deconvolution2D(in_channels, out_channels, ksize, stride, pad, nobias, outsize, initialW, initial_bias, **kwargs))

	def batchnorm(self, size=None, decay=0.9, eps=2e-5, dtype=None, use_gamma=True, use_beta=True, initial_gamma=None, initial_beta=None, axis=None, initial_avg_mean=None, initial_avg_var=None):
		return self.layer(L.BatchNormalization(size, decay, eps, dtype, use_gamma, use_beta, initial_gamma, initial_beta, axis, initial_avg_mean, initial_avg_var))


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


class Layer(Chain, ModelNode, FuncsHolder):
	"""レイヤ、学習対象の重み、活性化関数、入力レイヤへの参照を持つ.

	Args:
		parent_model: 所有者となる SubModel.
		link: 学習対象の重みを持つ chainer.links.Convolution2D など chainer.link.Link を継承するもの.
		input: 入力レイヤ、Layer を継承するものまたは None.
	"""

	def __init__(self, parent_model, link, input=None):
		if input is not None and not isinstance(input, Layer) and not isinstance(input, Gate):
			raise TypeError("Invalid argument. 'input' must be Layer or Gate.")
		if not isinstance(link, Link):
			raise TypeError('Cannot register a non-link object as a child')

		Chain.__init__(self)
		ModelNode.__init__(self, parent_model)
		FuncsHolder.__init__(self)

		self.input = input
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
		if self.is_built:
			return
		if self.is_uner_build:
			raise Exception()
		self.is_uner_build = True

		# 入力側を先にビルドと入力値を保持している変数ID取得
		if self.input is None:
			in_id = 0
		else:
			self.input.build(depth + 1)
			in_id = self.input.get_output_variable_id(self)
		in_name = self.root_model.get_local_variable_name(in_id)
		self.root_model.del_local_variable_id(in_id) # 名前取得済みなので削除

		# 出力値の代入先変数確保
		out_id = self.root_model.new_local_variable_id()
		out_name = self.root_model.get_local_variable_name(out_id)
		self.output_variable_ids[None] = out_id

		# コードの追加
		self_name = self.get_full_name()
		self.root_model.code.append('\t{} = self.{}({})'.format(out_name, self_name, in_name))

		dot_name = self.get_dot_name()
		if len(self.funcs) != 0:
			self.root_model.dot_code.append('{} [label="{}\\n{}"];'.format(dot_name, self_name, ', '.join([f.__name__ for f in self.funcs])))
		else:
			self.root_model.dot_code.append('{} [label="{}"];'.format(dot_name, self_name))
		if self.input is not None:
			self.root_model.dot_code.append('{} -> {} [label="{}"];'.format(self.input.get_dot_name(), dot_name, in_name))

		self.is_built = True
		self.is_uner_build = False

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


class Gate(ModelNode):
	"""指定値を１～複数の出力レイヤーで変換する、複数対複数のレイヤーをくっつける役割を持つ.

	Args:
		parent_model: 所有者となる SubModel.
		func: 引数を入力レイヤーを通しさらに出力レイヤーを通す処理、 def func(x, output_layers).
		inputs: 入力レイヤー列.
	"""

	def __init__(self, parent_model, func, *inputs):
		for i in inputs:
			if not isinstance(i, Layer):
				raise TypeError("Invalid argument. 'inputs' element must be Layer.")

		ModelNode.__init__(self, parent_model)

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

	def layer(self, link, input=None):
		"""自分を入力とする出力 Layer を生成する.

		Args:
			link: レイヤーの計算のメインとなる Link.
			input: None.

		Returns:
			Layer.
		"""
		layer = ModelNode.layer(self, link)
		self.outputs.append(layer)
		return layer

	def build(self, depth):
		"""コード生成を行う.

		Args:
			depth: 関数呼び出し深さ.
		"""
		if self.is_built:
			return
		if self.is_uner_build:
			raise Exception()
		self.is_uner_build = True

		# 入力側にある複数出力の Gate を収集し、呼び出しが早い方からビルドしていく
		depth += 1

		gate_set = set()
		for i in self.inputs:
			i.collect_gate(self, depth, gate_set)
		glues_list = [g for g in gate_set]
		glues_list.sort(key=lambda g: g[1])
		for g in reversed(glues_list):
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
		in_names = ', '.join([self.root_model.get_local_variable_name(id) for id in in_ids])
		out_names = ', '.join([self.root_model.get_local_variable_name(id) for id in out_ids])
		self.root_model.code.append('\t{} = self.{}(({}))'.format(out_names, self_name, in_names))

		dot_name = self.get_dot_name()
		self.root_model.dot_code.append('{} [label="{}\\n{}"];'.format(dot_name, self_name, self.func.__name__))
		for i in self.inputs:
			self.root_model.dot_code.append('{} -> {} [label="{}"];'.format(i.get_dot_name(), dot_name, self.root_model.get_local_variable_name(i.get_output_variable_id(self))))

		# func_code = inspect.getsource(self.func)
		# func_code = func_code.replace('\n', '\\n')
		# func_code = func_code.replace('\t', '\\t')
		# self.root_model.dot_code.append('{} [label="{}"];'.format(dot_name, func_code))

		for id in in_ids:
			self.root_model.del_local_variable_id(id) # 名前取得済みなので削除

		self.is_built = True
		self.is_uner_build = False

	def collect_gate(self, output, depth, gate_set):
		"""入力側の直近の複数出力 Gate を集める.

		Args:
			output: 呼び出し元出力側レイヤ.
			depth: 関数呼び出し深さ.
			gate_set: Gate 収集先 set.
		"""
		if 2 <= len(self.outputs):
			gate_set.add((self, depth))


class SubModel(Chain, ModelNode):
	"""モデルのサブセット、layer()、gate() によりレイヤを生成し保持する.

	Args:
		parent_model: 親となる Model.
		input: 入力レイヤ、Layer を継承するものまたは None.
	"""

	def __init__(self, parent_model, input=None):
		if input is not None and not isinstance(input, Layer) and not isinstance(input, Gate):
			raise TypeError("Invalid argument. 'input' must be Layer or Gate.")

		Chain.__init__(self)
		ModelNode.__init__(self, parent_model)

		self.parent_model = parent_model
		self.input = input
		self.layers = []
		self.glues = []
		self.submodels = []

	def layer(self, link, input=None):
		"""レイヤーを生成する.

		Args:
			link: レイヤーの計算のメインとなる Link.
			input: 入力となる Layer または None.
		"""
		if input is None and self.input is not None:
			input = self.input
			self.input = None
		name = 'layer_' + str(len(self.layers)) + '_' + type(link).__name__
		layer = Layer(self, link, input)
		self.add_link(name, layer)
		self.layers.append(layer)
		return layer

	def gate(self, func, *inputs):
		"""レイヤ同士を結合する Gate を作成する.

		Args:
			func: 入力値を出力レイヤーに通す処理、 def func(gate, x, output_layers).
			inputs: 入力レイヤー列.

		Returns:
			Gate.
		"""
		gate = Gate(self, func, *inputs)
		name = 'gate_' + str(len(self.glues))
		setattr(self, name, gate)
		gate.name = name
		self.glues.append(gate)
		return gate

	def model(self, input=None):
		"""子の SubModel を作成する.

		Args:
			input: 入力レイヤー.

		Returns:
			SubModel.
		"""
		if input is None and self.input is not None:
			input = self.input
			self.input = None
		name = 'model_' + str(len(self.submodels))
		model = SubModel(self, input)
		self.add_link(name, model)
		self.submodels.append(model)
		return model


class Model(SubModel):
	"""モデル、 input() 、 layer() 、 output() により複数のレイヤを保持する事が可能.

	Args:
		optimizer: 重み最適化処理方法、 chainer.optimizer.Optimizerr を継承するもの.
	"""

	def __init__(self, optimizer):
		SubModel.__init__(self, None)

		self.optimizer = optimizer
		self.output_layer = None
		self.local_variable_ids = set()
		self.code = []
		self.dot_code = []
		self.prediction = None

	def assign_output(self, output_layer):
		"""出力レイヤを割り当てる.

		Args:
			output_layer: 出力とする Layer.
		"""
		self.output_layer = output_layer

	def build(self):
		"""モデルの推論用関数をビルドする.
		"""
		self.code.append('def prediction(self, x):')
		self.dot_code.insert(0, 'digraph {\n')
		self.output_layer.build(0)
		self.dot_code.append('}')
		self.dot_code = '\n'.join(self.dot_code)
		self.code.append('\treturn {}\n'.format(self.get_local_variable_name(self.output_layer.get_output_variable_id(None))))
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
