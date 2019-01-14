import random
import numpy as np
import matplotlib.pyplot as plt
import cv2
import trade_environment
from trade_environment import TradeEnvironment

ma_kernel_size = 10
ma_kernel_size_half = ma_kernel_size // 2
ma_kernel = np.ones(ma_kernel_size) / ma_kernel_size

env = TradeEnvironment('test.dat', 240, (1, 100, 110))
env.spread = 5
reward = 0
sum_reward = 0
while True:
	env.reset()
	print(f'episode number: {env.cur_episode}')

	values = trade_environment.values_view_from_records(env.episode_records)
	o = values[:, 0]
	h = values[:, 1]
	l = values[:, 2]
	c = values[:, 3]
	mix = np.mean(values, axis=1)
	ma = np.convolve(mix, ma_kernel, mode='valid')
	ma_dif = np.diff(ma)
	ma_sign = np.sign(ma_dif)
	ma_sign_dif = np.diff(ma_sign)
	ma_sign_indices = np.nonzero(ma_sign_dif)[0]
	ma_sign_values = ma[ma_sign_indices]
	ma_sign_indices += ma_kernel_size_half

	fig = plt.figure()
	ax = fig.add_subplot(1, 1, 1)
	ax.plot(o)
	ax.plot(h)
	ax.plot(l)
	ax.plot(c)
	ax.plot(ma)
	ax.plot(ma_sign_indices, ma_sign_values)

	cur_sign_index = np.where(env.index_in_episode <= ma_sign_indices)[0][:1]
	cur_sign_index = cur_sign_index.item() if cur_sign_index.size else -1

	terminal = False

	step = 0
	ep_len = 0
	ep_reward = 0
	action = 0

	while not terminal:
		if random.random() < 0.9:
			suggested_action = 0 # 基本何もしない

			# if 0 <= cur_sign_index and ma_sign_indices[cur_sign_index] == env.index_in_episode:
			# 	next_sign_index = cur_sign_index + 1
			# 	if next_sign_index <= ma_sign_indices.shape[0] and next_sign_index < ma_sign_values.shape[0]:
			# 		d = ma_sign_values[next_sign_index] - ma_sign_values[cur_sign_index]
			# 		cur_sign_index = next_sign_index
			# 		if env.spread < d:
			# 			suggested_action = 1
			# 		elif d < -env.spread:
			# 			suggested_action = 2
			# 		else:
			# 			suggested_action = 3

			# 次の折り返し地点わかっているなら、折返し場所の値と現在値との差分からアクションを決定する
			i1 = np.where(env.index_in_episode <= ma_sign_indices)[0][:1]
			i1 = i1.item() if i1.size else -1
			if 0 <= i1 and ma_kernel_size_half <= env.index_in_episode:
				cv = ma[env.index_in_episode - ma_kernel_size_half]
				nv1 = ma_sign_values[i1]
				d1 = nv1 - cv

				threshould = int(env.spread * 2)

				if env.position_type == 0:
					# ポジション持っておらず、次の折返し値との差がスプレッドより大きいなら売買する
					if threshould < d1:
						suggested_action = 1
					elif d1 < -threshould:
						suggested_action = 2
				else:
					# 既にポジション持っている際の処理
					if env.index_in_episode == ma_sign_indices[i1]:
						# 目標値に達していたら基本は決済するが可能なら次の売買に備える
						suggested_action = 0 if 0 < env.calc_positional_reward() else 3

						i2 = np.where(ma_sign_indices[i1] < ma_sign_indices)[0][:1]
						i2 = i2.item() if i2.size else -1
						if 0 <= i2:
							d2 = ma_sign_values[i2] - nv1
							if threshould < d2:
								suggested_action = 1
							elif d2 < -threshould:
								suggested_action = 2
					elif np.sign(env.position_type) != np.sign(d1):
						# 理想と異なるポジションなら基本は決済するが可能ならポジションを調整する
						suggested_action = 0 if 0 < env.calc_positional_reward() else 3

						i2 = np.where(ma_sign_indices[i1] < ma_sign_indices)[0][:1]
						i2 = i2.item() if i2.size else -1
						if 0 <= i2:
							d2 = ma_sign_values[i2] - cv
							if threshould < d2:
								suggested_action = 1
							elif d2 < -threshould:
								suggested_action = 2
		else:
			suggested_action = random.randrange(0, 4)

		next_state, reward_info, terminal, info = env.step(suggested_action)
		reward = reward_info[0]

		img = next_state.sum(axis=0)
		img = img * (1 / img.max())
		cv2.imshow('img', img)
		cv2.waitKey(1)

		# if 49 <= ma_kernel and ma_kernel <= 52:
		# 	action = ma_kernel - 49
		# else:
		# 	action = 0
		# if ma_kernel == 27:
		# 	break

		print(f'Step {step} reward {reward} ep_len {ep_len} ep_reward {ep_reward} sum_reward {sum_reward}')
		ep_len += 1
		ep_reward += reward
		sum_reward += reward
		step += 1

	del mix
	del ma
