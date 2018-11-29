"""ディープラーニング関係ヘルパ、テンソルのCPU↔GPU変換処理など.

使用例）
	import math
	import matplotlib.pyplot as plt
	import dnn
	from dnn import np
	from dnn import chainer
	from dnn import F
	from dnn import Variable

	dnn.startup(0) # 0番のGPU使用
	xp = dnn.xp # numpy または cupy
	itr = 0

	figx_in_pred, ax_in_pred = plt.subplots()

	def calc_width_height(length):
		w = int(math.sqrt(length))
		while length % w != 0:
			w += 1
		h = length // w
		return (h, w)

	def plot(self, ax):
		def proc(x):
			img = dnn.to_cpu(x.data[0])
			img = img.reshape(calc_width_height(img.size))
			ax.cla()
			ax.imshow(img)
			ax.set_title("frame {}".format(itr))
			return x
		return self.funcs('plot', proc)

	# Node にメソッド追加してみる
	dnn.Node.plot = plot

	# モデル構築
	batch = 1
	model = dnn.Model(chainer.optimizers.Adam())
	with model.module as m:
		m\
		.conv2d(1, 1, 3, 1, 1).prelu()\
		.conv2d(1, 1, 3, 1, 1).prelu()\
		.conv2d(1, 1, 3, 1, 1).prelu()\
		.reshape((batch, 1024))\
		.dense(1024, 512).prelu()\
		.dense(512, 512).prelu()\
		.dense(512, 1024).prelu()\
		.reshape((batch, 1, 32, 32)).prelu()\
		.conv2d(1, 1, 3, 1, 1).prelu()\
		.conv2d(1, 1, 3, 1, 1).prelu()\
		.conv2d(1, 1, 3, 1, 1)

	model.build('dnn_test_prediction.py')

	# モデルデータが保存済みなら読み込み
	dnn.load_if_exists('dnn_test.h5', model)

	with open("dnn_test.dot", mode='w') as f:
		f.write(model.dot_code)

	# 所定のデバイスメモリに転送
	model = dnn.to_device(model)

	figx, ax = plt.subplots()

	# とりあえず入力値を単純に10倍にするネットを目標とする
	for i in range(10000):
		model.zero_grad()

		x = Variable(xp.random.uniform(0, 1, (1, 1, 32, 32)).astype(xp.float32))
		y = model(x)
		t = x * 10

		img = np.concatenate((dnn.to_cpu(x.data[0, 0]), dnn.to_cpu(y.data[0, 0])), axis=1)
		ax.cla()
		ax.imshow(img)
		ax.set_title("frame {}".format(i))

		plt.pause(0.01)

		loss = F.mean_squared_error(y, t)
		loss.backward()
		print(loss)
		model.step()

		itr += 1

	# 保存時はCPUメモリにしないとだめ
	dnn.save('dnn_test.h5', model)
"""
import os
from importlib.machinery import SourceFileLoader
import h5py
import numpy as np, chainer, chainer.functions as F, chainer.links as L
from chainer import Variable
from chainer import Chain
from chainer.link import Link
from chainer import Optimizer
from chainer import cuda
from chainer import optimizers
import cupy as cp

xp = None
test = False
dtype = np.float32


