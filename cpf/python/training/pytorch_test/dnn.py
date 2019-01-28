"""ディープラーニング関係ヘルパ、テンソルのCPU↔GPU変換処理など.
"""
import os
from importlib.machinery import SourceFileLoader
import numpy as np
import torch
from torch import nn
import torch.nn.functional as F
import torch.optim as optim
import torch.cuda

Trainable = nn.Module

uint8 = torch.uint8
int8 = torch.int8
int16 = torch.int16
int32 = torch.int32
int64 = torch.int64,
float16 = torch.float16
float32 = torch.float32
float64 = torch.float64

dtype = torch.float32
cpu = None
device = None

tensor = torch.tensor
zeros = torch.zeros
dot = torch.dot
cat = torch.cat
max = torch.max
argmax = torch.argmax


class Node:
	"""モデルを構成するノード、レイヤを生成する機能を提供するクラス.

	Args:
		owner: このノードの所有者となる Node.
		kind_name: 種類名.
	"""

	def __init__(self, owner, kind_name):
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

		# 直接メンバ変数に持つと state_dict() 呼び出し時に無限ループするので dict に入れておく
		self.other_nodes = {
		    'owner': owner,
		}

		# ルート Node の取得
		node = self
		while True:
			p = node.get_owner()
			if p is None:
				break
			node = p
		self.other_nodes['root'] = node

	def __str__(self):
		return self.get_full_name()

	def __repr__(self):
		return self.get_full_name()

	def get_root(self):
		"""この Node のルート Node の取得."""
		return self.other_nodes['root']

	def get_owner(self):
		"""この Node の所有者 Node の取得."""
		return self.other_nodes['owner']

	def get_model(self):
		"""この Node が属する Model の取得."""
		return self.get_root().get_model()

	def get_full_name(self):
		"""ルート Node からのフル名称を取得する."""
		root = self.get_root()
		if self == root:
			return self.name

		names = [self.name]
		node = self.get_owner()
		while node != root:
			nm = node.name
			if nm is not None and len(nm) != 0:
				names.append(nm)
			node = node.get_owner()
		return '.'.join(reversed(names))

	def get_code_node_name(self):
		"""生成される推論関数内でのこの Node の名称を取得する."""
		if self.tmp_code_node_name is not None:
			return self.tmp_code_node_name

		if self == self.get_root():
			return 'root'

		return f'{self.get_owner().get_code_node_name()}.{self.name}'

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
		return f'{self.get_full_name()}\\n{self.dot_param}'

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
			raise Exception(f"Node '{self.get_full_name()}' does not support multiple inputs.")
		self.inputs.append(node)

	def add_ouput(self, node):
		"""出力先ノードを追加する.

		Args:
			node: 出力先 Node.
		"""
		if not self.allow_multi_outputs and 1 <= len(self.outputs):
			raise Exception(f"Node '{self.get_full_name()}' does not support multiple outputs.")
		self.outputs.append(node)

	def set_inputs(self, inputs):
		"""入力ノード集合を設定する.

		Args:
			inputs: 入力 Node 集合.
		"""
		if not self.allow_multi_inputs and 2 <= len(inputs):
			raise Exception(f"Node '{self.get_full_name()}' does not support multiple inputs.")
		self.inputs = list(inputs)

	def set_outputs(self, outputs):
		"""入力ノード集合を設定する.

		Args:
			inputs: 入力 Node 集合.
		"""
		if not self.allow_multi_outputs and 2 <= len(outputs):
			raise Exception(f"Node '{self.get_full_name()}' does not support multiple outputs.")
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

	def visit_nodes(self, func):
		"""自分と子ノードを列挙する様に func を呼び出す.

		Args:
			func: Node を受け取る関数、 def func(node) の様な形式.
		"""
		func(self)

	def visit_trainables(self, func):
		"""自分と子が持つ Trainable を列挙する様に func を呼び出す.

		Args:
			func: Trainable を受け取る関数、 def func(trainable) の様な形式.
		"""

		def node_visitor(node):
			if hasattr(node, '_modules'):
				for t in node._modules.values():
					func(t)

		self.visit_nodes(node_visitor)

	def get_weights(self):
		"""自分と子の重みをリストで取得する.

		Returns:
			重みのリスト.
		"""
		weights = []

		def trainable_visitor(trainable):
			if hasattr(trainable, 'weight'):
				weights.append(trainable.weight)

		self.visit_trainables(trainable_visitor)

		return weights

	def get_biases(self):
		"""自分と子のバイアスをリストで取得する.

		Returns:
			バイアスのリスト.
		"""
		biases = []

		def trainable_visitor(trainable):
			if hasattr(trainable, 'bias'):
				biases.append(trainable.bias)

		self.visit_trainables(trainable_visitor)

		return biases

	def calc_output_shape(self, input_shape):
		"""バッチを除外した出力サイズを計算する.

		Args:
			input_shape: 入力値形状 tuple.
		
		Returns:
			出力値形状 tuple.
		"""
		for i in self.inputs:
			return i.calc_output_shape(input_shape)
		return input_shape

	def module(self, kind_name):
		"""自分を入力とする出力 Module を生成する.

		Args:
			kind_name: 種類名.

		Returns:
			Module.
		"""
		nodes_owner = self._get_nodes_owner()
		return nodes_owner._on_new_node(kind_name, Module(self.get_root().get_model(), nodes_owner, kind_name, self))

	def nop(self):
		"""自分を入力とする出力 NoOp を生成する.

		Returns:
			NoOp.
		"""
		nodes_owner = self._get_nodes_owner()
		return nodes_owner._on_new_node('nop', NoOp(nodes_owner, 'nop', self))

	def funcs(self, kind_name, funcs, shape_calculator=None):
		"""自分を入力とする出力 Funcs を生成する.

		Args:
			kind_name: 種類名.
			funcs: 関数列、各関数は def func(x): return x * 2 の様な形式.
			shape_calculator: 入力値形状 tuple から出力値形状 tuple を計算する関数.

		Returns:
			Funcs.
		"""
		nodes_owner = self._get_nodes_owner()
		return nodes_owner._on_new_node(kind_name, Funcs(nodes_owner, kind_name, funcs, self, shape_calculator=shape_calculator))

	def layer(self, kind_name, trainable):
		"""自分を入力とする出力 Layer を生成する.

		Args:
			kind_name: 種類名.
			trainable: レイヤーの計算のメインとなる Trainable.

		Returns:
			Layer.
		"""
		nodes_owner = self._get_nodes_owner()
		return nodes_owner._on_new_node(kind_name, Layer(nodes_owner, kind_name, trainable, self))

	def gate(self, kind_name, func, *inputs, output_same_value=False, shape_calculator=None):
		"""レイヤ同士を結合する Gate を作成する.

		Args:
			kind_name: 種類名.
			func: 引数を入力レイヤーを通しさらに出力レイヤーを通す処理、 def func(output_layers, x).
			inputs: 入力レイヤー列.
			output_same_value: 全出力先ノードに同じ値を出力するなら True.
			shape_calculator: 入力値形状 tuple から出力値形状 tuple を計算する関数.

		Returns:
			Gate.
		"""
		if len(inputs) == 0:
			inputs = (self,)
		nodes_owner = self._get_nodes_owner()
		return nodes_owner._on_new_node(kind_name, Gate(nodes_owner, kind_name, func, *inputs, output_same_value=output_same_value, shape_calculator=shape_calculator))

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

	def dense(self, in_features, out_features, bias=True):
		l = self.layer('dense', nn.Linear(in_features, out_features, bias))
		l.dot_param = f'(in:{in_features}, out:{out_features})'
		return l

	def conv2d(self, in_channels, out_channels, kernel_size, stride=1, padding=0, dilation=1, groups=1, bias=True):
		l = self.layer('conv2d', nn.Conv2d(in_channels, out_channels, kernel_size, stride, padding, dilation, groups, bias))
		l.dot_param = f'(in:{in_channels}, out:{out_channels}, ksize:{kernel_size})'
		return l

	def deconv2d(self, in_channels, out_channels, kernel_size, stride=1, padding=0, output_padding=0, groups=1, bias=True, dilation=1):
		l = self.layer('deconv2d', nn.ConvTranspose2d(in_channels, out_channels, kernel_size, stride, padding, output_padding, groups, bias, dilation))
		l.dot_param = f'(in:{in_channels}, out:{out_channels}, ksize:{kernel_size})'
		return l

	def batchnorm2d(self, num_features, eps=1e-5, momentum=0.1, affine=True, track_running_stats=True):
		l = self.layer('batchnorm', nn.BatchNorm2d(num_features, eps, momentum, affine, track_running_stats))
		l.dot_param = f'(num:{num_features})'
		return l

	def prelu(self, num_parameters=1, init=0.25):
		l = self.layer('prelu', nn.PReLU(num_parameters, init))
		l.dot_param = f'(params:{num_parameters})'
		return l

	def relu(self):
		return self.funcs('relu', F.relu)

	def leaky_relu(self):
		return self.funcs('leaky_relu', F.leaky_relu)

	def tanh(self):
		return self.funcs('tanh', F.tanh)

	def sigmoid(self):
		return self.funcs('sigmoid', F.sigmoid)

	def dropout(self, p=0.5, training=False, inplace=False):
		f = self.funcs('dropout', lambda x: F.dropout(x, p, training, inplace))
		f.dot_param = f'(p:{p})'
		return f

	def unpool2d(self, kernel_size, stride=None, padding=0):
		l = self.layer('unpool2d', nn.MaxUnpool2d(kernel_size, stride, padding))
		l.dot_param = f'(ksize:{kernel_size}, stride:{stride}, pad:{padding})'
		return l

	def flatten(self):
		return self.funcs('flatten', lambda x: x.view(x.size(0), -1), shape_calculator=lambda input_shape: np.prod(input_shape).item())

	def view(self, *shape):
		f = self.funcs('view', lambda x: x.view(*shape), shape_calculator=lambda input_shape: shape)
		f.dot_param = f'(shape:{shape})'
		return f

	def tile(self, *repeats):
		f = self.funcs('tile', lambda x: x.repeat(*repeats))
		f.dot_param = f'(repeats={repeats})'
		return f

	def expand_as(self, target):
		return self.gate('expand_as', lambda _, x, t: x.expand_as(t), self, target, output_same_value=True)

	def mean(self, axis=None, keepdims=False):
		f = self.funcs('mean', lambda x: x.mean(axis, keepdims))
		f.dot_param = f'(axis:{axis}, keepdims:{keepdims})'
		return f

	def concat(self, *inputs, dim=0):
		return self.gate('concat', lambda _, *x: torch.cat(x, dim), *inputs, output_same_value=True)

	def data(self):
		return self.funcs('data', lambda x: x.data)

	def _get_nodes_owner(self):
		"""新規 Node 生成時に親となる Node の取得.

		Returns:
			Node.
		"""
		return self if self.get_owner() is None else self.get_owner()

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
			raise Exception(f"Node '{self.get_full_name()}' has already built.")
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
			raise Exception(f"Node '{self.get_full_name()}' must have one or more outputs.")

		model = self.get_root().get_model()
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
		self._add_code(out_tuple_str, f'({in_tuple_str})' if self.allow_multi_inputs else in_tuple_str)

		# グラフ用コードの追加
		dot_name = self.get_dot_node_name()
		model.dot_code.append(f'{dot_name} [label="{self.get_dot_node_label()}"];')
		for i, var in zip(self.inputs, in_vars):
			model.dot_code.append(f'{i.get_dot_node_name()} -> {dot_name} [label="{var}"];')

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
			self.get_root().get_model().code.append(f'\t{self.get_code_node_name()}({in_tuple_str})')
		else:
			self.get_root().get_model().code.append(f'\t{out_tuple_str} = {self.get_code_node_name()}({in_tuple_str})')

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
			self.get_root().get_model().code.append(f'\t{out_tuple_str} = {in_tuple_str}')


