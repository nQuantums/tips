import dnn
from dnn import np
from dnn import chainer
from dnn import F
from dnn import L
from dnn import Variable
import matplotlib.pyplot as plt

dnn.startup(-1) # 0番のGPU使用
xp = dnn.xp # numpy または cupy

# x を出力先ノード数分に分割する
def split(x, out):
	n = len(out)
	shape = x.shape
	x = x.reshape((n, shape[0], shape[1] // n))
	return tuple(e for e in x)

# 分割されている x を１つに結合する
def concat(x, out):
	return F.concat(tuple(e for e in x), axis=1)

# 入力を２つに分割して dense 通して１つに結合するモデルを生成する
def diamond(self, ch):
	with self.model('diamond') as m: 
		g = m.gate(split)
		g = m.gate(concat, g.dense(ch // 2, ch // 2).relu(), g.dense(ch // 2, ch // 2).relu())
		return m

# モデル構築
dnn.Node.diamond = diamond # 呼び出しが面倒なので Node に diamond 機能をもたせる
with dnn.Model(chainer.optimizers.Adam()) as m: # とりあえず Model 作る
	m\
	.conv2d(1, 1, 3, 1, 1)\
	.conv2d(1, 1, 3, 1, 1)\
	.conv2d(1, 1, 3, 1, 1)\
	.conv2d(1, 1, 3, 1, 1)\
	.conv2d(1, 1, 3, 1, 1)\
	.conv2d(1, 1, 3, 1, 1)\
	.conv2d(1, 1, 3, 1, 1)
	# h = m.dense(32, 8).gate(split) # dense 通して分割用 Gate に通る様に
	# h = m.gate(concat, h.diamond(4), h.diamond(4)).dense(8, 32) # 分割用 Gate で２つの diamond モデル通す様に分割してそれを結合する Gate を通る様にする
	m.build('dnn_test_prediction.py', 'predmodule')

trainables = sorted([pl for pl in m.trainables()], key=lambda pl: pl[0])
def print_weights():
	for pl in trainables:
		print(pl[0], pl[1].W, pl[1].b)

print('Initial')
print_weights()

# モデルデータが保存済みなら読み込み

print('Loading...')
print('Optimizer')
dnn.load_if_exists("dnn_test", m, lambda v: isinstance(v, chainer.Optimizer))
print_weights()
print('Chain')
dnn.load_if_exists("dnn_test", m, lambda v: isinstance(v, chainer.Chain))
print_weights()
print('Done.')



with open("dnn_test.dot", mode='w') as f:
	f.write(m.dot_code)

# 所定のデバイスメモリに転送
m = dnn.to_xp(m)

figx, ax = plt.subplots()

# とりあえず入力値を単純に10倍にするネットを目標とする
for i in range(10):
	m.zerograds()
	x = Variable(xp.random.uniform(0, 1, (1, 1, 32, 32)).astype(xp.float32))
	y = m(x)
	t = x * 10

	img = np.concatenate((dnn.to_cpu(x.data[0, 0]), dnn.to_cpu(y.data[0, 0])), axis=1)
	ax.cla()
	ax.imshow(img)
	ax.set_title("frame {}".format(i))

	plt.pause(0.01)

	loss = F.mean_squared_error(y, t)
	loss.backward()
	print(loss)
	m.optimizer.update()

print_weights()

# 保存時はCPUメモリにしないとだめ
m = dnn.to_cpu(m)
dnn.save("dnn_test", m)

# g = ccg.build_computational_graph(y)
# with open('afe_graph.dot', 'w') as o:
# 	o.write(g.dump())
