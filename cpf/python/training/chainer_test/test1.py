import dnn
from dnn import np
from dnn import cp
from dnn import chainer
from dnn import F
from dnn import L
import chainer.computational_graph as ccg

dnn.startup(-1)
xp = dnn.xp

IN_LENGTH = 2
OUT_LENGTH = 2
MID_LENGTH = 2

m = dnn.Model(chainer.optimizers.SGD())
x = m.input(L.Linear(IN_LENGTH, MID_LENGTH), F.relu)
# x = m.glue(None, L.Linear(MID_LENGTH, MID_LENGTH), F.relu, x)
# x = m.glue(None, L.Linear(MID_LENGTH, MID_LENGTH), F.relu, x)
# x = m.glue(None, L.Linear(MID_LENGTH, MID_LENGTH), F.relu, x)
# x = m.glue(None, L.Linear(MID_LENGTH, MID_LENGTH), F.relu, x)
x = m.output(L.Linear(MID_LENGTH, OUT_LENGTH), F.relu, x)
m.compile()
m = dnn.to_gpu(m)

# x = dnn.to_gpu(np.random.uniform(0, 1, (1, 32)).astype(np.float32))
# t = dnn.to_gpu(np.random.uniform(0, 1, (1, 32)).astype(np.float32))

for i in range(10):
	m.zerograds()
	x = xp.random.uniform(0, 1, (1, IN_LENGTH)).astype(xp.float32)
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
