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


class LayerFactory:
	"""出力側のレイヤを生成する機能を提供するクラス.

	Args:
		model: Layer の所有者となる Model.
	"""

	def __init__(self, model):
		self.model = model

	def layer(self, link, input=None):
		"""自分を入力とする出力 Layer を生成する.

		Args:
			link: レイヤーの計算のメインとなる Link.
			input: None.

		Returns:
			Layer.
		"""
		return self.model.layer(link, self)

	def linear(self, in_size, out_size=None, nobias=False, initialW=None, initial_bias=None):
		return self.layer(L.Linear(in_size, out_size, nobias, initialW, initial_bias))

	def conv2d(self, in_channels, out_channels, ksize=None, stride=1, pad=0, nobias=False, initialW=None, initial_bias=None, **kwargs):
		return self.layer(L.Convolution2D(in_channels, out_channels, ksize, stride, pad, nobias, initialW, initial_bias, **kwargs))

	def deconv2d(self, in_channels, out_channels, ksize=None, stride=1, pad=0, nobias=False, outsize=None, initialW=None, initial_bias=None, **kwargs):
		return self.layer(L.Deconvolution2D(in_channels, out_channels, ksize, stride, pad, nobias, outsize, initialW, initial_bias, **kwargs))

	def batchnorm(self, size=None, decay=0.9, eps=2e-5, dtype=None, use_gamma=True, use_beta=True, initial_gamma=None, initial_beta=None, axis=None, initial_avg_mean=None, initial_avg_var=None):
		return self.layer(L.BatchNormalization(size, decay, eps, dtype, use_gamma, use_beta, initial_gamma, initial_beta, axis, initial_avg_mean, initial_avg_var))


class ActivatorHolder:
	"""活性化関数を生成し保持する機能を提供するクラス.
	"""

	def __init__(self):
		self.activator = None

	def relu(self):
		self.activator = F.relu
		return self

	def leaky_relu(self):
		self.activator = F.leaky_relu
		return self

	def sigmoid(self):
		self.activator = F.sigmoid
		return self


class Layer(Chain, LayerFactory, ActivatorHolder):
	"""レイヤ、学習対象の重み、活性化関数、入力レイヤへの参照を持つ.

	Args:
		model: このレイヤをメンバ変数として持つモデル.
		link: 学習対象の重みを持つ chainer.links.Convolution2D など chainer.link.Link を継承するもの.
		input: 入力レイヤ、Layer を継承するものまたは None.
	"""

	def __init__(self, model, link, input=None):
		if input is not None and not isinstance(input, Layer) and not isinstance(input, Gate):
			raise TypeError("Invalid argument. 'input' must be Layer or Gate.")
		if not isinstance(link, Link):
			raise TypeError('Cannot register a non-link object as a child')

		Chain.__init__(self)
		LayerFactory.__init__(self, model)
		ActivatorHolder.__init__(self)

		self.input = input
		self.output_variable_id = None
		self.is_uner_build = False
		self.is_built = False
		with self.init_scope():
			self.link = link

	def __str__(self):
		return self.name

	def __repr__(self):
		return self.name

	def __call__(self, x=None):
		"""指定値をレイヤーで変換する.

		Args:
			x: 入力値.

		Returns:
			変換後の値.
		"""
		x = self.link(x)
		if self.activator is not None:
			x = self.activator(x)
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

		if self.input is None:
			in_id = 0
		else:
			self.input.build(depth + 1)
			in_id = self.input.get_output_variable_id(self)
		in_name = self.model.get_local_variable_name(in_id)
		self.model.del_local_variable_id(in_id)

		out_id = self.model.new_local_variable_id()
		out_name = self.model.get_local_variable_name(out_id)
		self.output_variable_id = out_id

		self.model.code.append('\t{} = self.{}({})'.format(out_name, self.name, in_name))
		if self.activator is not None:
			self.model.dot_code.append('{} [label="{}\\n{}"];'.format(self.name, self.name, self.activator.__name__))
		if self.input is not None:
			self.model.dot_code.append('{} -> {} [label="{}"];'.format(self.input.name, self.name, in_name))

		self.is_built = True
		self.is_uner_build = False

	def get_output_variable_id(self, layer):
		"""このレイヤから指定レイヤへの出力を一時的に保持するローカル変数IDを取得する.

		Args:
			layer: 出力先レイヤ.

		Returns:
			ローカル変数ID.
		"""
		if self.output_variable_id is not None:
			return self.output_variable_id
		else:
			self.output_variable_id = self.model.new_local_variable_id()
			return self.output_variable_id

	def collect_glue(self, output, depth, glue_set):
		"""入力側の直近の複数出力 Gate を集める.

		Args:
			output: 呼び出し元出力側レイヤ.
			depth: 関数呼び出し深さ.
			glue_set: Gate 収集先 set.
		"""
		if self.input is None:
			return
		self.input.collect_glue(self, depth + 1, glue_set)


