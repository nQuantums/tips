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

m = dnn.to_gpu(m)

for i in range(10):
	m.zerograds()
	x = Variable(xp.random.uniform(0, 1, (1, 32)).astype(xp.float32))
	y = m(x)
	t = x * 10
	loss = F.mean_squared_error(y, t)
	loss.backward()
	print(loss)
	m.optimizer.update()

m = dnn.to_cpu(m)
dnn.save("modeldata", m)

# g = ccg.build_computational_graph(y)
# with open('afe_graph.dot', 'w') as o:
# 	o.write(g.dump())
