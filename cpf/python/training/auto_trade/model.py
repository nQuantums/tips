import math
import matplotlib.pyplot as plt

import dnn
from dnn import np
from dnn import nn

plot_enabled = False
show_plot = False


def plt_pause(sec):
	if plot_enabled and show_plot:
		plt.legend()
		plt.pause(sec)


def calc_width_height(length):
	w = int(math.sqrt(length))
	while length % w != 0:
		w += 1
	h = length // w
	return (h, w)


def plot_img(self, name, batch, ax):
	if not plot_enabled:
		return self

	def proc(x):
		if show_plot and name:
			img = x.detach()[batch].sum(dim=0).cpu().numpy()
			ax.cla()
			ax.imshow(img)
			ax.set_title(name)
		return x

	return self.funcs('plot_img', proc)


def plot_dense(self, name, batch, ax):
	if not plot_enabled:
		return self

	def proc(x):
		if show_plot and name:
			img = x.detach()[batch].cpu().numpy()
			img = img.reshape(calc_width_height(img.size))
			ax.cla()
			ax.imshow(img)
			ax.set_title(name)
		return x

	return self.funcs('plot_dense', proc)


def plot_action(self, name, batch, ax, x_data):
	if not plot_enabled:
		return self

	def proc(x):
		if show_plot and name:
			ax.cla()
			ax.plot(x_data, x.detach()[batch].cpu().numpy())
			ax.set_title(name)
		return x

	return self.funcs('plot_action', proc)


# Node にメソッド追加してみる
dnn.Node.plot_img = plot_img
dnn.Node.plot_action = plot_action
dnn.Node.plot_dense = plot_dense


def dqn(state_shape, action_dim, show_plot_name=None, hidden_size=32):
	"""DDQNモデルを生成する.

	Args:
		state_shape: バッチを除いた入力値形状 tuple.
		action_dim: アクション数、バッチを除いた出力値の要素数となる.
		show_plot_name: 変換途中データのプロット表示するならプロット名を、それ以外は None.
		hidden_size: 中間畳み込みレイヤのCH数.

	Returns:
		モデル.
	"""
	model = dnn.Root()

	if not plot_enabled:
		show_plot_name = None
	if show_plot_name:
		fig = plt.figure()
		fig.suptitle(show_plot_name, fontsize=12)
		ax_cnv1 = fig.add_subplot(2, 3, 1)
		ax_cnv1 = fig.add_subplot(2, 3, 1)
		ax_cnv2 = fig.add_subplot(2, 3, 2)
		ax_cnv3 = fig.add_subplot(2, 3, 3)
		ax_dense_val = fig.add_subplot(2, 3, 4)
		ax_dense_adv = fig.add_subplot(2, 3, 5)
		ax_action = fig.add_subplot(2, 3, 6)

		x_data = np.arange(action_dim)

	with model as m:
		c = m.conv2d(state_shape[0], hidden_size, 8, 4).relu().plot_img(show_plot_name, 0, ax_cnv1)
		c = c.conv2d(hidden_size, hidden_size * 2, 4, 2).relu().plot_img(show_plot_name, 0, ax_cnv2)
		c = c.conv2d(hidden_size * 2, hidden_size * 2, 3, 1).relu().plot_img(show_plot_name, 0, ax_cnv3)
		f = c.flatten()

		fc = f.calc_output_shape(state_shape)

		val = f.dense(fc, 512).relu().plot_dense(show_plot_name, 0, ax_dense_val)
		val = val.dense(512, 1)
		adv = f.dense(fc, 512).relu().plot_dense(show_plot_name, 0, ax_dense_adv)
		adv = adv.dense(512, action_dim)
		ave = adv.mean(1, keepdims=True)

		m.gate('merge', (lambda o, val, adv, ave: val + adv - ave), val, adv, ave).plot_action(
		    show_plot_name, 0, ax_action, x_data)

	for w in model.get_weights():
		nn.init.normal_(w, 0, 0.02)

	model.build()

	return model


