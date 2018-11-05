"""ディープラーニング関係ヘルパ、テンソルのCPU↔GPU変換処理など.

使用例）
	import dnn
	from dnn import chainer
	from dnn import F
	from dnn import L
	from dnn import Variable

	dnn.startup(0) # 0番のGPU使用
	xp = dnn.xp

	def separate(g, x, out):
		shape = x.shape
		x = x.reshape((2, shape[0], shape[1] // 2))
		return out[0](x[0]), out[1](x[1])

	def concat(g, x, out):
		h = F.concat((x[0], x[1]), axis=1)
		return out[0](h)

	m = dnn.Model(chainer.optimizers.Adam())
	l = m.input(L.Linear(32, 32))
	g = m.glue([l], separate) # ２つに分ける
	g = m.glue([g.output(L.Linear(16, 16)), g.output(L.Linear(16, 16))], concat) # ２つを１つに結合する
	m.assign_output(g.output(L.Linear(32, 32)))
	m.build()

	# 計算速度のためGPUメモリに転送
	m = dnn.to_gpu(m)

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
# import inspect
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
		model: このレイヤをメンバ変数として持つモデル.
		input: 入力レイヤ、Layer を継承するものまたは None.
		link: 学習対象の重みを持つ chainer.links.Convolution2D など chainer.link.Link を継承するもの.
		activator: 活性化関数、chainer.functions.relu など.
	"""

	def __init__(self, model, input, link, activator):
		if input is not None and not isinstance(input, Layer) and not isinstance(input, Glue):
			raise TypeError("Invalid argument. 'input' must be Layer or Glue.")
		if not isinstance(link, Link):
			raise TypeError('Cannot register a non-link object as a child')
		super().__init__()
		self.model = model
		self.input = input
		self.output_variable_id = None
		self.is_uner_build = False
		self.is_built = False
		with self.init_scope():
			self.link = link
			self.activator = activator

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
		"""入力側の直近の複数出力 Glue を集める.

		Args:
			output: 呼び出し元出力側レイヤ.
			depth: 関数呼び出し深さ.
			glue_set: Glue 収集先 set.
		"""
		if self.input is None:
			return
		self.input.collect_glue(self, depth + 1, glue_set)


class Glue:
	"""指定値を１～複数の出力レイヤーで変換する、複数対複数のレイヤーをくっつける役割を持つ.

	使用例）
		m = dnn.Model(chainer.optimizers.Adam())
		l = m.input(L.Linear(32, 32))
		g = m.glue([l], separate) # ２つに分ける
		g = m.glue([g.output(L.Linear(16, 16)), g.output(L.Linear(16, 16))], concat) # ２つを１つに結合する
		m.assign_output(g.output(L.Linear(32, 32)))
		m.build()

	Args:
		model: このレイヤをメンバ変数として持つモデル.
		inputs: 入力レイヤーリスト.
		func: 引数を入力レイヤーを通しさらに出力レイヤーを通す処理、 def func(glue, x, output_layers).
	"""

	def __init__(self, model, inputs, func):
		for i in inputs:
			if not isinstance(i, Layer):
				raise TypeError("Invalid argument. 'inputs' element must be Layer.")

		self.model = model
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
		return self.func(self, x, self.outputs)

	def output(self, link, activator=None):
		"""この Glue の出力レイヤを作成する.

		Args:
			link: 学習対象の重みを持つ chainer.links.Convolution2D など chainer.link.Link を継承するもの.
			activator: 活性化関数、chainer.functions.relu など.

		Returns:
			出力に結び付けられた Layer.
		"""
		layer = self.model.layer(self, link, activator)
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
		"""入力側の直近の複数出力 Glue を集める.

		Args:
			output: 呼び出し元出力側レイヤ.
			depth: 関数呼び出し深さ.
			glue_set: Glue 収集先 set.
		"""
		if 2 <= len(self.outputs):
			glue_set.add((self, depth))


class Model(chainer.Chain):
	"""モデル、 input() 、 layer() 、 output() により複数のレイヤを保持する事が可能.

	Args:
		optimizer: 重み最適化処理方法、 chainer.optimizer.Optimizerr を継承するもの.
	"""

	def __init__(self, optimizer):
		super().__init__()
		self.optimizer = optimizer
		self.input_layer = None
		self.output_layer = None
		self.nodes = set()
		self.layers = []
		self.glues = []
		self.local_variable_ids = set()
		self.code = []
		self.dot_code = []
		self.prediction = None

	def input(self, link, activator=None):
		"""入力レイヤを作成する.

		Args:
			link: 学習対象の重みを持つ chainer.links.Convolution2D など chainer.link.Link を継承するもの.
			activator: 活性化関数、chainer.functions.relu など.

		Returns:
			Layer.
		"""
		del self.input_layer
		layer = Layer(self, None, link, activator)
		self.add_link('input_layer', layer)
		return layer

	def assign_output(self, output_layer):
		"""出力レイヤを割り当てる.

		Args:
			output_layer: 出力とする Layer.
		"""
		del self.output_layer
		self.add_link('output_layer', output_layer)

	def layer(self, input_layer, link, activator=None):
		"""中間レイヤを作成する.

		Args:
			input_layer: 入力側の Layer を継承する入力レイヤ.
			link: 学習対象の重みを持つ chainer.links.Convolution2D など chainer.link.Link を継承するもの.
			activator: 活性化関数、chainer.functions.relu など.

		Returns:
			Layer.
		"""
		name = 'layer_' + str(len(self.layers))
		layer = Layer(self, input_layer, link, activator)
		self.add_link(name, layer)
		self.layers.append(layer)
		self.nodes.add(layer)
		return layer

	def glue(self, input_layers, func):
		"""レイヤ同士を結合する Glue を作成する.

		使用例）
			m = dnn.Model(chainer.optimizers.Adam())
			l = m.input(L.Linear(32, 32))
			g = m.glue([l], separate) # ２つに分ける
			g = m.glue([g.output(L.Linear(16, 16)), g.output(L.Linear(16, 16))], concat) # ２つを１つに結合する
			m.assign_output(g.output(L.Linear(32, 32)))
			m.build()

		Args:
			input_layers: 入力側の Layer を継承する入力レイヤのリスト.
			func: 引数を入力レイヤーを通しさらに出力レイヤーを通す処理、 def func(glue, x, output_layers).

		Returns:
			Glue.
		"""
		glue = Glue(self, input_layers, func)
		name = 'glue_' + str(len(self.glues))
		setattr(self, name, glue)
		glue.name = name
		self.glues.append(glue)
		self.nodes.add(glue)
		return glue

	def build(self):
		"""モデルの推論用関数をビルドする.
		"""
		self.code.append('def prediction(self, x):')
		self.dot_code.insert(0, 'digraph {\n')
		self.output_layer.build(0)
		self.dot_code.append('}')
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
