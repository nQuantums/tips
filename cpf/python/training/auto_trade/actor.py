#!/usr/bin/env python
import os
import time
import datetime
import json
import multiprocessing as mp
import random
import numpy as np
from collections import deque
from collections import namedtuple
import cv2
import torch
import psycopg2
import matplotlib.pyplot as plt

import db_initializer
import tables
import trade_environment
from trade_environment import TradeEnvironment
import model
import action_suggester


class Actor:

	def __init__(self, params, param_set_id, actor_id, status_dict, shared_state, remote_mem):
		self.params = params
		self.param_set_id = param_set_id
		self.actor_id = actor_id
		self.status_dict = status_dict
		self.shared_state = shared_state
		self.remote_mem = remote_mem

		ep = params['env']
		ap = params['actor']
		lp = params['learner']
		model_formula = f'model.{lp["model"]}(self.state_shape, self.action_dim, "Actor# {actor_id}", hidden_size={lp["hidden_size"]})'
		model_formula_target = f'model.{lp["model"]}(self.state_shape, self.action_dim, hidden_size={lp["hidden_size"]})'

		self.window_size = ep['window_size']
		self.state_shape = tuple(ep['frames_height_width'])
		self.frame_num = self.state_shape[0]
		self.action_dim = trade_environment.action_num
		self.num_steps = ap["num_steps"] # Nステップ数
		self.n_step_transition_batch_size = ap['n_step_transition_batch_size']
		self.env = TradeEnvironment('test.dat', self.window_size, self.state_shape[1:])
		self.epsilon = ap['epsilon']**(1 + ap['alpha'] * self.actor_id / (ap['num_actors'] - 1))
		self.last_Q_state_dict_id = 0

		self.Q = eval(model_formula)
		self.Q_target = eval(model_formula_target) # Target Q network which is slow moving replica of self.Q
		Q_state_dict = self.shared_state["Q_state_dict"]
		self.Q.load_state_dict(Q_state_dict[0])
		self.Q_target.load_state_dict(Q_state_dict[1])
		self.sum_los = 0

	def make_state(self, state, frame):
		return np.concatenate((frame, (state if state.shape[0] < self.frame_num else state[:state.shape[0] - 1])), axis=0)

	def run(self):
		torch.set_num_threads(1)

		ap = self.params['actor']
		lp = self.params['learner']

		conn = psycopg2.connect(self.params["db"]["connection_string"])
		conn.autocommit = True

		status_dict = self.status_dict

		t = tables.ActorData()
		record_type = t.get_record_type()
		record_insert = t.get_insert()
		param_set_id = self.param_set_id
		actor_id = self.actor_id
		now = datetime.datetime.now
		ep_len = 0
		ep_reward = 0.0
		sum_reward = 0.0

		transitions = deque()
		n_step_transitions = []
		n_step_transition_batch_size = self.n_step_transition_batch_size
		num_steps = self.num_steps
		gamma = ap['gamma']
		gamma_n = gamma**num_steps

		Q = self.Q
		Q_target = self.Q_target
		take_offsets = torch.arange(n_step_transition_batch_size) * self.action_dim

		wait_shared_memory_clear = ap['wait_shared_memory_clear']

		self.env.spread = ap['spread'] # 最初はスプレッド０でやらないとポジってくれなくてまともに学習できないので注意

		# Actorの最後のステータスを読み込む
		actor_state_file = f'{db_initializer.get_state_dict_name(self.params)}.{self.actor_id}.json'
		actor_state = {}
		if os.path.isfile(actor_state_file):
			with open(actor_state_file, 'r') as f:
				actor_state = json.load(f)
				sum_reward = actor_state['sum_reward']

		# お勧めアクション生成、報酬調整オブジェクトを取得する
		suggester = eval(f'action_suggester.{ap["action_suggester"].format("self.env")}')
		reward_adjuster = eval(f'action_suggester.{ap["reward_adjuster"].format("suggester")}')

		def plc_random(q):
			"""計算されたアクションまたは乱数を取得する.
			"""
			q_action = torch.argmax(q, 0).item()
			if random.random() < self.epsilon:
				return random.randrange(0, len(q)), q_action
			else:
				return q_action, q_action

		def plc_suggested(q):
			"""計算されたアクションまたはお勧めアクションを取得する.
			"""
			q_action = torch.argmax(q, 0).item()
			if random.random() < self.epsilon:
				return suggester.get_suggested_action(), q_action
			else:
				return q_action, q_action

		# 方策を取得する
		policy = eval(ap['policy'])

		while not status_dict['request_quit']:
			ep_len = 0
			ep_reward = 0.0
			terminal = False

			# 初期状態の取得
			state = self.env.reset()
			suggester.start_episode()
			for _ in range(self.frame_num - 1):
				frame, reward_info, terminal, _ = self.env.step(0)
				if terminal:
					break
				state = self.make_state(state, frame)

			while not terminal:
				# 状態とポリシーを基に取るべき行動を選択する
				with torch.no_grad():
					if self.actor_id == 7:
						model.show_plot = True
					q = Q(torch.tensor(state.reshape((1,) + state.shape))).squeeze()
					if self.actor_id == 7:
						model.plt_pause(0.001)
						model.show_plot = False
				action = policy(q)

				# 指定アクションから報酬調整量を取得する
				reward_adj = reward_adjuster.adjust_reward(action[0])

				# 環境に行動を適用し次の状態を取得する
				frame, reward_info, terminal, _ = self.env.step(action[0])
				next_state = self.make_state(state, frame)
				reward_org = reward_info[0]
				reward = reward_org + reward_adj
				img = next_state.sum(axis=0)
				img *= 1.0 / img.max()
				cv2.imshow(f'Actor# {self.actor_id}', img)
				# self.env.render()

				# N-StepTransition のために状態遷移情報を追加する
				transitions.append((state, action[0], reward, next_state, terminal))

				# N-StepTransition の処理
				len_transitions = len(transitions)
				if num_steps <= len_transitions or terminal:
					# N-StepTransition を生成して追加
					first = transitions[0]
					latest = transitions[len_transitions - 1]
					r = first[2]
					g = gamma
					for i in range(1, len_transitions):
						r += transitions[i][2] * g
						g *= gamma
					transitions.popleft()
					n_step_transitions.append((first[0], first[1], r, latest[1], latest[3], latest[4]))

					# N-StepTransition がある程度溜まったら優先度となるTD誤差を計算してリモートメモリに追加
					if n_step_transition_batch_size <= len(n_step_transitions):
						# サンプリングの優先度となるTD誤差を計算する
						s = torch.tensor([t[0] for t in n_step_transitions], dtype=torch.float32)
						a = torch.tensor([t[1] for t in n_step_transitions], dtype=torch.int64)
						r = torch.tensor([t[2] for t in n_step_transitions], dtype=torch.float32)
						a_latest = torch.tensor([t[3] for t in n_step_transitions], dtype=torch.int64)
						s_latest = torch.tensor([t[4] for t in n_step_transitions], dtype=torch.float32)
						term = torch.tensor([t[5] for t in n_step_transitions], dtype=torch.float32)
						n_step_transitions.clear()

						with torch.no_grad():
							Q.eval()
							Q_target.eval()
							Gt = r + (1.0 - term) * gamma_n * Q_target(s_latest).take(take_offsets + a_latest).squeeze()
							priorities = (Gt - Q(s).take(take_offsets + a).squeeze()).abs()
							del Gt

						# Learner 側が処理するのを待ってからリモートメモリに追加
						# ※torch.tensor のまま送るとLearner側の都合で問題があるので numpy にしている
						if wait_shared_memory_clear:
							while n_step_transition_batch_size <= self.remote_mem.qsize():
								time.sleep(0.001)
						s = s.numpy()
						a = a.numpy().astype(np.int8)
						r = r.numpy()
						a_latest = a_latest.numpy().astype(np.int8)
						s_latest = s_latest.numpy()
						term = term.numpy().astype(np.int8)
						batch = [v for v in zip(s, a, r, a_latest, s_latest, term)]
						self.remote_mem.put((priorities, batch))

				# Learner からの共有パラメータが更新されていたらロードする
				id = status_dict['Q_state_dict_id']
				if 1 <= id - self.last_Q_state_dict_id:
					print(f'Actor#: {self.actor_id} state loaded.')
					Q_state_dict = self.shared_state["Q_state_dict"]
					self.Q.load_state_dict(Q_state_dict[0])
					self.Q_target.load_state_dict(Q_state_dict[1])
					self.last_Q_state_dict_id = id

				# 終了要求があったらループを抜ける
				if cv2.waitKey(1) == 27 or status_dict['request_quit']:
					status_dict['request_quit'] = True
					break

				# 次のループに備える
				state = next_state
				ep_reward += reward_org
				ep_len += 1
				sum_reward += reward_org

				# エピソード終了かまたは報酬が入った際にDBへデータ登録
				if terminal or reward_org:
					if action[0] == action[1]:
						print(
						    f'Actor#: {self.actor_id} t: {t} rew: {reward_org} {reward} act: {action[0]} ep_len: {ep_len} ep_rew: {ep_reward} sum_rew: {sum_reward}'
						)
					train_num = status_dict['train_num']
					record = record_type(param_set_id, terminal, actor_id, now(), train_num, self.env.spread, action[0],
					                     action[1], reward_info[0], reward_info[1], reward_info[2], reward_info[3],
					                     reward_info[4], ep_len, ep_reward, sum_reward)
					with conn.cursor() as cur:
						record_insert(cur, record)

		# Actor の現在のステータスを保存しておく
		actor_state['sum_reward'] = sum_reward
		with open(actor_state_file, 'w') as f:
			json.dump(actor_state, f)

		print(f'Actor#: {self.actor_id} end')


if __name__ == "__main__":
	""" 
	Simple standalone test routine for Actor class
	"""
	import learner

	with open('parameters.json', 'r') as f:
		params = json.load(f)

	param_set_id = db_initializer.initialize(params)

	mp_manager = mp.Manager()
	status_dict = mp_manager.dict()
	shared_state = mp_manager.dict()
	shared_mem = mp_manager.Queue()

	params['actor']['wait_shared_memory_clear'] = False

	status_dict['quit'] = False
	status_dict['Q_state_dict_stored'] = False
	status_dict['request_quit'] = False

	l = learner.Learner(params, param_set_id, status_dict, shared_state, shared_mem)

	actor = Actor(params, param_set_id, 0, status_dict, shared_state, shared_mem)
	actor.run()
