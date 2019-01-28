import random
import numpy as np
from numpy.lib.stride_tricks import as_strided
import matplotlib.pyplot as plt
import cv2
import trade_environment
from trade_environment import TradeEnvironment

env = TradeEnvironment('test.dat', 240, (1, 100, 110))
env.reset(False)

values = env.episode_values
maxs = values.max(axis=1)
mins = values.min(axis=1)
ptps = maxs - mins

fig = plt.figure()
ax = fig.add_subplot(1, 1, 1)


def make_kernel(ksize):
	return np.ones(ksize) / ksize, ksize, ksize // 2


def convolve(values, kernel):
	return np.convolve(values, kernel[0], mode='valid'), kernel[2]


def detect_feature_indices(values):
	return np.nonzero(np.diff(np.sign(np.diff(values))))[0] + 2


def remove_small_gap(values, gap_size):
	return np.nonzero(gap_size <= np.abs(np.diff(values)))[0] + 1


def signs_calc(values, kernel):
	kernel_size_half = kernel.shape[0] // 2
	ma = np.convolve(values, kernel, mode='valid')
	indices1 = detect_feature_indices(ma)
	values1 = ma[indices1]
	return ma, indices1 + kernel_size_half, values1, kernel_size_half
	# indices2 = detect_feature_indices(values1)
	# indices = indices1[indices2]
	# values = ma[indices]
	# return ma, indices + kernel_size_half, values, kernel_size_half


def signs_x_indices(signs):
	return np.arange(signs[0].shape[0]) + signs[3]


def detect_turning_points(values, gap):
	"""指定数列の折返しポイントの地点を検出する.

	Args:
		values: 数列.
		gap: 折返し判定閾値、この値を超えて反転したら折返しと判断する.

	Returns:
		(折返しインデックス, 検出途中に生成した一定値以上距離を保って付いてくる値の数列) のタプル.
	"""
	indices = []
	stalkers = np.empty((len(values),), dtype=np.int32)
	last_value = values[0]
	stalker = last_value
	stalkers[0] = stalker
	for i in range(1, len(values)):
		v = values[i]
		d = v - stalker
		if last_value < stalker and stalker <= v or stalker < last_value and v <= stalker:
			indices.append(i)
		if d < -gap:
			stalker = v + gap
		elif gap < d:
			stalker = v - gap
		stalkers[i] = stalker
		last_value = v
	return np.array(indices, dtype=np.int32) - 1, stalkers


def detect_order_indices(values, max_los, search_length):
	indices = []

	index = 0
	index_end = len(values)
	start_value = values[index]
	indices.append(index)

	while True:
		index += 1
		if index_end <= index:
			break
		next_values = values[index:min(index + search_length, index_end)]
		deltas = next_values - start_value
		deltas_abs = np.abs(deltas)

		end_index = search_length
		over_los_indices = np.nonzero(max_los <= deltas_abs)[0]
		if len(over_los_indices):
			over_los_signs_dif = np.diff(np.sign(deltas[over_los_indices]))
			if len(over_los_signs_dif):
				turn_indices = np.nonzero(over_los_signs_dif)[0]
				if len(turn_indices):
					end_index = turn_indices[0].item() + 1

		index += np.argsort(deltas_abs[:end_index])[-1].item()
		start_value = values[index]
		indices.append(index)

	return indices


kernel = make_kernel(10)

o = values[:, 0]
h = values[:, 1]
l = values[:, 2]
c = values[:, 3]
x = np.arange(c.shape[0])

c_ma = convolve(c, kernel)
x_ma = np.arange(c_ma[0].shape[0]) + c_ma[1]

ax.plot(x, c, label='close')
ax.plot(x_ma, c_ma[0], label='close ma')

# cgap_indices = remove_small_gap(c, 10)
# cgap_values = c[cgap_indices]
# ax.plot(x[cgap_indices], cgap_values)

od_indices = detect_order_indices(c, 10, 30)
od_values = c[od_indices]
ax.plot(x[od_indices], od_values, label='order points', marker='o')

tp_indices, stalkers = detect_turning_points(c, 5)
tp_values = c[tp_indices]
# ax.plot(x, stalkers, label='stalker')
ax.plot(x[tp_indices], tp_values, label='turning point', marker='o')

# ksize = 30
# ksize_half = ksize // 2
# kernel = np.ones(ksize) / ksize
# ma = np.convolve(c, kernel, mode='valid')
# ma_x = np.arange(ma.shape[0]) + ksize_half
# ax.plot(ma_x, ma)

# feature_indices = detect_feature_indices(ma)
# feature_x = ma_x[feature_indices]
# feature_values = ma[feature_indices]
# ax.plot(feature_x, feature_values)

# gapremoved_indices = remove_small_gap(feature_values, 10)
# gapremoved_x = feature_x[gapremoved_indices]
# gapremoved_values = feature_values[gapremoved_indices]
# ax.plot(gapremoved_x, gapremoved_values)

# ksizes = [30]
# ma_high = [signs_calc(h, np.ones(ksize) / ksize) for ksize in ksizes]
# ma_low = [signs_calc(l, np.ones(ksize) / ksize) for ksize in ksizes]
# ma_closes = [signs_calc(c, np.ones(ksize) / ksize) for ksize in ksizes]

# # ax.plot(h)
# xbase = np.arange(c.shape[0])
# for sign in ma_high:
# 	x = signs_x_indices(sign)
# 	ax.plot(x, sign[0])

# 	idx = np.nonzero(5 < np.abs(np.diff(sign[2])))
# 	ax.plot(x, sign[0])

# 	ax.plot(xbase[sign[1]], sign[2])

# x = np.arange(c.shape[0])
# ma_x = signs_x_indices(ma_high)
# ax.plot(x[ma_high[1]], ma_high[2])
# ax.plot(ma_x, ma_high[0])
# ax.plot(ma_x, ma_low[0])

# maxwins = trade_environment.running_max_min_view(maxs + ptps, 10, 1).max(axis=1)
# minwins = trade_environment.running_max_min_view(mins - ptps, 10, 1).min(axis=1)
# # maxwins = trade_environment.running_max_min_view(maxwins, 10, 1).min(axis=1)
# # minwins = trade_environment.running_max_min_view(minwins, 10, 1).max(axis=1)
# # x = np.arange(15, 15 + maxwins.shape[0])
# x = np.arange(maxwins.shape[0])
# ax.plot(x, maxwins)
# ax.plot(x, minwins)

# n = maxwins.shape[0]
# maxs = maxs[:n]
# mins = mins[:n]
# c = values[:, 3][:n]
# down_indices = np.nonzero((c < minwins).astype('i4'))[0]
# x = np.arange(c.shape[0])
# # ax.plot(x, maxs)
# # ax.plot(x, mins)
# ax.plot(x, c)

# x = x[down_indices]
# c = c[down_indices]
# ax.plot(x, c)

plt.legend()
plt.show()
