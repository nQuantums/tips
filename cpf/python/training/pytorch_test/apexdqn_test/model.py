import math
import matplotlib.pyplot as plt

import dnn
from dnn import np
from dnn import nn

plot_enabled = False
show_plot = False


def calc_width_height(length):
	w = int(math.sqrt(length))
	while length % w != 0:
		w += 1
	h = length // w
	return (h, w)


def plot_img(self, name, batch, ax):

	def proc(x):
		if show_plot:
			img = x.detach()[batch].sum(dim=0).cpu().numpy()
			ax.cla()
			ax.imshow(img)
			ax.set_title(f"{name} : After conv")
		return x

	return self.funcs('plot_img', proc)


def plot_dense(self, name, batch, ax):

	def proc(x):
		if show_plot:
			img = x.detach()[batch].cpu().numpy()
			img = img.reshape(calc_width_height(img.size))
			ax.cla()
			ax.imshow(img)
			ax.set_title(f"{name} : Dense")
			plt.pause(0.001)
		return x

	return self.funcs('plot_dense', proc)


def plot_action(self, name, batch, ax, x_data):

	def proc(x):
		if show_plot:
			ax.cla()
			ax.plot(x_data, x.detach()[batch].cpu().numpy())
			ax.set_title(f"{name} : Q")
			plt.pause(0.001)
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

		# ax_in = fig.add_subplot(1, 2, 1)
		# ax_action = fig.add_subplot(1, 2, 2)

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
		# if show_plot_name:
		# 	c = c.plot_img(show_plot_name, 0, ax_in)

		c = c.conv2d(state_shape[0], hidden_size, 8, 4).relu()
		if show_plot_name:
			c = c.plot_img(show_plot_name, 0, ax_cnv1)
		c = c.conv2d(hidden_size, hidden_size * 2, 4, 2).relu()
		if show_plot_name:
			c = c.plot_img(show_plot_name, 0, ax_cnv2)
		c = c.conv2d(hidden_size * 2, hidden_size * 2, 3, 1).relu()
		if show_plot_name:
			c = c.plot_img(show_plot_name, 0, ax_cnv3)
		f = c.flatten()

		fc = f.calc_output_shape(state_shape)

		val = f.dense(fc, 512).relu()
		if show_plot_name:
			val = val.plot_dense(show_plot_name, 0, ax_dense_val)
		val = val.dense(512, 1)
		adv = f.dense(fc, 512).relu()
		if show_plot_name:
			adv = adv.plot_dense(show_plot_name, 0, ax_dense_adv)
		adv = adv.dense(512, action_dim)
		ave = adv.mean(1, keepdims=True)

		output = m.gate('merge', (lambda o, val, adv, ave: val + adv - ave), val, adv, ave)
		if show_plot_name:
			output.plot_action(show_plot_name, 0, ax_action, x_data)

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

		# ax_in = fig.add_subplot(1, 2, 1)
		# ax_action = fig.add_subplot(1, 2, 2)

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
		# if show_plot_name:
		# 	c = c.plot_img(show_plot_name, 0, ax_in)

		c = c.conv2d(state_shape[0], hidden_size, 8, 4).prelu()
		if show_plot_name:
			c = c.plot_img(show_plot_name, 0, ax_cnv1)
		c = c.conv2d(hidden_size, hidden_size * 2, 4, 2).prelu()
		if show_plot_name:
			c = c.plot_img(show_plot_name, 0, ax_cnv2)
		c = c.conv2d(hidden_size * 2, hidden_size * 2, 3, 1).prelu()
		if show_plot_name:
			c = c.plot_img(show_plot_name, 0, ax_cnv3)
		f = c.flatten()

		fc = f.calc_output_shape(state_shape)

		val = f.dense(fc, 512).prelu()
		if show_plot_name:
			val = val.plot_dense(show_plot_name, 0, ax_dense_val)
		val = val.dense(512, 1)
		adv = f.dense(fc, 512).prelu()
		if show_plot_name:
			adv = adv.plot_dense(show_plot_name, 0, ax_dense_adv)
		adv = adv.dense(512, action_dim)
		ave = adv.mean(1, keepdims=True)

		output = m.gate('merge', (lambda o, val, adv, ave: val + adv - ave), val, adv, ave)
		if show_plot_name:
			output.plot_action(show_plot_name, 0, ax_action, x_data)

	for w in model.get_weights():
		nn.init.normal_(w, 0, 0.02)

	model.build()

	return model
