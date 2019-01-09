import random
import numpy as np
from numpy.lib.stride_tricks import as_strided
import pandas as pd
import matplotlib.pyplot as plt
import cv2


def load_from_csv(csv_filepath):
	dtypes_csv = [('time', 'str'), ('open', 'f4'), ('high', 'f4'), ('low', 'f4'), ('close', 'f4'), ('volume', 'i4')]
	df = pd.read_csv(csv_filepath, names=('time', 'open', 'high', 'low', 'close', 'volume'), parse_dates=[0], dtype=dtypes_csv)
	return df


def csv_to_binary(csv_filepath, binary_filepath):
	df = load_from_csv(csv_filepath)
	df['time'] = df['time'].values.astype('u8') // 1000000000
	df['open'] = (df['open'] * 1000).astype('i4')
	df['high'] = (df['high'] * 1000).astype('i4')
	df['low'] = (df['low'] * 1000).astype('i4')
	df['close'] = (df['close'] * 1000).astype('i4')
	df = df.drop('volume', axis=1)
	records = df.to_records(index=False)
	with open(binary_filepath, 'wb') as f:
		f.write(records.tobytes())


def read_records(binary_filepath):
	dtypes = [('time', 'u8'), ('open', 'i4'), ('high', 'i4'), ('low', 'i4'), ('close', 'i4')]
	with open(binary_filepath, 'rb') as f:
		b = np.frombuffer(f.read(), dtype=dtypes)
	return b


def records_to_dataframe(records):
	df = pd.DataFrame(records)
	df = df.set_index(pd.to_datetime(df['time'], unit='s')).drop('time', axis=1)
	return df


def records_to_time(records):
	return records[['time']].view(('u8', 1))


def records_to_values(records):
	return records[['open', 'high', 'low', 'close']].view(('i4', 4)).copy()


def get_separation_indices(records):
	interval = 60 * 60
	time = records[['time']].astype('u8')
	dif_time = np.diff(time)
	time_areas = np.nonzero((dif_time > interval).astype('i4'))[0]
	time_areas += 1
	return time_areas


def tickdata(filepath):
	"""binary to pandas DataFrame using numpy.

	参考: (´・ω・｀；)ﾋｨｨｯ　すいません - pythonでMT4のヒストリファイルを読み込む
	http://fatbald.seesaa.net/article/447016624.html
	"""
	with open(filepath, 'rb') as f:
		ver = np.frombuffer(f.read(148)[:4], 'i4')
		if ver == 400:
			dtype = [('time', 'u4'), ('open', 'f8'), ('low', 'f8'), ('high', 'f8'), ('close', 'f8'), ('volume', 'f8')]
			df = pd.DataFrame(np.frombuffer(f.read(), dtype=dtype))
			df = df['time open high low close volume'.split()]
		elif ver == 401:
			dtype = [('time', 'u8'), ('open', 'f8'), ('high', 'f8'), ('low', 'f8'), ('close', 'f8'), ('volume', 'i8'),
			         ('s', 'i4'), ('r', 'i8')]
			df = pd.DataFrame(np.frombuffer(f.read(), dtype=dtype).astype(dtype[:-2]))
		df = df.set_index(pd.to_datetime(df['time'], unit='s')).drop('time', axis=1)
		return df


def running_max_min_view(a, window_size, step_size):
	nrows = (a.shape[0] - window_size) // step_size + 1
	ncols = np.prod(a.shape[1:]) * window_size
	return as_strided(a, shape=(nrows, ncols), strides=(step_size * a.strides[0], a.itemsize))


def running_max_min(a, window_size, step_size):
	return running_max_min_view(a, window_size, step_size).ptp(1)