class Funcs(Node):
	"""関数列.

	Args:
		owner: 所有者となる Node.
		kind_name: 種類名.
		funcs: 関数列、各関数は def func(x): return x * 2 の様な形式.
		input: 入力 Node または None.
		shape_calculator: 入力値形状 tuple から出力値形状 tuple を計算する関数.
	"""

	def __init__(self, owner, kind_name, funcs, input=None, shape_calculator=None):
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
		self.shape_calculator = shape_calculator

		if input is not None:
			input.add_ouput(self)
			self.add_input(input)

		self.func_list.extend(funcs)

	def calc_output_shape(self, input_shape):
		"""バッチを除外した出力サイズを計算する.

		Args:
			input_shape: 入力値形状 tuple.
		
		Returns:
			出力値形状 tuple.
		"""
		input_shape = Node.calc_output_shape(self, input_shape)
		return input_shape if self.shape_calculator is None else self.shape_calculator(input_shape)

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


class Layer(Trainable, Node):
	"""レイヤ、学習対象の重みなどを持つ.

	Args:
		owner: 所有者となる Node.
		kind_name: 種類名.
		trainable: 学習対象の重みを持つ torch.nn.Conv2d など Trainable を継承するもの.
		input: 入力 Node または None.
	"""

	def __init__(self, owner, kind_name, trainable, input=None):
		if input == owner:
			input = None

		if input is not None and not isinstance(input, Node):
			raise TypeError("Invalid argument. 'input' must be Node.")
		if not isinstance(trainable, Trainable):
			raise TypeError('Cannot register a non-trainable object as a child')

		Trainable.__init__(self)
		Node.__init__(self, owner, kind_name)

		self.allow_multi_inputs = False
		self.output_same_value = True

		if input is not None:
			input.add_ouput(self)
			self.add_input(input)

		self.trainable = trainable

	def __call__(self, x=None):
		"""指定値をレイヤーで変換する.

		Args:
			x: 入力値.

		Returns:
			変換後の値.
		"""
		return self.trainable(x)

	def calc_output_shape(self, input_shape):
		"""バッチを除外した出力サイズを計算する.

		Args:
			input_shape: 入力値形状 tuple.
		
		Returns:
			出力値形状 tuple.
		"""
		input_shape = Node.calc_output_shape(self, input_shape)
		t = self.trainable
		if isinstance(t, (nn.Conv1d, nn.Conv2d, nn.Conv3d)):
			input_shape = input_shape[1:]
			k = t.kernel_size
			s = t.stride
			p = t.padding
			d = t.dilation
			return (t.out_channels,) + tuple([conv_outsize(input_shape[i], k[i], s[i], p[i], False, d[i]) for i in range(len(input_shape))])
		elif isinstance(t, nn.Linear):
			return (t.out_features,)
		return input_shape