class Gate(LayerFactory):
	"""指定値を１～複数の出力レイヤーで変換する、複数対複数のレイヤーをくっつける役割を持つ.

	Args:
		model: このレイヤをメンバ変数として持つモデル.
		func: 引数を入力レイヤーを通しさらに出力レイヤーを通す処理、 def func(x, output_layers).
		inputs: 入力レイヤー列.
	"""

	def __init__(self, model, func, *inputs):
		for i in inputs:
			if not isinstance(i, Layer):
				raise TypeError("Invalid argument. 'inputs' element must be Layer.")

		LayerFactory.__init__(self, model)

		self.name = None
		self.inputs = inputs
		self.func = func
		self.outputs = []
		self.output_variable_ids = {}
		self.is_built = False
		self.is_uner_build = False

	def __str__(self):
		return self.name

	def __repr__(self):
		return self.name

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
		layer = LayerFactory.layer(self, link)
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

		depth += 1

		glue_set = set()
		for i in self.inputs:
			i.collect_glue(self, depth, glue_set)
		glues_list = [g for g in glue_set]
		glues_list.sort(key=lambda g: g[1])
		for g in reversed(glues_list):
			g[0].build(g[1])

		in_ids = []
		for i in self.inputs:
			i.build(depth)
			in_ids.append(i.get_output_variable_id(self))

		out_ids = [self.get_output_variable_id(o) for o in self.outputs]

		in_names = ', '.join([self.model.get_local_variable_name(id) for id in in_ids])
		out_names = ', '.join([self.model.get_local_variable_name(id) for id in out_ids])
		self.model.code.append('\t{} = self.{}(({}))'.format(out_names, self.name, in_names))

		# func_code = inspect.getsource(self.func)
		# func_code = func_code.replace('\n', '\\n')
		# func_code = func_code.replace('\t', '\\t')
		# self.model.dot_code.append('{} [label="{}"];'.format(self.name, func_code))

		for i in self.inputs:
			self.model.dot_code.append('{} -> {} [label="{}"];'.format(i.name, self.name, self.model.get_local_variable_name(i.get_output_variable_id(self))))

		for id in in_ids:
			self.model.del_local_variable_id(id)

		self.is_built = True
		self.is_uner_build = False

	def get_output_variable_id(self, layer):
		"""このレイヤから指定レイヤへの出力を一時的に保持するローカル変数IDを取得する.

		Args:
			layer: 出力先レイヤ.

		Returns:
			ローカル変数ID.
		"""
		if layer in self.output_variable_ids:
			return self.output_variable_ids[layer]
		else:
			id = self.model.new_local_variable_id()
			self.output_variable_ids[layer] = id
			return id

	def collect_glue(self, output, depth, glue_set):
		"""入力側の直近の複数出力 Gate を集める.

		Args:
			output: 呼び出し元出力側レイヤ.
			depth: 関数呼び出し深さ.
			glue_set: Gate 収集先 set.
		"""
		if 2 <= len(self.outputs):
			glue_set.add((self, depth))


class Model(Chain, LayerFactory):
	"""モデル、 input() 、 layer() 、 output() により複数のレイヤを保持する事が可能.

	Args:
		optimizer: 重み最適化処理方法、 chainer.optimizer.Optimizerr を継承するもの.
	"""

	def __init__(self, optimizer):
		Chain.__init__(self)
		LayerFactory.__init__(self, self)

		self.optimizer = optimizer
		self.output_layer = None
		self.layers = []
		self.glues = []
		self.local_variable_ids = set()
		self.code = []
		self.dot_code = []
		self.prediction = None

	def layer(self, link, input=None):
		"""入力レイヤーを生成する.

		Args:
			link: レイヤーの計算のメインとなる Link.
			input: 入力となる Layer または None.
		"""
		if input is None:
			name = 'input_layer' + '_' + type(link).__name__
		else:
			name = 'layer_' + str(len(self.layers)) + '_' + type(link).__name__
		layer = Layer(self, link, input)
		self.add_link(name, layer)
		self.layers.append(layer)
		return layer

	def assign_output(self, output_layer):
		"""出力レイヤを割り当てる.

		Args:
			output_layer: 出力とする Layer.
		"""
		self.add_link('output_layer_' + type(output_layer.link).__name__, output_layer)
		self.output_layer = output_layer

	def gate(self, func, *inputs):
		"""レイヤ同士を結合する Gate を作成する.

		Args:
			func: 引数を入力レイヤーを通しさらに出力レイヤーを通す処理、 def func(gate, x, output_layers).
			inputs: 入力レイヤー列.

		Returns:
			Gate.
		"""
		gate = Gate(self, func, *inputs)
		name = 'glue_' + str(len(self.glues))
		setattr(self, name, gate)
		gate.name = name
		self.glues.append(gate)
		return gate

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
