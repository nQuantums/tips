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


csv_filepath = 'C:/work/trade/USDJPY_M1.csv'
binary_filepath = 'test.dat'
hst_filepath = 'C:/work/trade/USDJPY.hst'

# df = tickdata(hst_filepath)
csv_to_binary(csv_filepath, binary_filepath)
records = read_records(binary_filepath)
# df = load_from_csv(csv_filepath)
# df = df.set_index('time')
# df = df.drop('time')

# 一定期時間以上間が空いている部分は学習に含まれないようにする
time_areas = get_separation_indices(records)

w = 240
w_max = w - 1
h = 200
h_max = h - 1
history = 4
img = np.zeros((history * 4, h, w), np.float32)
window_size = 30
window_sizes = []
for _ in range(history):
	window_sizes.append(window_size)
	window_size *= 2

fig = plt.figure()
ax_img = fig.add_subplot(2, 3, 1)
axs = [fig.add_subplot(2, 3, 2 + i)  for i in range(history)]

for i in range(len(time_areas) - 1):
	# 約１週間分抽出
	week_records = records[time_areas[i]:time_areas[i + 1]]

	# 240分程度のウィンドウをずらしながら表示
	for j in range(window_sizes[-1], week_records.shape[0], 1):
		img[:] = 0

		for hindex in range(history):
			window_size = window_sizes[hindex]
			windowed_records = week_records[j - window_size:j]

			time = records_to_time(windowed_records)
			values = records_to_values(windowed_records)

			time_min = time.min()
			time_max = time.max()
			values_max = values.max()
			values_min = values.min()

			time_scale = w_max / (time_max - time_min)
			value_scale = -h_max / (values_max - values_min)
			value_translate = h_max - values_min * value_scale

			ch_start = hindex * 4
			pts = np.empty((windowed_records.shape[0], 1, 2), dtype=np.int32)
			pts[:, 0, 0] = np.rint((time - time_min) * time_scale)
			for value_type in range(4):
				pts[:, 0, 1] = np.rint(values[:, value_type] * value_scale + value_translate)
				cv2.polylines(img[ch_start + value_type], [pts], False, 1.0)

			# df = records_to_dataframe(windowed_records)
			# axs[hindex].cla()
			# df.plot(ax=axs[hindex])
	
		ax_img.cla()
		ax_img.imshow(img.sum(axis=0))
		plt.pause(0.1)

	# cv2.waitKey(0)

	# over_indices = np.nonzero((ptp > 5000).astype('i4'))[0]

	# if len(over_indices) != 0:
	# 	fig = plt.figure()
	# 	x = np.arange(view.shape[0])
	# 	ax = fig.add_subplot(1,1,1)
	# 	ax.plot(x, values_max)
	# 	ax.plot(x, values_min)

	# 	df = records_to_dataframe(r)
	# 	df = df.drop('volume', axis=1)
	# 	df.plot()
	# 	plt.show()

# time = b[['time']].astype('u8')
# dif_time = np.diff(time)
# interval = 30 * 60 * 1000000000
# time_areas = np.nonzero((dif_time > interval).astype('i4'))
# values = b[['open', 'high', 'low', 'close']].view(('i4', 4))
# v = values[:1000]
# print(np.max(v))
# print(np.min(v))
# # print(time)

# # print(a)
# # print(b)
# print(b[:, ['time']])
# # print(a.tobytes())

# # dtype = [('time', 'str'), ('open', 'f4'), ('high', 'f4'), ('low', 'f4'), ('close', 'f4'), ('volume', 'i4')]
# # df = pd.read_csv('C:/work/trade/USDJPY_M1.csv', parse_dates=[0], dtype=dtype)
# # print(df.dtypes)
# # print(df)
