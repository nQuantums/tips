#!/usr/bin/env python
import time
import datetime
import multiprocessing as mp
import random
import numpy as np
from collections import deque
from collections import namedtuple
import cv2
import torch
import psycopg2

import db_initializer
import tables
from env import make_local_env
import model

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
		model_formula = f'model.{lp["model"]}(self.state_shape, self.action_dim, "Actor# {actor_id}")'
		model_formula_target = f'model.{lp["model"]}(self.state_shape, self.action_dim)'

		self.state_shape = tuple(ep['state_shape'])
		self.action_dim = ep['action_dim']
		self.frame_num = self.state_shape[0]
		self.frames = np.zeros((0,) + self.state_shape[1:], dtype=np.float32)
		self.num_steps = ap["num_steps"] # Nステップ数
		self.n_step_transition_batch_size = ap['n_step_transition_batch_size']
		self.env = make_local_env(ep['name'])
		self.epsilon = ap['epsilon']**(1 + ap['alpha'] * self.actor_id / (ap['num_actors'] - 1))
		self.rgb2gray_k = np.array([[0.299 / 255, 0.587 / 255, 0.114 / 255]], dtype=np.float32).T
		self.last_Q_state_dict_id = 0

		self.Q = eval(model_formula)
		self.Q_target = eval(model_formula_target) # Target Q network which is slow moving replica of self.Q
		Q_state_dict = self.shared_state["Q_state_dict"]
		self.Q.load_state_dict(Q_state_dict[0])
		self.Q_target.load_state_dict(Q_state_dict[1])

	def policy(self, q):
		return random.randrange(0, len(q)) if random.random() < self.epsilon else torch.argmax(q, 0).item()

	def obs_to_state(self, obs):
		obs = cv2.resize(obs, (self.state_shape[2], self.state_shape[1]))
		obs = np.dot(obs.astype(np.float32), self.rgb2gray_k)
		cv2.imshow(f'Actor{self.actor_id}', obs)
		# cv2.waitKey(1)
		obs = obs.reshape((1,) + obs.shape[:2])
		if self.frames.shape[0] < self.frame_num:
			self.frames = np.concatenate((self.frames, obs), axis=0)
		else:
			self.frames = np.concatenate((self.frames[1:], obs), axis=0)
		return self.frames

	def run(self):
		ap = self.params['actor']

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
		priorities = None

		transitions = deque()
		n_step_transitions = []
		n_step_transition_batch_size = self.n_step_transition_batch_size
		num_steps = self.num_steps
		gamma = ap['gamma']
		gamma_n = gamma**num_steps

		Q = self.Q
		Q_target = self.Q_target
		take_offsets = torch.arange(n_step_transition_batch_size) * self.action_dim
		q = None
		priorities = None

		wait_shared_memory_clear = ap['wait_shared_memory_clear']

		while not status_dict['request_quit']:
			ep_len = 0
			ep_reward = 0.0
			terminal = False
			last_lives = 3
			last_reward = None
			continuous_no_reward_frames = 0

			self.env.reset()
			for _ in range(self.frame_num):
				state, _, _, _ = self.env.step(0)
				state = self.obs_to_state(state)
			while not terminal:
				# 状態を基に取るべき行動を選択する
				with torch.no_grad():
					# model.show_plot = True
					q = Q(torch.tensor(state.reshape((1,) + state.shape))).squeeze()
					# model.show_plot = False
				action = self.policy(q)

				# 環境に行動を適用し次の状態を取得する
				next_state, reward_org, terminal, info = self.env.step(action)
				next_state = self.obs_to_state(next_state)
				reward = reward_org

				# パックマン専用の報酬調整
				if not reward:
					continuous_no_reward_frames += 1
				else:
					continuous_no_reward_frames = 0
				if not reward and last_reward == 0:
					reward -= continuous_no_reward_frames # ぼーっとしていると報酬が減っていく
				lives = info['ale.lives']
				if lives < last_lives:
					last_lives = lives
					reward -= 30 + continuous_no_reward_frames # 食われたら報酬ががっつり減る
				last_reward = reward_org

				# N-StepTransition のために状態遷移情報を追加する
				transitions.append((state, action, reward, next_state, terminal))

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
							model.show_plot = True
							Gt = r + (1.0 - term) * gamma_n * Q_target(s_latest).take(take_offsets + a_latest).squeeze()
							priorities = (Gt - Q(s).take(take_offsets + a).squeeze()).abs()
							model.show_plot = False
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

						# DB登録のため list へ変換しておく
						priorities = priorities.tolist()

				# Learner からの共有パラメータが更新されていたらロードする
				id = status_dict['Q_state_dict_id']
				if 3 <= id - self.last_Q_state_dict_id:
					print(f'Actor#: {self.actor_id} state loaded.')
					Q_state_dict = self.shared_state["Q_state_dict"]
					self.Q.load_state_dict(Q_state_dict[0])
					self.Q_target.load_state_dict(Q_state_dict[1])
					self.last_Q_state_dict_id = id

				# DB登録のため list へ変換しておく
				q = q.tolist()

				# 終了要求があったらループを抜ける
				if cv2.waitKey(1) == 27 or status_dict['request_quit']:
					status_dict['request_quit'] = True
					break

				# 次のループに備える
				state = next_state
				ep_reward += reward_org
				ep_len += 1

			# DBへデータ登録
			print(f'Actor#: {self.actor_id} t: {t} ep_len: {ep_len} ep_reward: {ep_reward}')
			train_num = status_dict['train_num']
			record = record_type(param_set_id, actor_id, now(), train_num, ep_len, ep_reward, q, action, priorities)
			with conn.cursor() as cur:
				record_insert(cur, record)

		print(f'Actor#: {self.actor_id} end')


if __name__ == "__main__":
	""" 
	Simple standalone test routine for Actor class
	"""
	import json
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
