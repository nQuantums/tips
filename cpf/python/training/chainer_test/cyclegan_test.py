import os
import random
import inspect
import dnn
from dnn import np
from dnn import chainer
from dnn import F
from dnn import L
from dnn import Variable
import cv2
import imgutil

xp = None
test = False
bgr_to_dnn = imgutil.bgr_to_pm
dnn_to_bgr = imgutil.pm_to_bgr
bgr_to_dnn_batch = imgutil.bgr_to_pm_batch


def cal_l2_sum(h, t):
	return F.sum((h - t)**2) / np.prod(h.shape)


def loss_func_rec_l1(x_out, t):
	return F.mean_absolute_error(x_out, t)


def loss_func_rec_l2(x_out, t):
	return F.mean_squared_error(x_out, t)


def loss_func_adv_dis_fake(y_fake):
	return cal_l2_sum(y_fake, 0.1)


def loss_func_adv_dis_real(y_real):
	return cal_l2_sum(y_real, 0.9)


def loss_func_adv_gen(y_fake):
	return cal_l2_sum(y_fake, 0.9)


def cbr(self, ch0, ch1, bn=True, sample='down', activation=dnn.Node.prelu, dropout=False, noise=False):
	with self.model('cbr') as m:
		if sample == 'down':
			ksize, stride, pad = 4, 2, 1
		elif sample == 'none-9':
			ksize, stride, pad = 9, 1, 4
		elif sample == 'none-7':
			ksize, stride, pad = 7, 1, 3
		elif sample == 'none-5':
			ksize, stride, pad = 5, 1, 2
		else:
			ksize, stride, pad = 3, 1, 1

		h = m
		if sample == "up":
			h = h.unpool2d(2, 2, 0, cover_all=False)
		h = h.conv2d(ch0, ch1, ksize, stride, pad, initialW=chainer.initializers.Normal(0.02))
		if bn:
			h = h.batchnorm(ch1, use_gamma=False if noise else True)
		if noise and not test:
			h = h.funcs('add_noise', lambda x: 0.2 * xp.random.randn(*x.data.shape, dtype=xp.float32))
		if dropout:
			h = h.dropout(train=not test)
		if activation is not None:
			h = activation(h)
		return m


def resblk(self, ch, bn=True, activation=dnn.Node.prelu):
	with self.model('resblk') as m:
		raw = m.nop()
		h = raw.conv2d(ch, ch, 3, 1, 1)
		if bn:
			h = h.batchnorm(ch)
		h = activation(h)
		h = h.conv2d(ch, ch, 3, 1, 1)
		if bn:
			h = h.batchnorm(ch)
		m.gate('add', (lambda a, b, _: a + b), h, raw)
		return m


def opt_gen(alpha=0.0002, beta1=0.5):
	return chainer.optimizers.Adam(alpha=alpha, beta1=beta1)


def resblk_9(in_ch, out_ch):
	with dnn.Model(opt_gen()) as m:
		m.cbr(in_ch, 32, bn=True, sample='none-7')\
        .cbr(32, 64, bn=True, sample='down')\
        .cbr(64, 128, bn=True, sample='down')\
        .resblk(128, bn=True)\
        .resblk(128, bn=True)\
        .resblk(128, bn=True)\
        .resblk(128, bn=True)\
        .resblk(128, bn=True)\
        .resblk(128, bn=True)\
        .resblk(128, bn=True)\
        .resblk(128, bn=True)\
        .resblk(128, bn=True)\
        .cbr(128, 64, bn=True, sample='up')\
        .cbr(64, 32, bn=True, sample='up')\
        .cbr(32, out_ch, bn=True, sample='none-7', activation=dnn.Node.tanh)
		return m


def discriminator(in_ch=3, n_down_layers=4):
	with dnn.Model(opt_gen()) as m:
		base = 64
		h = m.cbr(in_ch, 64, bn=False, sample='down', activation=dnn.Node.leaky_relu, dropout=False, noise=True)
		for _ in range(1, n_down_layers):
			h = h.cbr(base, base * 2, bn=True, sample='down', activation=dnn.Node.leaky_relu, dropout=False, noise=True)
			base *= 2
		h = h.cbr(base, 1, bn=False, sample='none', activation=None, dropout=False, noise=True)
		return m


