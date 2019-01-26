import typing
import random
import numpy as np
from numpy.lib.stride_tricks import as_strided
import pandas as pd
import matplotlib.pyplot as plt
import cv2

action_num = 4
overlay_num = 1

ma_kernel_sizes = np.array([5, 15, 31, 61], np.int64)
ma_kernel_size_halfs = ma_kernel_sizes // 2
ma_kernels = [np.ones(size) / size for size in ma_kernel_sizes]

chance_ma_kernel_size = 15
chance_ma_kernel_size_halfs = chance_ma_kernel_size // 2
chance_ma_kernel = np.ones(chance_ma_kernel_size) / chance_ma_kernel_size


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


class TradeEnvironment:

	def __init__(self, binary_filepath, window_size=30, height_width=(200, 240)):
		# グラフ描画のサイズなど初期化
		self.w = height_width[1] # グラフ画像幅(px)
		self.w_max = self.w - 1 # グラフ画像幅(px)-1
		self.h = height_width[0] # グラフ画像高さ(px)
		self.h_max = self.h - 1 # グラフ画像高さ(px)-1
		self.img = np.zeros((1, self.h, self.w), np.float32) # グラフ描画先データ、これが状態となる

		# グラフ描画時の窓サイズ計算
		self.window_size = window_size

		# 全データ読み込み
		self.records = read_records(binary_filepath)
		# 一定期間データが存在しないエリアを探し出し、その間を１エピソードとする
		self.episodes = get_separation_indices(self.records)

		# エピソードとして使える区間があるか調べる
		if self.episodes.shape[0] < 2:
			raise Exception('No area exists for episode in histrical data.')
		valid_episode_exists = False
		for i in range(self.episodes.shape[0] - 1):
			if self.window_size * 2 <= self.episodes[i + 1] - self.episodes[i]:
				valid_episode_exists = True
				break
		if not valid_episode_exists:
			raise Exception('No episode area exists lager than window size.')

		# その他変数初期化
		self.num_acions = 4 # 選択可能アクション数
		self.spread = 5 # スプレッド
		self.loss_cut = 100 # これ以上損したらロスカットされる

		# 注文関係
		self.position_type = 0 # ポジションタイプ、0: なし、1: 買い、-1: 売り
		self.position_action = 0 # ポジション決めた際のアクション
		self.position_q_action = 0 # ポジション決めた際のQアクション
		self.position_index_in_episode = -1 # ポジション持った時のエピソード内でのインデックス
		self.position_start_value = 0 # ポジション持った時の pip

		# グラフ表示用変数初期化
		self.fig = None
		self.ax_img = None
		self.axs = None

		# １エピソードの対象となるエリアをランダムに選択
		self.cur_episode = -1 # 現在のエピソードのインデックス
		self.episode_time = None # １エピソード全体分の time 値
		self.episode_values = None # １エピソード全体分の open, high, low, close 値
		self.index_in_episode = 0 # episode_values 内での現在値に対応するインデックス

	def draw_img(self) -> None:
		end = self.index_in_episode + 1 # エピソード内での現在の最新値インデックス+1
		img = self.img # 描画先バッファ
		w = self.w # 画像幅(px)
		h = self.h # 画像高さ(px)
		chart_x = 5 # チャート部のX左端(px)
		chart_y = 0 # チャート部のY上端(px)
		chart_w = w - 10 # チャート部の幅(px)
		chart_h = h # チャート部の高さ(px)
		chart_w_for_scale = chart_w - 1 # チャート部X座標スケーリング用のチャート幅(px)
		chart_h_for_scale = chart_h - 1 # チャート部Y座標スケーリング用のチャート高さ(px)
		chart_right = chart_x + chart_w # チャート右端のX左端(px)+1
		chart_bottom = chart_y + chart_h # チャート下端のY左端(px)+1
		h_max = self.h_max

		img[:] = 0
		position_type = self.position_type
		position_start_value = self.position_start_value
		positional_reward = self.calc_positional_reward() if position_start_value else 0

		if positional_reward < 0:
			ind_x1 = 0
			ind_x2 = 5
		elif 0 < positional_reward:
			ind_x1 = w - 5
			ind_x2 = w

		window_size = self.window_size
		time = self.episode_time[end - window_size:end]
		values = self.episode_values[end - window_size:end]
		ma = []

		# 可能なら移動平均を計算
		for ki in range(len(ma_kernel_sizes)):
			size_needed = window_size + ma_kernel_size_halfs[ki] * 2
			if size_needed <= end:
				start = end - size_needed
				ma.append(np.convolve(self.episode_values[start:end, 1], ma_kernels[ki], mode='valid'))
				ma.append(np.convolve(self.episode_values[start:end, 2], ma_kernels[ki], mode='valid'))
				ma.append(np.convolve(self.episode_values[start:end, 3], ma_kernels[ki], mode='valid'))

		# 表示範囲となる最大最小を探す
		time_max = time.max()
		time_min = time_max - window_size * 60
		values_min = values.min()
		values_max = values.max()

		for y in ma:
			values_min = min(values_min, y.min())
			values_max = max(values_max, y.max())

		if position_type:
			values_min = min(values_min, position_start_value)
			values_max = max(values_max, position_start_value)

		# values_min -= values_min % 100
		# values_max += 100 - values_max % 100

		time_scale = chart_w_for_scale / (time_max - time_min)
		value_scale = -chart_h_for_scale / (values_max - values_min)
		value_translate = chart_h_for_scale - values_min * value_scale

		cur = int(np.rint(values[-1, 3] * value_scale + value_translate).item())
		if position_type:
			pos = int(np.rint(position_start_value * value_scale + value_translate).item())
		else:
			pos = 0

		trg = img[0]
		chart_trg = trg[chart_y:chart_bottom, chart_x:chart_right]

		# インジケーター描画

		# 目盛り描画
		for y in np.rint(
		    np.arange(values_min - values_min % 50, values_max + 51 - (values_max % 50), 50) * value_scale +
		    value_translate).astype(np.int32):
			if 0 <= y and y < chart_trg.shape[0]:
				chart_trg[y, :] = 0.1

		# ポジション持っていたら、ポジった値から現在値まで塗りつぶす
		if position_type and positional_reward:
			ind_y1 = max(min(pos, cur), 0)
			ind_y2 = min(max(pos, cur), h_max) + 1
			trg[ind_y1:ind_y2, ind_x1:ind_x2] = 1

		# 現在値として水平線を描画
		if 0 <= cur and cur < h:
			cur_y1 = max(cur - 1, 0)
			cur_y2 = min(cur + 1, h_max) + 1
			trg[cur_y1:cur_y2, :] = 1.0

		# チャートを描画開始
		pts = np.empty((values.shape[0], 1, 2), dtype=np.int32)
		pts[:, 0, 0] = np.rint((time - time_min) * time_scale)

		# 可能なら移動平均線を描画
		for y in ma:
			pts[:, 0, 1] = np.rint(y * value_scale + value_translate)
			cv2.polylines(chart_trg, [pts], False, 0.3)

		# open, high, low, close を描画
		for value_type in range(4):
			pts[:, 0, 1] = np.rint(values[:, value_type] * value_scale + value_translate)
			cv2.polylines(chart_trg, [pts], False, 0.7)

	def reset(self, random_episode_or_index=True) -> np.ndarray:
		"""エピソードをリセットしエピソードの先頭初期状態に戻る.

		Returns:
			状態.
		"""
		self.settle()

		min_episode_len = self.window_size * 2
		eps = self.episodes
		while True:

			if isinstance(random_episode_or_index, bool):
				if random_episode_or_index:
					self.cur_episode = random.randint(0, len(eps) - 2)
				else:
					self.cur_episode += 1
					if len(eps) - 1 <= self.cur_episode:
						self.cur_episode = 0
			elif isinstance(random_episode_or_index, int):
				if random_episode_or_index < 0:
					random_episode_or_index = 0
				elif len(eps) <= random_episode_or_index:
					random_episode_or_index = len(eps) - 1
				self.cur_episode = random_episode_or_index

			i1 = eps[self.cur_episode]
			i2 = eps[self.cur_episode + 1]

			if min_episode_len <= i2 - i1:
				rcds = self.records[i1:i2]
				self.episode_time = records_to_time(rcds)
				self.episode_values = values_view_from_records(rcds)
				self.index_in_episode = self.window_size
				self.draw_img()
				return self.img

	def get_value(self) -> float:
		"""現在の値を取得する.

		Return:
			現在値.
		"""
		return self.episode_values[self.index_in_episode, 3].item()

	def order(self, position_type: int, action: int, q_action: int) -> None:
		"""注文する.

		Args:
			position_type: -1 売り、1 買い.
			action: ポジション決めた際のアクション.
			q_action: ポジション決めた際のQアクション.

		Returns:
			報酬.
		"""
		if self.position_type != 0:
			raise Exception('Can not order when you already have position.')
		self.position_type = position_type
		self.position_action = action
		self.position_q_action = q_action
		self.position_index_in_episode = self.index_in_episode
		self.position_start_value = self.get_value() + position_type * self.spread

	def calc_positional_reward(self) -> float:
		"""現在のポジションと現在値から損益を計算する.
		"""
		return (self.get_value() - self.position_start_value) * self.position_type if self.position_type != 0 else 0

	def calc_reward(self, settle: bool = True) -> typing.Tuple[float, int, int, int]:
		"""現在の報酬値を取得.

		Return:
			(報酬, ポジションを決めた際のアクション, ポジションを決めた際のQアクション,  ポジションを決めた際のエピソード内でのインデックス) のタプル.
		"""
		return (self.calc_positional_reward()
		        if settle else 0), self.position_action, self.position_q_action, self.position_index_in_episode

	def settle(self) -> typing.Tuple[float, int, int, int]:
		"""決済する.

		Returns:
			(報酬, ポジションを決めた際のアクション, ポジションを決めた際のQアクション,  ポジションを決めた際のエピソード内でのインデックス) のタプル.
		"""
		reward = self.calc_reward()

		if self.position_type == 0:
			return reward

		self.position_type = 0
		self.position_action = 0
		self.position_q_action = 0
		self.position_index_in_episode = -1
		self.position_start_value = 0

		return reward

	def is_action_ignored(self, action: int) -> bool:
		"""step メソッドにアクションを指定しても無視されるかどうか調べる."""
		return action == 1 and 0 < self.position_type or action == 2 and self.position_type < 0 or action == 3 and self.position_type == 0

	def step(self, action: int,
	         q_actino: int) -> typing.Tuple[np.ndarray, typing.Tuple[float, int, int, int], bool, object]:
		"""指定のアクションを行い次の状態を得る.

		Args:
			action: アクション、0 何もしない、1 買う、2 売る、3 決済.
			action: Qアクション、ポジションと共に情報記録するために使用.

		Returns:
			(状態, 報酬, エピソード終了かどうか, その他情報) のタプル.
		"""
		terminal = self.episode_values.shape[0] - 1 <= self.index_in_episode

		buy = action == 1 and self.position_type != 1
		sell = action == 2 and self.position_type != -1
		exit = (action == 3 or terminal) and self.position_type != 0
		losscut = self.position_type != 0 and self.calc_positional_reward() < -self.loss_cut
		reward = self.settle() if buy or sell or exit or losscut else self.calc_reward(False)

		if buy:
			# 買い
			self.order(1, action, q_actino)
		elif sell:
			# 売り
			self.order(-1, action, q_actino)

		# 次の分足が窓に入る様に進める
		if not terminal:
			self.index_in_episode += 1
		self.draw_img()

		return self.img, reward, terminal, None

	# def render(self):
	# 	"""現在の状態をグラフ表示する.
	# 	"""
	# 	if self.fig is None:
	# 		self.fig = plt.figure()
	# 		self.ax_img = self.fig.add_subplot(2, 3, 1)
	# 		self.ax = self.fig.add_subplot(2, 3, 2 + i)

	# 	df = records_to_dataframe(self.episode_values.framewise_records[f])
	# 	self.axs[f].cla()
	# 	df.plot(ax=self.axs[f])

	# 	self.ax_img.cla()
	# 	self.ax_img.imshow(self.img.sum(axis=0))
	# 	plt.pause(0.001)
