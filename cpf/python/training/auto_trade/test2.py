import random
import numpy as np
from numpy.lib.stride_tricks import as_strided
import matplotlib.pyplot as plt
import cv2
import trade_environment
from trade_environment import TradeEnvironment

env = TradeEnvironment('test.dat', 240, (1, 100, 110))
env.reset()

values = trade_environment.values_view_from_records(env.episode_records)
maxs = values.max(axis=1)
mins = values.min(axis=1)
ptps = maxs - mins

fig = plt.figure()
ax = fig.add_subplot(1, 1, 1)

k_size = 10
k_size_helf = k_size // 2
k = np.ones(k_size) / k_size
o = values[:, 0]
h = values[:, 1]
l = values[:, 2]
c = values[:, 3]
mix = np.mean(values, axis=1)
ma = np.convolve(mix, k, mode='valid')
ma_dif = np.diff(ma)
ma_sign = np.sign(ma_dif)
ma_sign_dif = np.diff(ma_sign)
ma_sign_indices = np.nonzero(ma_sign_dif)[0]
ma_sign_values = ma[ma_sign_indices]
ma_sign_indices += k_size_helf

down = np.nonzero((ma_dif < 0).astype('i4'))[0]
up = np.nonzero((0 < ma_dif).astype('i4'))[0]

x = np.arange(c.shape[0])
ax.plot(o)
ax.plot(h)
ax.plot(l)
ax.plot(c)
ax.plot(ma)
ax.plot(ma_sign_indices, ma_sign_values)

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

plt.show()
