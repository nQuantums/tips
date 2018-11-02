import dnn
from dnn import np
from dnn import cp
from dnn import chainer
from dnn import F
from dnn import L
from dnn import Variable
import chainer.computational_graph as ccg

dnn.startup(0)
xp = dnn.xp

IN_LENGTH = 32
OUT_LENGTH = 32
MID_LENGTH = 32

m = dnn.Model(chainer.optimizers.Adam())
x = m.input(L.Linear(IN_LENGTH, MID_LENGTH), None)
x = m.layer(L.Linear(MID_LENGTH, MID_LENGTH), None, dnn.Lambda(lambda a, x: a.pl(x) * 2, pl=x))
x = m.layer(L.Linear(MID_LENGTH, MID_LENGTH), None, x)
x = m.layer(L.Linear(MID_LENGTH, MID_LENGTH), None, x)
x = m.layer(L.Linear(MID_LENGTH, MID_LENGTH), None, x)
x = m.layer(L.Linear(MID_LENGTH, MID_LENGTH), None, x)
x = m.output(L.Linear(MID_LENGTH, OUT_LENGTH), None, x)
m.compile()
m = dnn.to_gpu(m)

# x = dnn.to_gpu(np.random.uniform(0, 1, (1, 32)).astype(np.float32))
# t = dnn.to_gpu(np.random.uniform(0, 1, (1, 32)).astype(np.float32))

for i in range(10):
	m.zerograds()
	x = Variable(xp.random.uniform(0, 1, (1, IN_LENGTH)).astype(xp.float32))
	y = m(x)
	t = x * 10
	loss = F.mean_squared_error(y, t)
	loss.backward()
	print(loss)
	m.optimizer.update()

m = dnn.to_cpu(m)
dnn.save("modeldata", m)

g = ccg.build_computational_graph(y)
with open('afe_graph.dot', 'w') as o:
	o.write(g.dump())
