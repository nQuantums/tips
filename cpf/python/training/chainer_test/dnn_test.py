import dnn
from dnn import chainer
from dnn import F
from dnn import L
from dnn import Variable
import chainer.computational_graph as ccg

dnn.startup(-1) # 0番のGPU使用
xp = dnn.xp

def separate(g, x, out):
	shape = x.shape
	x = x.reshape((2, shape[0], shape[1] // 2))
	return out[0](x[0]), out[1](x[1])

def concat(g, x, out):
	h = F.concat((x[0], x[1]), axis=1)
	return out[0](h)

m = dnn.Model(chainer.optimizers.Adam()) # とりあえず Model 作る
g = m.glue([m.linear(32, 32).relu()], separate) # 全結合層生成して relu 活性化関数セットしたものを入力し、それを２つに分ける Glue 作る
g = m.glue([g.linear(16, 16), g.linear(16, 16)], concat) # ２つの入力を１つに結合する Glue 作る
m.assign_output(g.linear(32, 32)) # 全結合層を１つ作ってそれを出力とする
m.build()

with open("dnn_test.dot", mode='w') as f:
	f.write(m.dot_code)
with open("dnn_test_prediction.py", mode='w') as f:
	f.write(m.code)

# 所定のデバイスメモリに転送
m = dnn.to_xp(m)

# とりあえず入力値を単純に10倍にするネットを目標とする
for i in range(10000):
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

# g = ccg.build_computational_graph(y)
# with open('afe_graph.dot', 'w') as o:
# 	o.write(g.dump())