def dqn_prelu(state_shape, action_dim, show_plot_name=None, hidden_size=32):
	"""DDQNモデルを生成する.

	Args:
		state_shape: バッチを除いた入力値形状 tuple.
		action_dim: アクション数、バッチを除いた出力値の要素数となる.
		show_plot_name: 変換途中データのプロット表示するならプロット名を、それ以外は None.
		hidden_size: 中間畳み込みレイヤのCH数.

	Returns:
		モデル.
	"""
	model = dnn.Root()

	if not plot_enabled:
		show_plot_name = None
	if show_plot_name:
		fig = plt.figure()
		fig.suptitle(show_plot_name, fontsize=12)
		ax_cnv1 = fig.add_subplot(2, 3, 1)
		ax_cnv1 = fig.add_subplot(2, 3, 1)
		ax_cnv2 = fig.add_subplot(2, 3, 2)
		ax_cnv3 = fig.add_subplot(2, 3, 3)
		ax_dense_val = fig.add_subplot(2, 3, 4)
		ax_dense_adv = fig.add_subplot(2, 3, 5)
		ax_action = fig.add_subplot(2, 3, 6)
		x_data = np.arange(action_dim)
	else:
		ax_cnv1 = None
		ax_cnv1 = None
		ax_cnv2 = None
		ax_cnv3 = None
		ax_dense_val = None
		ax_dense_adv = None
		ax_action = None
		x_data = None

	with model as m:
		c = m.conv2d(state_shape[0], hidden_size, 8, 4).prelu().plot_img('conv 1', 0, ax_cnv1)
		c = c.conv2d(hidden_size, hidden_size * 2, 4, 2).prelu().plot_img('conv 2', 0, ax_cnv2)
		c = c.conv2d(hidden_size * 2, hidden_size * 2, 3, 1).prelu().plot_img('conv 3', 0, ax_cnv3)
		f = c.flatten()

		fc = f.calc_output_shape(state_shape)

		val = f.dense(fc, 512).prelu().plot_dense('dense val', 0, ax_dense_val)
		val = val.dense(512, 1)
		adv = f.dense(fc, 512).prelu().plot_dense('dense adv', 0, ax_dense_adv)
		adv = adv.dense(512, action_dim)
		ave = adv.mean(1, keepdims=True)

		m.gate('merge', (lambda o, val, adv, ave: val + adv - ave), val, adv, ave).plot_action(
		    'action', 0, ax_action, x_data)

	for w in model.get_weights():
		nn.init.normal_(w, 0, 0.02)

	model.build()

	return model

def dqn_prelu_long_dense(state_shape, action_dim, show_plot_name=None, hidden_size=32):
	"""DDQNモデルを生成する.

	Args:
		state_shape: バッチを除いた入力値形状 tuple.
		action_dim: アクション数、バッチを除いた出力値の要素数となる.
		show_plot_name: 変換途中データのプロット表示するならプロット名を、それ以外は None.
		hidden_size: 中間畳み込みレイヤのCH数.

	Returns:
		モデル.
	"""
	model = dnn.Root()

	if not plot_enabled:
		show_plot_name = None
	if show_plot_name:
		fig = plt.figure()
		fig.suptitle(show_plot_name, fontsize=12)
		ax_cnv1 = fig.add_subplot(2, 3, 1)
		ax_cnv1 = fig.add_subplot(2, 3, 1)
		ax_cnv2 = fig.add_subplot(2, 3, 2)
		ax_cnv3 = fig.add_subplot(2, 3, 3)
		ax_dense_val = fig.add_subplot(2, 3, 4)
		ax_dense_adv = fig.add_subplot(2, 3, 5)
		ax_action = fig.add_subplot(2, 3, 6)
		x_data = np.arange(action_dim)
	else:
		ax_cnv1 = None
		ax_cnv1 = None
		ax_cnv2 = None
		ax_cnv3 = None
		ax_dense_val = None
		ax_dense_adv = None
		ax_action = None
		x_data = None

	with model as m:
		c = m.conv2d(state_shape[0], hidden_size, 8, 4).prelu().plot_img('conv 1', 0, ax_cnv1)
		c = c.conv2d(hidden_size, hidden_size * 2, 4, 2).prelu().plot_img('conv 2', 0, ax_cnv2)
		c = c.conv2d(hidden_size * 2, hidden_size * 2, 3, 1).prelu().plot_img('conv 3', 0, ax_cnv3)
		f = c.flatten()

		fc = f.calc_output_shape(state_shape)

		val = f.dense(fc, 512).prelu().dense(512, 512).prelu().plot_dense('dense val', 0, ax_dense_val)
		val = val.dense(512, 1)
		adv = f.dense(fc, 512).prelu().dense(512, 512).prelu().plot_dense('dense adv', 0, ax_dense_adv)
		adv = adv.dense(512, action_dim)
		ave = adv.mean(1, keepdims=True)

		m.gate('merge', (lambda o, val, adv, ave: val + adv - ave), val, adv, ave).plot_action(
		    'action', 0, ax_action, x_data)

	for w in model.get_weights():
		nn.init.normal_(w, 0, 0.02)

	model.build()

	return model