class Gate(Node):
	"""指定値を１～複数の出力レイヤーで変換する、複数対複数のレイヤーをくっつける役割を持つ.

	Args:
		owner: 所有者となる Node.
		kind_name: 種類名.
		func: 引数を入力レイヤーを通しさらに出力レイヤーを通す処理、 def func(output_layers, x).
		inputs: 入力レイヤー列.
		output_same_value: 全出力先ノードに同じ値を出力するなら True.
		shape_calculator: 入力値形状 tuple から出力値形状 tuple を計算する関数.
	"""

	def __init__(self, owner, kind_name, func, *inputs, output_same_value=False, shape_calculator=None):
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
		self.shape_calculator = shape_calculator

	def get_dot_node_label(self):
		"""dot 内でのノードのラベルの取得.

		Returns:
			ノードのラベル.
		"""
		fn = f' {self.func.__name__}' if self.func.__name__ != '<lambda>' else ''
		return f'{self.get_full_name()}{fn}\\n{self.dot_param}'

	def calc_output_shape(self, input_shape):
		"""バッチを除外した出力サイズを計算する.

		Args:
			input_shape: 入力値形状 tuple.
		
		Returns:
			出力値形状 tuple.
		"""
		input_shape = Node.calc_output_shape(self, input_shape)
		return input_shape if self.shape_calculator is None else self.shape_calculator(input_shape)

	def __call__(self, x):
		"""指定値をユーザー指定処理で変換する.

		Args:
			x: 入力値.

		Returns:
			変換後の値.
		"""
		return self.func(self.outputs, *x) if isinstance(x, tuple) else self.func(self.outputs, x)


