"""ディープラーニング関係ヘルパ、テンソルのCPU↔GPU変換処理など.
"""
import numpy as np, chainer, chainer.functions as F, chainer.links as L
from chainer import Variable
from chainer.link import Link
xp = None
cp = None
cuda = None
test = False

class Layer(chainer.Chain):
	"""レイヤ、学習対象の重み、活性化関数、入力レイヤへの参照を持つ.

	Args:
		input: 入力レイヤ、Layer を継承するものまたは None.
		link: 学習対象の重みを持つ chainer.links.Convolution2D など chainer.link.Link を継承するもの.
		activator: 活性化関数、chainer.functions.relu など.
	"""

	def __init__(self, input, link, activator):
		if not isinstance(link, Layer):
			raise TypeError("Invalid argument. 'input' must be Layer.")
		if not isinstance(link, Link):
			raise TypeError('Cannot register a non-link object as a child')
		super().__init__()
		self.model = None
		self.input = input
		with self.init_scope():
			self.link = link
			self.activator = activator

	def __call__(self, x=None):
		"""指定値をレイヤーで変換する.

		Args:
			x: 入力値.

		Returns:
			変換後の値.
		"""
		if self.input is not None:
			x = self.input(x)
		x = self.link(x)
		if self.activator is not None:
			x = self.activator(x)
		return x

	def pretrace(self, caller, depth, code):
		if self.input is not None:
			return self.input.pretrace(caller, depth + 1, code)
		else:
			return None

class Lambda:
	"""ラムダ式を実行する.

	Lambda(lambda a, x: a.pl(x) * 2, pl=prev_layer) の様に初期化し、引数 args に渡したものが a に渡る。

	Args:
		func: 計算処理、lambda a, x: x * a.k の様に args に指定した値と入力値を受け取り計算を行う処理.
		args: 係数辞書、 k=2 の様に指定してメンバ変数を作りそれが func の x に渡る.
	"""
	def __init__(self, func, **args):
		if not callable(func):
			raise TypeError("Invalid argument. 'func' must be callable.")
		self.func = func
		for k, v in args.items():
			setattr(self, k, v)

	def __call__(self, x):
		"""計算を実行する.

		Args:
				x: 入力値.

		Returns:
				結果.
		"""
		return self.func(self, x)

class Glue:
	"""指定値を１～複数の出力レイヤーで変換する、複数対複数のレイヤーをくっつける役割を持つ.

	使用例）
		m = dnn.Model(chainer.optimizers.Adam())
		def saparate(glue, x, out):
			x = x.reshape(2, 1, 16)
			return [out[0](x[0]), out[1](x[1])]
		def concat(glue, x, out):
			h = F.concat((x[0], x[1]), axis=1)
			return out[0](h)
		l1 = m.input(L.Linear(32, 32))
		l2 = m.layer(None, L.Linear(16, 16))
		l3 = m.layer(None, L.Linear(16, 16))
		l4 = m.layer(None, L.Linear(32, 32))
		g1 = m.glue([l1], [l2, l3], saparate)
		g2 = m.glue([l2, l3], [l4], concat)

	Args:
		inputs: 入力レイヤーリスト.
		outputs: 出力レイヤーリスト.
		func: 引数を入力レイヤーを通しさらに出力レイヤーを通す処理.
	"""
	def __init__(self, inputs, outputs, func):
		self.model = None
		self.inputs = inputs
		self.outputs = outputs
		self.func = func

		self.outputs_set = {o for o in outputs}
		self.input_len  = len(inputs)
		self.output_len = len(outputs)
		self.traced_outputs = set()
		self.is_traced_all = False
		for layer in outputs:
			layer.input = self

	def __call__(self, x):
		"""指定値をレイヤーで変換する.

		Args:
			x: 入力値.

		Returns:
			変換後の値.
		"""
		return self.func(self, x, self.inputs, self.outputs)

	def pretrace(self, caller, depth, code):
		if 2 <= self.output_len:
			# 出力が複数あるなら全出力（呼び出し元）からの呼び出し完了後に呼び出してもらう
			self.traced_outputs.add(caller)
			self.is_traced_all = self.outputs_set.issubset(self.traced_outputs)
			return (self, depth)
		else:
			depth += 1
			glues = set()
			for input in self.inputs:
				g = input.pretrace(self, depth, code)
				if g is not None:
					glues.add(g)

class Model(chainer.Chain):
	"""モデル、 input() 、 layer() 、 output() により複数のレイヤを保持する事が可能.

	Args:
		optimizer: 重み最適化処理方法、 chainer.optimizer.Optimizerr を継承するもの.
	"""

	def __init__(self, optimizer):
		super().__init__()
		self.input_layer_name_prefix = 'input_layer_'
		self.input_layer_name_count = 0
		self.layer_name_prefix = 'layer_'
		self.layer_name_count = 0
		self.output_layer = None
		self.optimizer = optimizer

	def input(self, link, activator=None):
		"""入力レイヤを作成する.

		Args:
			link: 学習対象の重みを持つ chainer.links.Convolution2D など chainer.link.Link を継承するもの.
			activator: 活性化関数、chainer.functions.relu など.

		Returns:
			レイヤ.
		"""
		input_layer_name_count = self.input_layer_name_count
		input_layer_name_count += 1
		name = self.input_layer_name_prefix + str(input_layer_name_count)
		layer = Layer(None, link, activator)
		self.add_link(name, layer)
		self.input_layer_name_count = input_layer_name_count
		return layer

	def layer(self, input_layer, link, activator=None):
		"""中間レイヤを作成する.

		Args:
			input_layer: Layer を継承する入力レイヤ.
			link: 学習対象の重みを持つ chainer.links.Convolution2D など chainer.link.Link を継承するもの.
			activator: 活性化関数、chainer.functions.relu など.

		Returns:
			レイヤ.
		"""
		layer_name_count = self.layer_name_count
		layer_name_count += 1
		name = self.layer_name_prefix + str(layer_name_count)
		layer = Layer(input_layer, link, activator)
		self.add_link(name, layer)
		self.layer_name_count = layer_name_count
		return layer

	def output(self, input_layer, link, activator=None):
		"""出力レイヤを作成する.

		Args:
			input_layer: Layer を継承する入力レイヤ.
			link: 学習対象の重みを持つ chainer.links.Convolution2D など chainer.link.Link を継承するもの.
			activator: 活性化関数、chainer.functions.relu など.

		Returns:
			レイヤ.
		"""
		layer = Layer(input_layer, link, activator)
		del self.output_layer
		with self.init_scope():
			self.output_layer = layer
		return layer

	def __call__(self, x):
		"""計算を実行する.

		Args:
			x: 入力値.

		Returns:
			結果.
		"""
		return self.output_layer(x)

	def compile(self):
		"""モデルの構築が完了後、計算準備の最終処理を行う.
		"""
		self.optimizer.setup(self)


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
