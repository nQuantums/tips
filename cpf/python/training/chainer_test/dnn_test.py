import math
import matplotlib.pyplot as plt
import dnn
from dnn import np
from dnn import optim
from dnn import F
from dnn import Variable

dnn.startup(0) # 0番のGPU使用
xp = dnn.xp # numpy または cupy
itr = 0

figx, ax = plt.subplots()
figx_in_pred, ax_in_pred = plt.subplots()

def calc_width_height(length):
	w = int(math.sqrt(length))
	while length % w != 0:
		w += 1
	h = length // w
	return (h, w)

def plot(self, batch, ax):
	def proc(x):
		img = dnn.to_cpu(x.data[batch])
		img = img.reshape(calc_width_height(img.size))
		ax.cla()
		ax.imshow(img)
		ax.set_title("frame {}".format(itr))
		return x
	return self.funcs('plot', proc)

# Node にメソッド追加してみる
dnn.Node.plot = plot

# モデル構築
batch = 1
model = dnn.Model(optim.Adam())
with model.module as m:
	m\
	.conv2d(1, 1, 3, 1, 1).prelu()\
	.conv2d(1, 1, 3, 1, 1).prelu()\
	.conv2d(1, 1, 3, 1, 1).prelu()\
	.reshape((batch, 1024))\
	.dense(1024, 512).prelu()\
	.plot(0, ax_in_pred)\
	.dense(512, 512).prelu()\
	.dense(512, 1024).prelu()\
	.reshape((batch, 1, 32, 32)).prelu()\
	.conv2d(1, 1, 3, 1, 1).prelu()\
	.conv2d(1, 1, 3, 1, 1).prelu()\
	.conv2d(1, 1, 3, 1, 1)

model.build('dnn_test_prediction.py')

# モデルデータが保存済みなら読み込み
dnn.load_if_exists('dnn_test.h5', model)

with open("dnn_test.dot", mode='w') as f:
	f.write(model.dot_code)

# 所定のデバイスメモリに転送
model = dnn.to_device(model)

# とりあえず入力値を単純に10倍にするネットを目標とする
for i in range(100):
	model.zero_grad()

	x = Variable(xp.random.uniform(0, 1, (1, 1, 32, 32)).astype(xp.float32))
	y = model(x)
	t = x * 10

	img = np.concatenate((dnn.to_cpu(x.data[0, 0]), dnn.to_cpu(y.data[0, 0])), axis=1)
	ax.cla()
	ax.imshow(img)
	ax.set_title("frame {}".format(i))

	plt.pause(0.01)

	loss = F.mean_squared_error(y, t)
	loss.backward()
	print(loss)
	model.step()

	itr += 1

# 保存しておく
dnn.save('dnn_test.h5', model)