class Module(Trainable, Node):
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

		Trainable.__init__(self)
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
		self.assembly_depth = 0 # これが 0 以外ならノード生成時に子ノードとして登録される、0 なら self.get_owner() の子ノードとして登録される

	def __enter__(self):
		self.assembly_depth += 1
		return self

	def __exit__(self, type, trainable, traceback):
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

	def visit_nodes(self, func):
		"""自分と子ノード列挙する様に func を呼び出す.

		Args:
			func: ノードを受け取る関数、 def func(node) の様な形式.
		"""
		func(self)
		for n in self.nodes:
			n.visit_nodes(func)

	def _get_nodes_owner(self):
		"""新規 Node 生成時に親となる Node の取得.

		Returns:
			Node.
		"""
		return self if self.assembly_depth != 0 or self.get_owner() is None else self.get_owner()

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
				raise Exception(f"Child nodes of '{self.get_full_name()}' can not input external nodes. {', '.join(i.get_full_name() for i in dif)}")

		# 最初のノードを集める
		firsts = [n for n, firsts in nodewise_inputs.items() if len(firsts) == 0]
		if len(firsts) == 0:
			raise Exception(f"Node '{self.get_full_name()}' must have a one input.")
		if len(firsts) != 1:
			raise Exception(f"Node '{self.get_full_name()}' does not support multiple inputs. {', '.join(i.get_full_name() for i in firsts)}")

		# 最後のノード（入力とされていないノード）を集める
		lasts = [n for n in all_nodes if not n.is_observer and n not in all_inputs]
		if len(lasts) == 0:
			raise Exception(f"Node '{self.get_full_name()}' must have a one output.")
		if len(lasts) != 1:
			raise Exception(f"Node '{self.get_full_name()}' does not support multiple outputs. {', '.join(i.get_full_name() for i in lasts)}")

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
		model = self.get_root().get_model()
		tmp_var = None
		if self != self.get_root() and 2 <= len(self.nodes):
			tmp_var = model.tmp_var_mgr.get_var((self, self))
			model.code.append(f'\t{tmp_var} = {self.get_code_node_name()}')
			self.tmp_code_node_name = tmp_var

		self.lasts[0]._build(depth)

		if tmp_var is not None:
			self.tmp_code_node_name = None
			model.tmp_var_mgr.release_var(tmp_var)

		# 使用ノードをサブグラフとする
		if self.get_owner() is not None:
			dot_node_name = Node.get_dot_node_name(self)
			model.dot_code.append(f'subgraph cluster_{dot_node_name} {{ label="{dot_node_name}"')
			for node in self.nodes:
				model.dot_code.append(f'{node.get_dot_node_name()};')
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
				raise Exception(f"Internal error. Local variable for {var_key} is not exists.")
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


