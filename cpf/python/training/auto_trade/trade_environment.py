import random
import numpy as np
from numpy.lib.stride_tricks import as_strided
import pandas as pd
import matplotlib.pyplot as plt
import cv2

action_num = 4
overlay_num = 1


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


def values_view_from_records(records):
	return records[['open', 'high', 'low', 'close']].view(('i4', 4))


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
	ncols = int(np.prod(a.shape[1:]) * window_size)
	return as_strided(a, shape=(nrows, ncols), strides=(step_size * a.strides[0], a.itemsize))


def running_max_min(a, window_size, step_size):
	return running_max_min_view(a, window_size, step_size).ptp(1)


def get_state_shape(frames_height_width):
	return (frames_height_width[0] * overlay_num,) + frames_height_width[1:]


class TradeEnvironment:

	def __init__(self, binary_filepath, window_size=30, frames_height_width=(4, 200, 240)):
		# グラフ描画のサイズなど初期化
		self.w = frames_height_width[2] # グラフ画像幅(px)
		self.w_max = self.w - 1 # グラフ画像幅(px)-1
		self.h = frames_height_width[1] # グラフ画像高さ(px)
		self.h_max = self.h - 1 # グラフ画像高さ(px)-1
		self.frames = frames_height_width[0] # １状態作成におけるグラフ描画回数、１回描画する度に１つ過去の窓になる、１つ過去に遡ると描画時間範囲が倍になる
		self.img = np.zeros((self.frames * overlay_num, self.h, self.w), np.float32) # グラフ描画先データ、これが状態となる
		self.state_shape = get_state_shape(frames_height_width) # 状態データの形状

		# グラフ描画時の窓サイズ計算
		window_size = window_size
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

		# その他変数初期化
		self.num_acions = 4 # 選択可能アクション数
		self.spread = 5 # スプレッド
		self.loss_cut = 10000 # これ以上損したらロスカットされる

		# 注文関係
		self.position_type = 0 # ポジションタイプ、0: なし、1: 買い、-1: 売り
		self.position_episode = -1 # ポジション持った時のエピソードインデックス
		self.position_index = -1 # ポジション持った時のエピソード内でのインデックス
		self.position_start_value = 0 # ポジション持った時の pip

		# グラフ表示用変数初期化
		self.fig = None
		self.ax_img = None
		self.axs = None

		# １エピソードの対象となるエリアをランダムに選択
		self.cur_episode = -1 # 現在のエピソードのインデックス
		self.episode_records = None # １エピソード全体分のレコード
		self.index_in_episode = 0 # episode_records 内での現在値に対応するインデックス
		self.framewise_records = [None for _ in range(self.frames)] # フレーム番号が大きいほど過去に向かって増えるレコード

	def update_framewise_records(self):
		er = self.episode_records
		i = self.index_in_episode + 1
		fr = self.framewise_records
		fws = self.framewise_window_sizes
		for f in range(self.frames):
			fr[f] = er[i - fws[f]:i]

	def draw_img(self):
		fr = self.framewise_records
		fws = self.framewise_window_sizes
		img = self.img
		w = self.w
		h = self.h
		chart_x = 5
		chart_w = w - 10 - 1
		h_max = self.h_max

		img[:] = 0
		position_type = self.position_type
		position_start_value = self.position_start_value
		positional_reward = self.calc_positional_reward() if position_start_value else 0

		ind_color = 0.5
		if positional_reward < 0:
			ind_x1 = 0
			ind_x2 = 5
		elif 0 < positional_reward:
			ind_x1 = w - 5
			ind_x2 = w

		for f in range(self.frames):
			window_size = fws[f]
			windowed_records = fr[f]

			time = records_to_time(windowed_records)
			values = values_view_from_records(windowed_records)

			time_max = time.max()
			time_min = time_max - window_size * 60
			values_min = values.min()
			values_max = values.max()
			if position_type:
				values_min = min(values_min, position_start_value)
				values_max = max(values_max, position_start_value)
			values_min -= values_min % 250
			values_max += 250 - values_max % 250

			time_scale = chart_w / (time_max - time_min)
			value_scale = -h_max / (values_max - values_min)
			value_translate = h_max - values_min * value_scale

			cur = int(np.rint(values[-1, 3] * value_scale + value_translate).item())
			if position_type:
				pos = int(np.rint(position_start_value * value_scale + value_translate).item())
			else:
				pos = 0

			ch_start = f * overlay_num
			pts = np.empty((windowed_records.shape[0], 1, 2), dtype=np.int32)
			pts[:, 0, 0] = np.rint(chart_x + (time - time_min) * time_scale)
			for value_type in range(4):
				trg = img[ch_start]

				# チャートを描画
				pts[:, 0, 1] = np.rint(values[:, value_type] * value_scale + value_translate)
				cv2.polylines(trg, [pts], False, 0.5 if value_type != 3 else 1.0)

				# インジケーター描画

				# ポジション持っていたら、ポジった値から現在値まで塗りつぶす
				if position_type and positional_reward:
					ind_y1 = max(min(pos, cur), 0)
					ind_y2 = min(max(pos, cur), h_max) + 1
					trg[ind_y1:ind_y2, ind_x1:ind_x2] = ind_color

				# 現在値に水平線を加算
				if 0 <= cur and cur < h:
					cur_y1 = max(cur - 0, 0)
					cur_y2 = min(cur + 0, h_max) + 1
					# trg[cur_y1:cur_y2, w_chart_max:] = 1.0
					trg[cur_y1:cur_y2, :] = 1.0

	def reset(self, random_episode=True):
		"""エピソードをリセットしエピソードの先頭初期状態に戻る.

		Returns:
			状態.
		"""
		self.settle()

		min_episode_len = self.window_size_max * 2
		while True:

			if random_episode:
				self.cur_episode = random.randint(0, self.episodes.shape[0] - 2)
			else:
				self.cur_episode += 1
				if self.episodes.shape[0] - 1 <= self.cur_episode:
					self.cur_episode = 0

			self.episode_records = self.records[self.episodes[self.cur_episode]:self.episodes[self.cur_episode + 1]]

			if min_episode_len <= self.episode_records.shape[0]:
				self.index_in_episode = self.window_size_max
				self.update_framewise_records()
				self.draw_img()
				return self.img

	def get_value(self):
		"""現在の値を取得する.

		Return:
			現在値.
		"""
		return self.episode_records[self.index_in_episode][4].item()

	def order(self, position_type):
		"""注文する.

		Args:
			position_type: -1 売り、1 買い

		Returns:
			報酬.
		"""
		if self.position_type != 0:
			raise Exception('Can not order when you already have position.')
		self.position_type = position_type
		self.position_episode = self.cur_episode
		self.position_index = self.index_in_episode
		self.position_start_value = self.get_value()

	def calc_positional_reward(self):
		"""現在のポジションと現在値から損益を計算する.
		"""
		return (self.get_value() - self.position_start_value) * self.position_type - self.spread if self.position_type != 0 else 0

	def calc_reward(self):
		"""現在の報酬値を取得.
		"""
		return self.calc_positional_reward(), self.position_type, self.position_episode, self.position_index, self.position_start_value

	def settle(self):
		"""決済する.

		Returns:
			報酬.
		"""
		reward = self.calc_reward()

		if self.position_type == 0:
			return reward

		self.position_type = 0
		self.position_episode = -1
		self.position_index = -1
		self.position_start_value = 0

		return reward

	def step(self, action):
		"""指定のアクションを行い次の状態を得る.

		Args:
			action: 0 何もしない、1 買う、2 売る、3 決済.

		Returns:
			(状態, 報酬, エピソード終了かどうか, その他情報) のタプル.
		"""
		terminal = self.episode_records.shape[0] - 1 <= self.index_in_episode
		if action == 1:
			# 買い
			reward = self.settle()
			self.order(1)
		elif action == 2:
			# 売り
			reward = self.settle()
			self.order(-1)
		elif action == 3 or terminal:
			# 決済
			reward = self.settle()
		elif self.position_type != 0 and self.calc_positional_reward() < -self.loss_cut:
			# ロスカット
			reward = self.settle()
		else:
			# 何もしない
			reward = self.calc_reward()

		# 次の分足が窓に入る様に進める
		if not terminal:
			self.index_in_episode += 1
		self.update_framewise_records()
		self.draw_img()

		return self.img, reward, terminal, None

	def render(self):
		"""現在の状態をグラフ表示する.
		"""
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
