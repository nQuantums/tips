import random
import numpy as np
import cv2
import trade_environment
from trade_environment import TradeEnvironment

k = np.ones(20) / 20

env = TradeEnvironment('test.dat', 240, (1, 100, 110))
env.spread = 0
reward = 0
sum_reward = 0
while True:
	env.reset()
	print(f'episode number: {env.cur_episode}')

	values = trade_environment.values_view_from_records(env.episode_records)
	mix = np.mean(values, axis=1)
	ma = np.convolve(mix, k, mode='valid')
	ma_dif = np.diff(ma)
	ma_sign = np.sign(ma_dif)
	ma_sign_dif = np.diff(ma_sign)
	ma_sign_indices = np.nonzero(ma_sign_dif)[0]
	ma_sign_values = ma[ma_sign_indices]
	ma_sign_indices += 10

	cur_sign_index = np.where(env.index_in_episode <= ma_sign_indices)[0][:1]
	cur_sign_index = cur_sign_index.item() if cur_sign_index.size else -1

	terminal = False

	step = 0
	ep_reward = 0
	action = 0

	while not terminal:
		suggested_action = 0

		if 0 <= cur_sign_index and ma_sign_indices[cur_sign_index] == env.index_in_episode:
			next_sign_index = cur_sign_index + 1
			if next_sign_index <= ma_sign_indices.shape[0] and next_sign_index < ma_sign_values.shape[0]:
				d = ma_sign_values[next_sign_index] - ma_sign_values[cur_sign_index]
				cur_sign_index = next_sign_index
				if env.spread < d:
					suggested_action = 1
				elif d < env.spread:
					suggested_action = 2
				else:
					suggested_action = 3

		next_state, reward_info, terminal, info = env.step(suggested_action)
		reward = reward_info[0]

		img = next_state.sum(axis=0)
		img *= 1 / img.max()
		cv2.imshow('img', img)
		k = cv2.waitKey(1)
		# if 49 <= k and k <= 52:
		# 	action = k - 49
		# else:
		# 	action = 0
		# if k == 27:
		# 	break

		print(f'Step {step} reward {reward} ep_reward {ep_reward} sum_reward {sum_reward}')
		ep_reward += reward
		sum_reward += reward
		step += 1