class Model:
	"""モデル
	"""

	def __init__(self):
		self.module = Module(self) # ニューラルネットワーク
		self.optimizer = None # 勾配の最適化用オブジェクト
		self.tmp_var_mgr = VarMgr('m') # 生成する推論関数内での各ノードを格納する一時変数名を管理する
		self.var_mgr = VarMgr('x') # 生成する推論関数内での各ノード出力を受け取る一時変数名を管理する
		self.code = [] # 推論コード
		self.dot_code = [] # Graphviz の .dot コード
		self.prediction = None # 推論関数

		# ルートとして初期化
		self.module.name = 'root'
		self.module.add_ouput(None)

	def optimizer_RMSprop(self, lr=1e-2, alpha=0.99, eps=1e-8, weight_decay=0, momentum=0, centered=False):
		self.optimizer = optim.RMSprop(self.module.parameters(), lr, alpha, eps, weight_decay, momentum, centered)

	def optimizer_Adam(self, lr=1e-3, betas=(0.9, 0.999), eps=1e-8, weight_decay=0, amsgrad=False):
		self.optimizer = optim.Adam(self.module.parameters(), lr, betas, eps, weight_decay, amsgrad)

	def optimizer_Adamax(self, lr=2e-3, betas=(0.9, 0.999), eps=1e-8, weight_decay=0):
		self.optimizer = optim.Adamax(self.module.parameters(), lr, betas, eps, weight_decay)

	def build(self, output_file_name=None, module_name='Prediction'):
		"""モデルの推論用関数をビルドする.

		Args:
			output_file_name: 推論関数出力ソースファイル名、None 以外が指定されたらこのファイル作成してインポートされる、デバッグ用.
			module_name: output_file_name ロード時に付与するモジュール名.

		Returns:
			self.
		"""
		self.code.append('def prediction(root, x):')
		self.dot_code.insert(0, 'digraph {\n')
		self.dot_code.insert(1, 'node [shape=box]\n')

		self.module._build(0)

		if len(self.var_mgr.vars) == 0:
			raise Exception(f"Internal error. No output exists after '{self.module.get_full_name()}' built.")
		if 2 <= len(self.var_mgr.vars):
			raise Exception(f"Internal error. Multiple output exists after '{self.module.get_full_name()}' built. Exists outputs is {self.var_mgr.vars}.")
		for v in self.var_mgr.vars.values():
			var_name = v.get_var_name()

		self.dot_code.append('}')
		self.dot_code = '\n'.join(self.dot_code)
		self.code.append(f'\treturn {var_name}\n')
		self.code = '\n'.join(self.code)

		if output_file_name is None:
			l = {}
			exec(self.code, globals(), l)
			self.prediction = l['prediction']
		else:
			with open(os.path.abspath(output_file_name), mode='w') as f:
				f.write(self.code)
			self.prediction = SourceFileLoader(module_name, output_file_name).load_module().prediction

		return self

	def to(self, device):
		self.module = self.module.to(device)
		return self

	def zero_grad(self):
		self.module.zerograds()

	def train(self):
		self.module.train()

	def eval(self):
		self.module.eval()

	def step(self):
		self.optimizer.step()

	def __call__(self, x):
		"""計算を実行する.

		Args:
			x: 入力値.

		Returns:
			結果.
		"""
		return self.prediction(self.module, x)

	def state_dict(self):
		"""重み、パラメータなどモデルの状態を辞書に入れて返す.

		Returns:
			dict.
		"""
		d = {
		    'module': self.module.state_dict(),
		}
		if self.optimizer is not None:
			d['optimizer'] = self.optimizer.state_dict()
		return d

	def load_state_dict(self, d):
		"""state_dict() で取得した辞書から状態を復元する.
		"""
		self.module.load_state_dict(d['module'])
		if self.optimizer is not None:
			self.optimizer.load_state_dict(d['optimizer'])


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

	def to(self, device):
		d = self.__dict__
		for k, v in d.items():
			if isinstance(v, Model):
				d[k] = v.to(device)
		return self

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