class CycleGan(dnn.Models):
	"""CycleGanのモデル.
	"""

	def __init__(self, image_width, image_height, in_ch, out_ch, batch_size, mode, learning_rate_g=0.0002, learning_rate_d=0.0002, lambda1=10.0, lambda2=3.0, learning_rate_anneal=0.0, learning_rate_anneal_interval=1000):
		"""モデル初期化.

		Args:
			image_width: 入力画像幅(px).
			image_height: 入力画像高さ(px).
			in_ch: 入力CH数.
			out_ch: 出力CH数.
			batch_size: バッチサイズ.
			mode: モード、次のうちのどれか train / test.
			args: コマンドライン引数の内モデル作成に必要な部分のみ抜き出したリスト.
		"""

		if mode == "train":
			# モデル構築
			self.gen_g = resblk_9(in_ch, out_ch)
			self.gen_f = resblk_9(in_ch, out_ch)
			self.dis_x = discriminator(in_ch)
			self.dis_y = discriminator(in_ch)

			# 推論の関数作成
			self.build('cyclegan.prediction')

			# ハイパーパラメータセットアップ
			self.lambda1 = lambda1
			self.lambda2 = lambda2
			self.learning_rate_anneal = learning_rate_anneal
			self.learning_rate_anneal_interval = learning_rate_anneal_interval
			self.image_width = image_width
			self.image_height = image_height
			self.iter = 0
			self.max_buffer_size = 50

			# gen_g の出力を蓄えておくリングバッファ
			self.ring_x = np.zeros((self.max_buffer_size, batch_size, in_ch, self.image_height, self.image_width), dtype=dnn.dtype)
			self.ring_x_len = 0 # バッファ内有効要素数
			self.ring_x_next = 0 # 次回バッファに書き込み時のインデックス
			# gen_f の出力を蓄えておくリングバッファ
			self.ring_y = np.zeros((self.max_buffer_size, batch_size, in_ch, self.image_height, self.image_width), dtype=dnn.dtype)
			self.ring_y_len = 0 # バッファ内有効要素数
			self.ring_y_next = 0 # 次回バッファに書き込み時のインデックス
		# elif mode == "test":
		# 	# テストモード用コマンドライン引数解析
		# 	parser = argparse.ArgumentParser(description='Testing for CycleGAN.')
		# 	parser.add_argument("-d", "--dataprefix", default="", help='Prefix for trained data file select.')
		# 	parser.add_argument("-s", "--selectgen", default="gen_g", choices=['gen_g', 'gen_f'], help='Generator name "gen_g" or "gen_f".')
		# 	parser.add_argument("-r", "--resblock", default="9", help='Number of Generator_ResBlock.', choices=['6', '9', '11', '12'])
		# 	args = parser.parse_args(args)

		# 	self.trainedDataPrefix = args.dataprefix

		# 	# テスト用ニューラルネットワークセットアップ
		# 	if args.selectgen == "gen_g":
		# 		self.gen = generatorResBlock[args.resblock](in_ch, out_ch)
		# 		self.chains["gen_g"] = self.gen
		# 	elif args.selectgen == "gen_f":
		# 		self.gen = generatorResBlock[args.resblock](in_ch, out_ch)
		# 		self.chains["gen_f"] = self.gen
		# 	else:
		# 		print(args.selectgen, "このジェネレータは存在しません。")
		# 		raise
		else:
			raise Exception("Not implemented mode. '{}'".format(mode))

	def train(self, x, t, saveEval, showEval):
		"""入力データと教師データを用いて学習を実行する.
		# Args:
			x: 入力データ. ※dnn.to_xp() で変換済みでなければならない
			t: 教師データ. ※dnn.to_xp() で変換済みでなければならない
			saveEval: 評価用画像を所定のディレクトリに保存するかどうか.
			showEval: 評価用画像を表示するかどうか.
		"""
		self.iter += 1

		x = x if isinstance(x, Variable) else Variable(x)
		y = t if isinstance(t, Variable) else Variable(t)

		x_y = self.gen_g(x)
		x_y_copy = Variable(self.add_x_to_ring_and_get(x_y.data))
		x_y_x = self.gen_f(x_y)

		y_x = self.gen_f(y)
		y_x_copy = Variable(self.add_y_to_ring_and_get(y_x.data))
		y_x_y = self.gen_g(y_x)

		if self.learning_rate_anneal > 0 and self.iter % self.learning_rate_anneal_interval == 0:
			if self.gen_g.optimizer.alpha > self.learning_rate_anneal:
				self.gen_g.optimizer.alpha -= self.learning_rate_anneal
			if self.gen_f.optimizer.alpha > self.learning_rate_anneal:
				self.gen_f.optimizer.alpha -= self.learning_rate_anneal
			if self.dis_x.optimizer.alpha > self.learning_rate_anneal:
				self.dis_x.optimizer.alpha -= self.learning_rate_anneal
			if self.dis_y.optimizer.alpha > self.learning_rate_anneal:
				self.dis_y.optimizer.alpha -= self.learning_rate_anneal

		self.gen_g.zerograds()
		self.gen_f.zerograds()
		self.dis_x.zerograds()
		self.dis_y.zerograds()

		loss_dis_y_fake = loss_func_adv_dis_fake(self.dis_y(x_y_copy))
		loss_dis_y_real = loss_func_adv_dis_real(self.dis_y(y))
		loss_dis_y = loss_dis_y_fake + loss_dis_y_real
		# chainer.report({'loss': loss_dis_y}, self.dis_y)
		print('loss', loss_dis_y.data)

		loss_dis_x_fake = loss_func_adv_dis_fake(self.dis_x(y_x_copy))
		loss_dis_x_real = loss_func_adv_dis_real(self.dis_x(x))
		loss_dis_x = loss_dis_x_fake + loss_dis_x_real
		# chainer.report({'loss': loss_dis_x}, self.dis_x)
		print('loss', loss_dis_x.data)

		loss_dis_y.backward()
		loss_dis_x.backward()

		self.dis_y.optimizer.update()
		self.dis_x.optimizer.update()

		loss_gen_g_adv = loss_func_adv_gen(self.dis_y(x_y))
		loss_gen_f_adv = loss_func_adv_gen(self.dis_x(y_x))

		loss_cycle_x = self.lambda1 * loss_func_rec_l1(x_y_x, x)
		loss_cycle_y = self.lambda1 * loss_func_rec_l1(y_x_y, y)
		loss_gen = self.lambda2 * loss_gen_g_adv + self.lambda2 * loss_gen_f_adv + loss_cycle_x + loss_cycle_y
		loss_gen.backward()
		self.gen_f.optimizer.update()
		self.gen_g.optimizer.update()

		# chainer.report({'loss_rec': loss_cycle_y}, self.gen_g)
		# chainer.report({'loss_rec': loss_cycle_x}, self.gen_f)
		# chainer.report({'loss_adv': loss_gen_g_adv}, self.gen_g)
		# chainer.report({'loss_adv': loss_gen_f_adv}, self.gen_f)
		print('loss_rec', loss_cycle_y.data)
		print('loss_rec', loss_cycle_x.data)
		print('loss_adv', loss_gen_g_adv.data)
		print('loss_adv', loss_gen_f_adv.data)

		if saveEval or showEval:
			w_in = self.image_width
			h_in = self.image_height
			img = np.zeros((x.data.shape[1], h_in * 2, w_in * 3), dtype=dnn.dtype)
			cells = [
				dnn.to_cpu(x.data[0]), dnn.to_cpu(x_y.data[0]), dnn.to_cpu(x_y_x.data[0]),
				dnn.to_cpu(y.data[0]), dnn.to_cpu(y_x.data[0]), dnn.to_cpu(y_x_y.data[0])
			]
			i = 0
			for r in range(2):
				rs = r * h_in
				for c in range(3):
					cs = c * w_in
					img[:, rs:rs + h_in, cs:cs + w_in] = cells[i]
					i += 1
			img = dnn_to_bgr(img)

			# if saveEval:
			# 	f = os.path.normpath(os.path.join(dnn.GetEvalDataDir(), "eval.png"))
			# 	cv2.imwrite(f, img)
			# 	uploader.Upload(f)
			if showEval:
				cv2.imshow("CycleGAN", img)

		return

	def test(self, x):
		"""学習済みのモデルを使用して変換を行う.
		# Args:
			x: 入力データ. ※dnn.to_xp() で変換済みでなければならない
		# Returns:
			出力データchainer.Variable
		"""
		return self.gen_f(x if isinstance(x, Variable) else Variable(x))

	def add_x_to_ring_and_get(self, data):
		"""指定値をリングバッファに追加し、リングバッファ内からランダムに選んだ値を返す.
		# Args:
			data: リングバッファに追加する値.
		# Returns:
			入力値またはリングバッファ内の値.
		"""
		# リングバッファキャパシティ
		n = len(self.ring_x)

		# とりあえず書き込む
		self.ring_x[self.ring_x_next, :] = dnn.to_cpu(data)
		self.ring_x_next = (self.ring_x_next + 1) % n
		if self.ring_x_len < n:
			self.ring_x_len += 1
			return data # バッファが埋まるまでは入力値をそのまま使用

		# 50%の確率で入力値をそのまま返す
		if np.random.rand() < 0.5:
			return data

		# リングバッファ内からランダムに選んで返す
		id = np.random.randint(0, n)
		return dnn.to_xp(self.ring_x[id, :].reshape(data.shape[:2] + (self.image_height, self.image_width)))

	def add_y_to_ring_and_get(self, data):
		"""指定値をリングバッファに追加し、リングバッファ内からランダムに選んだ値を返す.
		# Args:
			data: リングバッファに追加する値.
		# Returns:
			入力値またはリングバッファ内の値.
		"""
		# リングバッファキャパシティ
		n = len(self.ring_y)

		# とりあえず書き込む
		self.ring_y[self.ring_y_next, :] = dnn.to_cpu(data)
		self.ring_y_next = (self.ring_y_next + 1) % n
		if self.ring_y_len < n:
			self.ring_y_len += 1
			return data # バッファが埋まるまでは入力値をそのまま使用

		# 50%の確率で入力値をそのまま返す
		if np.random.rand() < 0.5:
			return data

		# リングバッファ内からランダムに選んで返す
		id = np.random.randint(0, n)
		return dnn.to_xp(self.ring_y[id, :].reshape(data.shape[:2] + (self.image_height, self.image_width)))

	def add_x_to_ring_and_get_imshow(self, data):
		ret = self.add_x_to_ring_and_get(data)
		img = np.zeros((data.shape[1], self.image_height, self.image_width * 2), dtype=dnn.dtype)
		img[:, :, :self.image_width] = dnn.to_cpu(data[0])
		img[:, :, self.image_width:] = dnn.to_cpu(ret[0])
		cv2.imshow("x", dnn_to_bgr(img))
		return ret

	def add_y_to_ring_and_get_imshow(self, data):
		ret = self.add_y_to_ring_and_get(data)
		img = np.zeros((data.shape[1], self.image_height, self.image_width * 2), dtype=dnn.dtype)
		img[:, :, :self.image_width] = dnn.to_cpu(data[0])
		img[:, :, self.image_width:] = dnn.to_cpu(ret[0])
		cv2.imshow("y", dnn_to_bgr(img))
		return ret