class TradeEnvironment:

	def __init__(self, binary_filepath, image_width=240, image_height=200):
		# グラフ描画のサイズなど初期化
		self.w = image_width # グラフ画像幅(px)
		self.w_max = self.w - 1 # グラフ画像幅(px)-1
		self.h = image_height # グラフ画像高さ(px)
		self.h_max = self.h - 1 # グラフ画像高さ(px)-1
		self.frames = 4 # １状態作成におけるグラフ描画回数、１回描画する度に１つ過去の窓になる、１つ過去に遡ると描画時間範囲が倍になる
		self.img = np.zeros((self.frames * 4, self.h, self.w), np.float32) # グラフ描画先データ、これが状態となる

		# グラフ描画時の窓サイズ計算
		window_size = 30
		framewise_window_sizes = []
		for _ in range(self.frames):
			framewise_window_sizes.append(window_size)
			window_size *= 2
		self.framewise_window_sizes = framewise_window_sizes # 各フレームのグラフ描画窓幅(分)
		self.window_size_max = self.framewise_window_sizes[-1]

		# 全データ読み込み
		self.records = read_records(binary_filepath)
		# 一定期間データが存在しないエリアを探し出し、その間を１エピソードとする
		self.episodes = get_separation_indices(self.records)

		# エピソードとして使える区間があるか調べる
		if self.episodes.shape[0] < 2:
			raise Exception('No area exists for episode in histrical data.')
		valid_episode_exists = False
		for i in range(self.episodes.shape[0] - 1):
			if self.window_size_max * 2 <= self.episodes[i + 1] - self.episodes[i]:
				valid_episode_exists = True
				break
		if not valid_episode_exists:
			raise Exception('No episode area exists lager than window size.')

		# グラフ表示用変数初期化
		self.fig = None
		self.ax_img = None
		self.axs = None

		# １エピソードの対象となるエリアをランダムに選択
		self.episode_records = None
		self.index_in_episode = 0
		self.framewise_records = [None for _ in range(self.frames)]
		self.reset()

		self.num_acions = 4 # 選択可能アクション数
		self.spread = 5 # スプレッド
		self.position_type = 0 # ポジションタイプ、0: なし、1: 買い、2: 売り
		self.position_sec = 0 # ポジション持った際の秒
		self.start_position = 0 # ポジション持った時の pip
		self.loss_cut = 30 # これ以上損したらロスカットされる

	def random_episode(self):
		i = random.randint(0, self.episodes.shape[0] - 2)
		return self.records[self.episodes[i]:self.episodes[i + 1]]

	def update_framewise_records(self):
		fr = self.framewise_records
		fws = self.framewise_window_sizes
		er = self.episode_records
		i = self.index_in_episode
		for f in range(self.frames):
			fr[f] = er[i - fws[f]:i]

	def draw_img(self):
		fr = self.framewise_records
		fws = self.framewise_window_sizes
		img = self.img
		w_max = self.w_max
		h_max = self.h_max

		img[:] = 0

		for f in range(self.frames):
			window_size = fws[f]
			windowed_records = fr[f]

			time = records_to_time(windowed_records)
			values = records_to_values(windowed_records)

			time_max = time.max()
			time_min = time_max - window_size * 60
			values_min = values.min()
			values_max = values.max()

			time_scale = w_max / (time_max - time_min)
			value_scale = -h_max / (values_max - values_min)
			value_translate = h_max - values_min * value_scale

			ch_start = f * 4
			pts = np.empty((windowed_records.shape[0], 1, 2), dtype=np.int32)
			pts[:, 0, 0] = np.rint((time - time_min) * time_scale)
			for value_type in range(4):
				pts[:, 0, 1] = np.rint(values[:, value_type] * value_scale + value_translate)
				cv2.polylines(img[ch_start + value_type], [pts], False, 1.0)

	def reset(self):
		self.index_in_episode = self.window_size_max
		while True:
			self.episode_records = self.random_episode()
			if self.window_size_max <= self.episode_records.shape[0]:
				self.update_framewise_records()
				self.draw_img()
				break

	def step(self, action):
		self.index_in_episode += 1
		self.update_framewise_records()
		self.draw_img()
		return self.img, 0, self.episode_records.shape[0] <= self.index_in_episode, None

	def render(self):
		if self.fig is None:
			self.fig = plt.figure()
			self.ax_img = self.fig.add_subplot(2, 3, 1)
			self.axs = [self.fig.add_subplot(2, 3, 2 + i) for i in range(self.frames)]

		for f in range(self.frames):
			df = records_to_dataframe(self.framewise_records[f])
			self.axs[f].cla()
			df.plot(ax=self.axs[f])

		self.ax_img.cla()
		self.ax_img.imshow(self.img.sum(axis=0))
		plt.pause(0.001)