def startup(gpu):
	"""環境を初期化する.

	Args:
		gpu: 使用するGPUインデックス、負数ならGPU未使用となる.
	"""
	global device

	device_name = f'cuda:{gpu}' if 0 <= gpu and torch.cuda.is_available() else 'cpu'

	print((f'Using device : {device_name}.'))

	torch.cuda.set_device(gpu)
	cpu = torch.device('cpu')
	device = torch.device(device_name)


def to_cpu(x):
	"""GPU利用可能状態ならCPUメモリオブジェクトに変換する.

	Args:
		x: 変換対象オブジェクト.
	Returns:
		変換後のオブジェクト.
	"""
	return x.to(cpu) if isinstance(x, (Trainable, torch.Tensor, Model, Models)) else x


def to_device(x):
	"""オブジェクトを startup() 関数に指定したデバイスメモリに変換する.

	Args:
		x: 変換対象オブジェクト.
	Returns:
		変換後のオブジェクト.
	"""
	return x.to(device) if isinstance(x, (Trainable, torch.Tensor, Model, Models)) else x


def no_grad():
	"""勾配計算を無効化するコンテキストマネージャーを返す.
	Remarks:
		with dnn.no_grad(): の様にして使う.
	"""
	return torch.no_grad()


def save(filename, d):
	"""dict をファイルに保存する.

	Args:
		filename: 保存先ファイル名.
		d: 保存する dict オブジェクト.
	"""
	torch.save(d, filename)