def run_train():
	global xp

	# 呼び出しを簡単にするため Node のメソッドとして組み込む
	dnn.Node.cbr = cbr
	dnn.Node.resblk = resblk
	dnn.startup(0) # 0番のGPU使用
	xp = dnn.xp

	batch_size = 3
	image_width = 640
	image_height = 360
	image_size = (image_height, image_width)

	# モデル構築
	m = CycleGan(640, 360, 3, 3, batch_size, 'train')

	# 学習済みデータがあれば読み込む
	print("Loading trained data...")
	dnn.load_if_exists('cycle_gan', m)
	print("Done.")

	m = dnn.to_xp(m)

	# バッチサイズ分のCPU上でのメモリ領域、一旦ここに展開してから to_xp すると無駄が少ない
	xcpu = np.zeros((batch_size, 3, image_height, image_width), dtype=dnn.dtype)
	tcpu = np.zeros((batch_size, 3, image_height, image_width), dtype=dnn.dtype)

	# データ用意
	x_caps = []
	t_caps = []
	for b in range(batch_size):
		cap = cv2.VideoCapture("c:/work/Fortnite.mp4")
		x_caps.append(cap)

		cap = cv2.VideoCapture("c:/work/DarkSouls3Full.mp4")
		t_caps.append(cap)

	x_start_frame = 0
	x_end_frame = int(x_caps[0].get(cv2.CAP_PROP_FRAME_COUNT))
	t_start_frame = 0
	t_end_frame = int(t_caps[0].get(cv2.CAP_PROP_FRAME_COUNT))
	frame_step = 100

	# 学習ループ
	request_quit = False
	iter_count = 0

	while True:
		if request_quit:
			break

		# バッチセットアップ
		for i in range(batch_size):
			x_cap = x_caps[i]
			x_cap.set(cv2.CAP_PROP_POS_FRAMES, random.randint(x_start_frame, x_end_frame))
			success, image = x_cap.read()
			if success:
				image = cv2.resize(image, (image_width, image_height), interpolation=cv2.INTER_AREA)
				# image = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
				# image = cv2.cvtColor(image, cv2.COLOR_GRAY2BGR)
				xcpu[i, :, :, :] = bgr_to_dnn(image)

			t_cap = t_caps[i]
			t_cap.set(cv2.CAP_PROP_POS_FRAMES, random.randint(t_start_frame, t_end_frame))
			success, image = t_cap.read()
			if success:
				image = cv2.resize(image, (image_width, image_height), interpolation=cv2.INTER_AREA)
				tcpu[i, :, :, :] = bgr_to_dnn(image)

		# 学習実行
		m.train(dnn.to_xp(xcpu), dnn.to_xp(tcpu), iter_count % 10 == 0, iter_count % 10 == 0)
		iter_count += 1

		# OpenCVウィンドウアクティブにしてESCキーで中断できるようにしておく
		k = cv2.waitKey(1)
		if k == 27:
			request_quit = True
			break

	# 学習結果を保存
	print("Saving trained data...")
	dnn.save('cycle_gan', m)
	print("Done.")


