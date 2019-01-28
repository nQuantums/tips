import dnn
from dnn import np
from dnn import chainer
from dnn import F
from dnn import L
from dnn import Variable

def dqn(history_size, hidden_size, action_size):
	fc = hidden_size * 704

	with dnn.Model(chainer.optimizers.Adam()) as m:
		flat = m\
		.conv2d(history_size, hidden_size, 8, 4).relu()\
		.conv2d(hidden_size, hidden_size * 2, 4, 2).relu()\
		.conv2d(hidden_size * 2, hidden_size * 2, 3, 1).relu()\
		.reshape((1, fc))

		a = flat.dense(fc, 512).relu().dense(512, action_size)
		v = flat.dense(fc, 512).relu().dense(512, 1).tile((1, action_size))
		av = a.average(1, keepdims=True).tile((1, action_size))

		m.gate('merge', (lambda a, v, av, o: a + v - av), a, v, av)
		m.build('dqn_prediction.py', 'dqn')
		return m

m = dqn(4, 32, 6)

with open("dqn_model.dot", mode='w') as f:
	f.write(m.dot_code)


ch = 4
w = 210
h = 160
x = Variable(np.arange(ch * w * h, dtype=np.float32).reshape((1, ch, h, w)))
x = m(x)

print(x)