def load(filename):
	"""ファイルから dict を読み込む.

	Args:
		filename: 読み込み元ファイル名.

	Returns:
		dict.
	"""
	return torch.load(filename)


def save_model(filename, model):
	"""指定モデルをファイルへ保存する.

	Args:
		filename: 保存先ファイル名.
		model: 保存元 Model.
	"""
	save(filename, model.state_dict())


def load_model_if_exists(filename, model):
	"""指定ファイルが存在するなら Model に読み込む.

	Args:
		filename: 読み込み元ファイル名.
		model: 読み込み先 Model.
	"""
	if os.path.isfile(filename):
		model.load_state_dict(load(filename))


def conv_outsize(size, k, s, p, cover_all=False, d=1):
	"""Calculates output size of convolution.

	This function takes the size of input feature map, kernel, stride, and
	pooling of one particular dimension, then calculates the output feature
	map size of that dimension.

	.. seealso:: :func:`~chainer.utils.get_deconv_outsize`

	Args:
		size (int): The size of input feature map. It usually is the length of
			a side of feature map.
		k (int): The size of convolution kernel.
		s (int): The size of stride.
		p (int): The size of padding.
		cover_all (bool): Use ``cover_all`` option or not.
		d (int): The size of dilation.

	Returns:
		int: The expected output size of the convolution operation.

	"""
	dk = k + (k - 1) * (d - 1)
	if cover_all:
		return (size + p * 2 - dk + s - 1) // s + 1
	else:
		return (size + p * 2 - dk) // s + 1


def deconv_outsize(size, k, s, p, cover_all=False, d=1):
	"""Calculates output size of deconvolution.

	This function takes the size of input feature map, kernel, stride, and
	pooling of one particular dimension, then calculates the output feature
	map size of that dimension.

	.. seealso:: :func:`~chainer.utils.get_conv_outsize`

	Args:
		size (int): The size of input feature map. It usually is the length of
			a side of feature map.
		k (int): The size of deconvolution kernel.
		s (int): The size of stride.
		p (int): The size of padding.
		cover_all (bool): Use ``cover_all`` option or not.
		d (int): The size of dilation.

	Returns:
		int: The expected output size of the deconvolution operation.

	"""
	dk = (k - 1) * d + 1
	if cover_all:
		return s * (size - 1) + dk - s + 1 - 2 * p
	else:
		return s * (size - 1) + dk - 2 * p


if __name__ == '__main__':
	startup(0)