def run_test():
	global xp

	# 呼び出しを簡単にするため Node のメソッドとして組み込む
	dnn.Node.cbr = cbr
	dnn.Node.resblk = resblk
	dnn.startup(0) # 0番のGPU使用
	xp = dnn.xp

	batch_size = 1
	image_width = 640
	image_height = 360

	# モデル構築
	m = CycleGan(640, 360, 3, 3, batch_size, 'train')

	# 学習済みデータがあれば読み込む
	print("Loading trained data...")
	dnn.load_if_exists('cycle_gan', m)
	print("Done.")

	m = dnn.to_xp(m)

	# データ用意
	cap = cv2.VideoCapture("c:/work/ds3tamanegi.mp4")
	cap.set(cv2.CAP_PROP_POS_FRAMES, 14000)

	# バッチサイズ分のCPU上でのメモリ領域、一旦ここに展開してから to_xp すると無駄が少ない
	xcpu = np.zeros((batch_size, 3, image_height, image_width), dtype=dnn.dtype)

	# 動画再生ループ
	request_quit = False
	while not request_quit:
		success, image = cap.read()
		if success:
			image = cv2.resize(image, (image_width, image_height), interpolation=cv2.INTER_AREA)
			xcpu[0, :, :, :] = bgr_to_dnn(image)

		y = m.test(dnn.to_xp(xcpu))
		y = dnn.to_cpu(y.data[0])
		image = dnn_to_bgr(y)

		cv2.imshow("CycleGAN", image)

		# OpenCVウィンドウアクティブにしてESCキーで中断できるようにしておく
		k = cv2.waitKey(1)
		if k == 27:
			request_quit = True
			break

if __name__ == '__main__':
	if test:
		run_test()
	else:
		run_train()
