import os
import time
import datetime
import json
import numpy as np
import cv2
import torch
import matplotlib.pyplot as plt

import db_initializer
import trade_environment
from trade_environment import TradeEnvironment
import model
import action_suggester

model.plot_enabled = False


def make_state(state, frame):
	return np.concatenate((frame, (state if state.shape[0] < frame_num else state[:state.shape[0] - 1])), axis=0)

with open('parameters.json', 'r') as f:
	params = json.load(f)

ep = params['env']
ap = params['actor']
lp = params['learner']

window_size = ep['window_size']
state_shape = tuple(ep['frames_height_width'])
frame_num = state_shape[0]
action_dim = trade_environment.action_num
model_formula = f'model.{lp["model"]}(state_shape, action_dim, "TestMode", hidden_size={lp["hidden_size"]})'

env = TradeEnvironment('test.dat', window_size, state_shape[1:])

Q = eval(model_formula)

model_file_name = db_initializer.get_state_dict_name(params)
if model_file_name and os.path.isfile(model_file_name):
	print(f'Loading {model_file_name}')
	saved_state = torch.load(model_file_name)
	Q.load_state_dict(saved_state['module'])

ep_len = 0
ep_reward = 0.0
terminal = False

# 初期状態の取得
state = env.reset()
for _ in range(frame_num - 1):
	frame, reward_info, terminal, _ = env.step(0)
	if terminal:
		break
	state = make_state(state, frame)

while not terminal:
	# 状態を基に取るべき行動を選択する
	with torch.no_grad():
		model.show_plot = True
		q = Q(torch.tensor(state.reshape((1,) + state.shape))).squeeze()
		model.plt_pause(0.001)
		model.show_plot = False
	action = torch.argmax(q, 0).item()

	# 環境に行動を適用し次の状態を取得する
	frame, reward_info, terminal, _ = env.step(action)
	next_state = make_state(state, frame)
	reward = reward_info[0]
	img = next_state.sum(axis=0)
	img *= 1.0 / img.max()
	cv2.imshow('TestMode', img)
	cv2.waitKey(1)

	# 次のループに備える
	state = next_state
	ep_reward += reward
	ep_len += 1

	print(f'ep_reward: {ep_reward}')