def dqn_batchnorm_prelu(state_shape, action_dim, show_plot_name=None, hidden_size=32):
	"""DDQNモデルを生成する.

	Args:
		state_shape: バッチを除いた入力値形状 tuple.
		action_dim: アクション数、バッチを除いた出力値の要素数となる.
		show_plot_name: 変換途中データのプロット表示するならプロット名を、それ以外は None.
		hidden_size: 中間畳み込みレイヤのCH数.

	Returns:
		モデル.
	"""
	model = dnn.Root()

	if not plot_enabled:
		show_plot_name = None
	if show_plot_name:
		fig = plt.figure()
		ax_cnv1 = fig.add_subplot(2, 3, 1)
		ax_cnv1 = fig.add_subplot(2, 3, 1)
		ax_cnv2 = fig.add_subplot(2, 3, 2)
		ax_cnv3 = fig.add_subplot(2, 3, 3)
		ax_dense_val = fig.add_subplot(2, 3, 4)
		ax_dense_adv = fig.add_subplot(2, 3, 5)
		ax_action = fig.add_subplot(2, 3, 6)
		x_data = np.arange(action_dim)

	with model as m:
		c = m.conv2d(state_shape[0], hidden_size, 8, 4).batchnorm2d(hidden_size).prelu().plot_img(show_plot_name, 0, ax_cnv1)
		c = c.conv2d(hidden_size, hidden_size * 2, 4, 2).batchnorm2d(hidden_size * 2).prelu().plot_img(
		    show_plot_name, 0, ax_cnv2)
		c = c.conv2d(hidden_size * 2, hidden_size * 2, 3, 1).batchnorm2d(hidden_size * 2).prelu().plot_img(
		    show_plot_name, 0, ax_cnv3)
		f = c.flatten()

		fc = f.calc_output_shape(state_shape)

		val = f.dense(fc, 512).prelu().plot_dense(show_plot_name, 0, ax_dense_val)
		val = val.dense(512, 1)
		adv = f.dense(fc, 512).prelu().plot_dense(show_plot_name, 0, ax_dense_adv)
		adv = adv.dense(512, action_dim)
		ave = adv.mean(1, keepdims=True)

		m.gate('merge', (lambda o, val, adv, ave: val + adv - ave), val, adv, ave).plot_action(
		    show_plot_name, 0, ax_action, x_data)

	for w in model.get_weights():
		nn.init.normal_(w, 0, 0.02)

	model.build()

	return model


def dqn_batchnorm_prelu_no_rnd_init(state_shape, action_dim, show_plot_name=None, hidden_size=32):
	"""DDQNモデルを生成する.

	Args:
		state_shape: バッチを除いた入力値形状 tuple.
		action_dim: アクション数、バッチを除いた出力値の要素数となる.
		show_plot_name: 変換途中データのプロット表示するならプロット名を、それ以外は None.
		hidden_size: 中間畳み込みレイヤのCH数.

	Returns:
		モデル.
	"""
	model = dnn.Root()

	if not plot_enabled:
		show_plot_name = None
	if show_plot_name:
		fig = plt.figure()
		ax_cnv1 = fig.add_subplot(2, 3, 1)
		ax_cnv1 = fig.add_subplot(2, 3, 1)
		ax_cnv2 = fig.add_subplot(2, 3, 2)
		ax_cnv3 = fig.add_subplot(2, 3, 3)
		ax_dense_val = fig.add_subplot(2, 3, 4)
		ax_dense_adv = fig.add_subplot(2, 3, 5)
		ax_action = fig.add_subplot(2, 3, 6)
		x_data = np.arange(action_dim)

	with model as m:
		c = m.nop()
		c = c.conv2d(state_shape[0], hidden_size, 8, 4).batchnorm2d(hidden_size).prelu().plot_img(show_plot_name, 0, ax_cnv1)
		c = c.conv2d(hidden_size, hidden_size * 2, 4, 2).batchnorm2d(hidden_size * 2).prelu().plot_img(
		    show_plot_name, 0, ax_cnv2)
		c = c.conv2d(hidden_size * 2, hidden_size * 2, 3, 1).batchnorm2d(hidden_size * 2).prelu().plot_img(
		    show_plot_name, 0, ax_cnv3)
		f = c.flatten()

		fc = f.calc_output_shape(state_shape)

		val = f.dense(fc, 512).prelu().plot_dense(show_plot_name, 0, ax_dense_val)
		val = val.dense(512, 1)
		adv = f.dense(fc, 512).prelu().plot_dense(show_plot_name, 0, ax_dense_adv)
		adv = adv.dense(512, action_dim)
		ave = adv.mean(1, keepdims=True)

		m.gate('merge', (lambda o, val, adv, ave: val + adv - ave), val, adv, ave).plot_action(
		    show_plot_name, 0, ax_action, x_data)

	model.build()

	return model