import dnn
from dnn import nn

def dqn(lr, state_shape, action_dim, hidden_size):
	"""DDQNモデルを生成する.

	Args:
		lr: 学習率.
		state_shape: バッチを除いた入力値形状 tuple.
		action_dim: アクション数、バッチを除いた出力値の要素数となる.
		hidden_size: 中間畳み込みレイヤのCH数.

	Returns:
		モデル.
	"""
	model = dnn.Model()

	with model.module as m:
		f = m\
        .conv2d(state_shape[0], hidden_size, 8, 4).relu()\
        .conv2d(hidden_size, hidden_size * 2, 4, 2).relu()\
        .conv2d(hidden_size * 2, hidden_size * 2, 3, 1).relu()\
        .flatten()

		fc = f.calc_output_shape(state_shape)

		val = f.dense(fc, 512).relu().dense(512, 1)
		adv = f.dense(fc, 512).relu().dense(512, action_dim)
		ave = adv.data().mean(1, keepdims=True)

		m.gate('merge', (lambda o, val, adv, ave: val + adv - ave), val, adv, ave)

	for w in model.module.get_weights():
		nn.init.normal_(w, 0, 0.02)

	if lr:
		model.optimizer_Adamax(lr)

	model.build('dqn.prediction.py', 'dqn')

	return model