class Node:
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
		self.is_observer = False # 途中でプロットするなど出力が無くても良いノードなら True
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

	def get_model(self):
		"""この Node が属する Model の取得.
		"""
		return self.root.get_model()

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

		if self == self.root:
			return 'root'

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
		return '{}\\n{}'.format(self.get_full_name(), self.dot_param)

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

	def trainables(self):
		"""学習可能な Link オブジェクトを列挙する.

		Returns:
			Link を列挙するジェネレータ.
		"""
		if isinstance(self, Link):
			for pl in self.namedlinks():
				l = pl[1]
				if 'W' in l.__dict__ and isinstance(l.W, Variable):
					yield pl

	def assign_trainables(self, trainables):
		"""学習結果をアサインする.
		"""
		my_trainables = {kv[0]: kv[1] for kv in self.trainables()}
		trainables = {kv[0]: kv[1] for kv in trainables}

		if len(my_trainables) != len(trainables):
			raise Exception('Trainable structure mismatch.')

		for k, v in my_trainables.items():
			if k not in trainables:
				raise Exception("No trainable named '{}'.".format(k))
			v2 = trainables[k]
			v.W.data[:] = v2.W.data
			if v.b is not None:
				v.b.data[:] = v2.b.data

	def module(self, kind_name):
		"""自分を入力とする出力 Module を生成する.

		Args:
			kind_name: 種類名.

		Returns:
			Module.
		"""
		nodes_owner = self._get_nodes_owner()
		return nodes_owner._on_new_node(kind_name, Module(self.root.get_module(), nodes_owner, kind_name, self))

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

	def gate(self, kind_name, func, *inputs, output_same_value=False):
		"""レイヤ同士を結合する Gate を作成する.

		Args:
			kind_name: 種類名.
			func: 引数を入力レイヤーを通しさらに出力レイヤーを通す処理、 def func(output_layers, x).
			inputs: 入力レイヤー列.
			output_same_value: 全出力先ノードに同じ値を出力するなら True.

		Returns:
			Gate.
		"""
		if len(inputs) == 0:
			inputs = (self,)
		nodes_owner = self._get_nodes_owner()
		return nodes_owner._on_new_node(kind_name, Gate(nodes_owner, kind_name, func, *inputs, output_same_value=output_same_value))

	def observer(self, kind_name, func, *inputs):
		"""推論関数の途中でプロット表示を行うなど出力を必要としない Gate を作成する.

		Args:
			kind_name: 種類名.
			func: 引数を入力レイヤーを通しさらに出力レイヤーを通す処理、 def func(output_layers, x).
			inputs: 入力レイヤー列.

		Returns:
			Gate.
		"""
		o = self.gate(kind_name, func, *inputs)
		o.is_observer = True
		return o

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

	def relu(self):
		return self.funcs('relu', F.relu)

	def leaky_relu(self):
		return self.funcs('leaky_relu', F.leaky_relu)

	def tanh(self):
		return self.funcs('tanh', F.tanh)

	def sigmoid(self):
		return self.funcs('sigmoid', F.sigmoid)

	def dropout(self, ratio=.5, **kwargs):
		f = self.funcs('dropout', lambda x: F.dropout(x, ratio, **kwargs))
		f.dot_param = '(ratio:{})'.format(ratio)
		return f

	def unpool2d(self, ksize, stride=None, pad=0, outsize=None, cover_all=True):
		f = self.funcs('unpool2d', lambda x: F.unpooling_2d(x, ksize, stride, pad, outsize, cover_all))
		f.dot_param = '(ksize:{}, stride:{}, pad:{}, outsize:{}, cover_all:{})'.format(ksize, stride, pad, outsize, cover_all)
		return f

	def flatten(self):
		return self.funcs('flatten', F.flatten)

	def reshape(self, shape):
		f = self.funcs('reshape', lambda x: F.reshape(x, shape))
		f.dot_param = '(shape:{})'.format(shape)
		return f

	def tile(self, reps):
		f = self.funcs('tile', lambda x: F.tile(x, reps))
		f.dot_param = '(reps:{})'.format(reps)
		return f

	def repeat(self, repeats, axis=None):
		f = self.funcs('repeat', lambda x: F.repeat(x, repeats, axis))
		f.dot_param = '(repeats={}, axis={})'.format(repeats, axis)
		return f

	def average(self, axis=None, weights=None, keepdims=False):
		f = self.funcs('average', lambda x: F.average(x, axis, weights, keepdims))
		f.dot_param = '(axis:{}, weights:{}, keepdims:{})'.format(axis, weights, keepdims)
		return f

	def mean(self, axis=None, keepdims=False):
		f = self.funcs('mean', lambda x: F.mean(x, axis, keepdims=keepdims))
		f.dot_param = '(axis:{}, keepdims:{})'.format(axis, keepdims)
		return f

	def split(self, indices_or_sections, axis=0):
		g = self.gate('split', lambda _, x: F.split_axis(x, indices_or_sections, axis))
		return (g,) * (len(indices_or_sections) if isinstance(indices_or_sections, (tuple, list)) else indices_or_sections)

	def concat(self, *inputs, axis=1):
		return self.gate('concat', lambda _, *inputs: F.concat(inputs, axis), *inputs, output_same_value=True)

	def tile_as(self, input, target):
		return self.gate('tile_as', lambda _, x, t: F.tile(x, tuple([ts // xs for ts, xs in zip(t.shape, x.shape)])), input, target, output_same_value=True)

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

		if len(self.outputs) == 0 and not self.is_observer:
			raise Exception("Node '{}' must have one or more outputs.".format(self.get_full_name()))

		model = self.root.get_model()
		old_depth = depth
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
			in_vars.append(model.var_mgr.get_var((i, self), False))
		for v in in_vars:
			model.var_mgr.release_var(v)

		# 出力値の代入先変数確保
		if self.is_observer:
			out_vars = None
		else:
			out_vars = [model.var_mgr.get_var((self, o)) for o in self.outputs]

		# コードの追加
		in_tuple_str = ', '.join(in_vars) if len(in_vars) != 0 else 'x'
		if out_vars is None:
			out_tuple_str = None
		elif len(out_vars) != 0:
			out_tuple_str = out_vars[0] if self.output_same_value else ', '.join(out_vars)
		else:
			out_tuple_str = model.var_mgr.get_var((self, None))
		self._add_code(out_tuple_str, '({})'.format(in_tuple_str) if self.allow_multi_inputs else in_tuple_str)

		# グラフ用コードの追加
		dot_name = self.get_dot_node_name()
		model.dot_code.append('{} [label="{}"];'.format(dot_name, self.get_dot_node_label()))
		for i, var in zip(self.inputs, in_vars):
			model.dot_code.append('{} -> {} [label="{}"];'.format(i.get_dot_node_name(), dot_name, var))

		self._build_end()

		# Observer は出力側から呼び出されないので入力側から呼び出しておく
		depth = old_depth - 1
		for o in self.outputs:
			if o is not None and o.is_observer:
				o._build(depth)

	def _add_code(self, out_tuple_str, in_tuple_str):
		"""推論関数用コードを追加する.

		Args:
			out_tuple_str: 計算結果を格納する変数名.
			in_tuple_str: 入力値を格納している変数名.
		"""
		if out_tuple_str is None:
			self.root.get_model().code.append('\t{}({})'.format(self.get_code_node_name(), in_tuple_str))
		else:
			self.root.get_model().code.append('\t{} = {}({})'.format(out_tuple_str, self.get_code_node_name(), in_tuple_str))

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
			self.root.get_model().code.append('\t{} = {}'.format(out_tuple_str, in_tuple_str))


class Funcs(Node):
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

		self.func_list = [] # 関数リスト、先頭から順に実行される

		self.allow_multi_inputs = False
		self.output_same_value = True

		if input is not None:
			input.add_ouput(self)
			self.add_input(input)

		self.func_list.extend(funcs)

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


class Layer(Chain, Node):
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

		self.allow_multi_inputs = False
		self.output_same_value = True

		if input is not None:
			input.add_ouput(self)
			self.add_input(input)

		with self.init_scope():
			self.link = link

	def __call__(self, x=None):
		"""指定値をレイヤーで変換する.

		Args:
			x: 入力値.

		Returns:
			変換後の値.
		"""
		return self.link(x)


class Gate(Node):
	"""指定値を１～複数の出力レイヤーで変換する、複数対複数のレイヤーをくっつける役割を持つ.

	Args:
		owner: 所有者となる Node.
		kind_name: 種類名.
		func: 引数を入力レイヤーを通しさらに出力レイヤーを通す処理、 def func(output_layers, x).
		inputs: 入力レイヤー列.
		output_same_value: 全出力先ノードに同じ値を出力するなら True.
	"""

	def __init__(self, owner, kind_name, func, *inputs, output_same_value=False):
		inputs = tuple([(i if i != owner else None) for i in inputs])

		for i in inputs:
			if i is not None and not isinstance(i, Node):
				raise TypeError("Invalid argument. 'inputs' element must be Node.")

		Node.__init__(self, owner, kind_name)

		for input in inputs:
			if input is not None:
				input.add_ouput(self)
				self.add_input(input)

		self.func = func
		self.output_same_value = output_same_value

	def get_dot_node_label(self):
		"""dot 内でのノードのラベルの取得.

		Returns:
			ノードのラベル.
		"""
		fn = ' {}'.format(self.func.__name__) if self.func.__name__ != '<lambda>' else ''
		return '{}{}\\n{}'.format(self.get_full_name(), fn, self.dot_param)

	def __call__(self, x):
		"""指定値をユーザー指定処理で変換する.

		Args:
			x: 入力値.

		Returns:
			変換後の値.
		"""
		return self.func(self.outputs, *x) if isinstance(x, tuple) else self.func(self.outputs, x)


class Module(Chain, Node):
	"""複数の子 Node を持つジュール.

	Args:
		model: モジュールが属することになるモデル.
		owner: 親となる Module、None が指定されたらルートとなる.
		kind_name: 種類名.
		input: 入力 Node または None.
	"""

	def __init__(self, model, owner=None, kind_name='root', input=None):
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

		self.model = model # この Module が属する Model
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

	def get_model(self):
		"""この Node が属する Model の取得.
		"""
		return self.model

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
		lasts = [n for n in all_nodes if not n.is_observer and n not in all_inputs]
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
		model = self.root.get_model()
		tmp_var = None
		if self != self.root and 2 <= len(self.nodes):
			tmp_var = model.tmp_var_mgr.get_var((self, self))
			model.code.append('\t{} = {}'.format(tmp_var, self.get_code_node_name()))
			self.tmp_code_node_name = tmp_var

		self.lasts[0]._build(depth)

		if tmp_var is not None:
			self.tmp_code_node_name = None
			model.tmp_var_mgr.release_var(tmp_var)

		# 使用ノードをサブグラフとする
		if self.owner is not None:
			dot_node_name = Node.get_dot_node_name(self)
			model.dot_code.append('subgraph cluster_{} {{ label="{}"'.format(dot_node_name, dot_node_name))
			for node in self.nodes:
				model.dot_code.append('{};'.format(node.get_dot_node_name()))
			model.dot_code.append('}')

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


class Var:
	"""生成する推論関数内で使用される一時変数.
	"""

	def __init__(self, id, prefix):
		self.id = id
		self.prefix = prefix
		self.refcount = 1

	def get_var_name(self):
		return self.prefix + str(self.id)


class VarMgr:
	"""生成する推論関数内で使用される一時変数名の管理クラス.

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


class DictSerializer:

	def __init__(self, dictionary):
		self.dictionary = dictionary

	def __getitem__(self, key):
		key = key.replace('/', '.')
		d = {}
		self.dictionary[key] = d
		return DictSerializer(d)

	def __call__(self, key, value):
		ret = value
		if isinstance(value, cp.ndarray):
			value = value.get()
		value = np.asarray(value)
		self.dictionary[key] = value
		return ret


class DictDeserializer:

	def __init__(self, dictionary):
		self.dictionary = dictionary

	def __getitem__(self, key):
		key = key.replace('/', '.')
		return DictDeserializer(self.dictionary[key])

	def __call__(self, key, value):
		key = key.replace('.', '/')
		value_in_dict = self.dictionary[key]

		if value_in_dict[()] is None:
			return None
		if value is None:
			return value_in_dict
		if isinstance(value, np.ndarray):
			np.copyto(value, value_in_dict)
		elif isinstance(value, cp.ndarray):
			value.set(np.asarray(value_in_dict, dtype=value.dtype))
		else:
			value = type(value)(np.asarray(value_in_dict))
		return value


class Model:
	"""モデル

	Args:
		optimizer: 重み最適化処理方法、 chainer.optimizer.Optimizer を継承するもの.
	"""

	def __init__(self, optimizer=None):
		self.module = Module(self) # ニューラルネットワーク
		self.optimizer = optimizer # 勾配の最適化用オブジェクト
		self.tmp_var_mgr = VarMgr('m') # 生成する推論関数内での各ノードを格納する一時変数名を管理する
		self.var_mgr = VarMgr('x') # 生成する推論関数内での各ノード出力を受け取る一時変数名を管理する
		self.code = [] # 推論コード
		self.dot_code = [] # Graphviz の .dot コード
		self.prediction = None # 推論関数

		self.module.name = 'root'
		self.module.add_ouput(None)

	def build(self, output_file_name=None, module_name='Prediction'):
		"""モデルの推論用関数をビルドする.

		Args:
			output_file_name: 推論関数出力ソースファイル名、None 以外が指定されたらこのファイル作成してインポートされる、デバッグ用.
			module_name: output_file_name ロード時に付与するモジュール名.
		"""
		self.code.append('def prediction(root, x):')
		self.dot_code.insert(0, 'digraph {\n')
		self.dot_code.insert(1, 'node [shape=box]\n')

		self.module._build(0)

		if len(self.var_mgr.vars) == 0:
			raise Exception("Internal error. No output exists after '{}' built.".format(self.module.get_full_name()))
		if 2 <= len(self.var_mgr.vars):
			raise Exception("Internal error. Multiple output exists after '{}' built. Exists outputs is {}.".format(self.module.get_full_name(), self.var_mgr.vars))
		for v in self.var_mgr.vars.values():
			var_name = v.get_var_name()

		self.dot_code.append('}')
		self.dot_code = '\n'.join(self.dot_code)
		self.code.append('\treturn {}\n'.format(var_name))
		self.code = '\n'.join(self.code)

		if output_file_name is None:
			l = {}
			exec(self.code, globals(), l)
			self.prediction = l['prediction']
		else:
			self.code = """{}
def bind_prediction(model):
	model.prediction = prediction
""".format(self.code)
			output_file_name = os.path.abspath(output_file_name)
			with open(output_file_name, mode='w') as f:
				f.write(self.code)
			m = SourceFileLoader(module_name, output_file_name).load_module()
			m.bind_prediction(self)

		if self.optimizer is not None:
			self.optimizer.setup(self.module)

	def state_dict(self):
		"""重み、パラメータなどモデルの状態を辞書に入れて返す.

		Returns:
			dict.
		"""
		d = {}
		mo = {}
		d['module'] = mo
		self.module.serialize(DictSerializer(mo))
		if self.optimizer is not None:
			do = {}
			d['optimizer'] = do
			self.optimizer.serialize(DictSerializer(do))
		return d

	def load_state_dict(self, d):
		"""state_dict() で取得した辞書から状態を復元する.
		"""
		self.module.serialize(DictDeserializer(d['module']))
		if self.optimizer is not None:
			self.optimizer.serialize(DictDeserializer(d['optimizer']))

	def zero_grad(self):
		self.module.zerograds()

	def step(self):
		self.optimizer.update()

	def __call__(self, x):
		"""計算を実行する.

		Args:
			x: 入力値.

		Returns:
			結果.
		"""
		return self.prediction(self.module, x)


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

	def state_dict(self):
		d = {}
		for k, v in vars(self).items():
			if isinstance(v, Model):
				d[k] = v.state_dict()
		return d

	def load_state_dict(self, d):
		sd = self.__dict__
		for k, v in d.items():
			if k in sd:
				sv = sd[k]
				if isinstance(sv, Model) and isinstance(v, dict):
					sv.load_state_dict(v)


def startup(gpu, train=True):
	"""環境を初期化する.

	Args:
		gpu: 使用するGPUインデックス、負数ならGPU未使用となる.
	"""
	global test, xp
	if xp is not None:
		return
	test = not train
	chainer.config.train = train
	if not train:
		chainer.config.no_backprop_mode()
	if 0 <= gpu:
		print(('Using cuda device {}.').format(gpu))
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
		if isinstance(x, np.ndarray):
			return cuda.to_gpu(x)
		if isinstance(x, Variable):
			return x.to_gpu()
		if isinstance(x, tuple):
			return tuple([to_gpu(e) for e in x])
		if isinstance(x, list):
			return [to_gpu(e) for e in x]
		if isinstance(x, Link):
			return x.to_gpu()
		if isinstance(x, Optimizer):
			return x
		if isinstance(x, Model):
			x.module = x.module.to_gpu()
			return x
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
		if isinstance(x, cp.ndarray):
			return cuda.to_cpu(x)
		if isinstance(x, Variable):
			return x.to_cpu()
		if isinstance(x, tuple):
			return tuple([to_cpu(e) for e in x])
		if isinstance(x, list):
			return [to_cpu(e) for e in x]
		if isinstance(x, Link):
			return x.to_cpu()
		if isinstance(x, Optimizer):
			return x
		if isinstance(x, Model):
			x.module = x.module.to_cpu()
			return x
		if isinstance(x, Models):
			d = vars(x)
			for k, v in d.items():
				if isinstance(v, Model):
					d[k] = to_cpu(v)
			return x
		return cuda.to_cpu(x)
	else:
		return x


def to_device(x):
	"""オブジェクトを startup() 関数に指定したデバイスメモリに変換する.

	Args:
		x: 変換対象オブジェクト.
	Returns:
		変換後のオブジェクト.
	"""
	return to_gpu(x) if xp is cp else to_cpu(x)


def dict_to_hdf5(d, g, compression='gzip', compression_opts=9):
	"""dict と同じ構造を h5py 内に作成する.

	Args:
		d: dict オブジェクト.
		g: h5py のグループ.
	"""
	for k, v in d.items():
		if isinstance(v, dict):
			dict_to_hdf5(v, g.create_group(k), compression, compression_opts)
		else:
			if isinstance(v, np.ndarray) and 256 <= v.size:
				g.create_dataset(k, data=v, compression=compression, compression_opts=compression_opts)
			else:
				g.create_dataset(k, data=v)


def hdf5_to_dict(g):
	"""h5py のグループを dict に変換する.

	Args:
		g: h5py のグループ.
	"""
	d = {}
	for k, v in g.items():
		if hasattr(v, 'value'):
			d[k] = v.value
		else:
			d[k] = hdf5_to_dict(dict(v))
		# d[k] = v.value if hasattr(v, 'value') else hdf5_to_dict(v)
	return d


def save_dict_to_hdf5(filename, d, compression='gzip', compression_opts=9):
	"""dict をHDF5形式ファイルへ保存する.

	Args:
		filename: 保存先ファイル名.
		d: dict オブジェクト.
	"""
	with h5py.File(filename, 'w') as f:
		dict_to_hdf5(d, f, compression, compression_opts)


def load_dict_from_hdf5(filename):
	"""HDF5形式ファイルを dict に読み込んで取得する.

	Args:
		filename: 読み込み元ファイル名.

	Returns:
		dict オブジェクト.
	"""
	with h5py.File(filename, 'r') as f:
		return hdf5_to_dict(dict(f))


def save(filename, model, compression='gzip', compression_opts=9):
	"""Model をHDF5形式ファイルに保存する.

	Args:
		filename: 保存先ファイル名.
		model: 保存する Model オブジェクト.
	"""
	model = to_cpu(model)
	save_dict_to_hdf5(filename, model.state_dict(), compression, compression_opts)


def load(filename, model):
	"""HDF5形式ファイルを Model に読み込む.

	Args:
		filename: 読み込み元ファイル名.
		model: 読み込み先 Model オブジェクト.
	"""
	model = to_cpu(model)
	model.load_state_dict(load_dict_from_hdf5(filename))


def load_if_exists(filename, model):
	"""指定のHDF5形式ファイルが存在するなら Model に読み込む.

	Args:
		filename: 読み込み元ファイル名.
		model: 読み込み先 Model オブジェクト.
	"""
	if os.path.isfile(filename):
		load(filename, model)


if __name__ == '__main__':
	startup(0)
