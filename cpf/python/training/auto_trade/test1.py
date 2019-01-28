import random
import numpy as np
import matplotlib.pyplot as plt
import cv2
import trade_environment
from trade_environment import TradeEnvironment
import action_suggester

frame_num = 5

env = TradeEnvironment('test.dat', 30, (100, 110))
env.spread = 5
sum_reward = 0.0

suggester = action_suggester.TpActionSuggester(env)
rew_adjuster = action_suggester.TpRewardAdjuster(suggester, loss_cut_check=True, securing_profit_check=True)

plt.style.use('seaborn-whitegrid')


def make_state(state, frame):
	return np.concatenate((frame, (state if state.shape[0] < frame_num else state[:state.shape[0] - 1])), axis=0)


while True:
	print(f'episode number: {env.cur_episode}')

	# 初期状態の取得
	state = env.reset(False)
	suggester.start_episode()
	for _ in range(frame_num - 1):
		frame, reward_info, terminal, info = env.step(0)
		if terminal:
			break
		state = make_state(state, frame)

	terminal = False

	step = 0
	ep_len = 0
	ep_reward = 0.0
	action = 0

	rewards = np.zeros((len(env.episode_values),), dtype=np.float64)
	reward_adjs = np.zeros((len(env.episode_values),), dtype=np.float64)
	buy_indices = []
	sell_indices = []
	exit_indices = []
	last_action = 0

	while not terminal:
		# if random.random() < 0.5:
		# 	action = suggester.get_suggested_action()
		# else:
		# 	action = random.randrange(0, 4)
		# action = suggester.get_suggested_action()
		# action = random.randrange(0, 4)
		action = 2
		if env.is_action_ignored(action):
			action = 0

		reward_adj = rew_adjuster.adjust_reward(action)
		reward_adjs[env.index_in_episode] = reward_adj

		if action == 1:
			buy_indices.append(env.index_in_episode)
		elif action == 2:
			sell_indices.append(env.index_in_episode)
		elif action == 3:
			exit_indices.append(env.index_in_episode)

		# アクションを指定して次の状態を得る
		frame, reward_info, terminal, info = env.step(action)
		next_state = make_state(state, frame)
		reward_org = reward_info[0]
		reward = reward_info[0] + reward_adj
		rewards[env.index_in_episode - 1] = reward

		# img = next_state.sum(axis=0)
		# img = img * (1 / img.max())
		# cv2.imshow('img', img)
		# cv2.waitKey(1)

		# if 49 <= ma_kernel and ma_kernel <= 52:
		# 	action = ma_kernel - 49
		# else:
		# 	action = 0
		# if ma_kernel == 27:
		# 	break

		# print(f'Step {step} rew {reward} rew_adj {reward_adj} ep_len {ep_len} ep_rew {ep_reward} sum_rew {sum_reward}')

		# 次のループに備える
		state = next_state
		ep_reward += reward_org
		ep_len += 1
		sum_reward += reward_org
		step += 1

	values = env.episode_values
	o = values[:, 0]
	h = values[:, 1]
	l = values[:, 2]
	c = values[:, 3]

	reward_indices = np.nonzero(rewards)[0]
	c_reward = c[reward_indices] + rewards[reward_indices]

	adj_rew_indices = np.nonzero(reward_adjs)[0]
	c_rewadj = c[adj_rew_indices] + reward_adjs[adj_rew_indices]

	fig = plt.figure()
	ax = fig.add_subplot(1, 1, 1)
	x = np.arange(len(c))
	# ax.plot(x, o, label='open')
	# ax.plot(x, h, label='high')
	# ax.plot(x, l, label='low')
	ax.plot(x[reward_indices], c_reward, label='reward', marker='x')
	ax.plot(x[suggester.tp_indices], c[suggester.tp_indices], label='tp')
	ax.plot(x, c, label='close')
	ax.plot(x[adj_rew_indices], c_rewadj, label='rewadj', linestyle='None', marker='+')
	ax.plot(x[buy_indices], c[buy_indices], label='buy', linestyle='None', marker='^')
	ax.plot(x[sell_indices], c[sell_indices], label='sell', linestyle='None', marker='v')
	ax.plot(x[exit_indices], c[exit_indices], label='exit', linestyle='None', marker='o')

	plt.legend()
	plt.show()
