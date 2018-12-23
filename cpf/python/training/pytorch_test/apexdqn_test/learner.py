#!/usr/bin/env python
import os
import time
import datetime
import multiprocessing as mp
from collections import OrderedDict
import torch
import numpy as np
from collections import namedtuple
import psycopg2

import db_initializer
import tables
from replay import ReplayMemory
import model


class Learner(object):

	def __init__(self, params, param_set_id, status_dict, shared_state, shared_mem):
		gpu = 0
		torch.cuda.set_device(gpu)

		env_conf = params['env']
		learner_params = params['learner']

		model_formula = f'model.{learner_params["model"]}(self.state_shape, self.action_dim).to(self.device)'
		optimizer_formula = learner_params["optimizer"].format('self.Q.parameters()')

		self.conn = psycopg2.connect(params["db"]["connection_string"])
		self.conn.autocommit = True
		self.cur = self.conn.cursor()
		self.param_set_id = param_set_id

		self.device = torch.device("cuda:{}".format(gpu) if 0 <= gpu and torch.cuda.is_available() else "cpu")
		self.status_dict = status_dict
		self.state_shape = env_conf['state_shape']
		self.batch_size = learner_params['replay_sample_size']
		self.action_dim = env_conf['action_dim']
		self.params = learner_params
		self.q_target_sync_freq = self.params['q_target_sync_freq']
		self.shared_state = shared_state
		self.shared_mem = shared_mem
		self.replay_memory = ReplayMemory(params["replay_memory"])
		self.num_q_updates = 0
		self.offsets_for_action = (torch.arange(self.batch_size) * self.action_dim).to(self.device)
		self.Q = eval(model_formula)
		self.Q_double = eval(model_formula) # Target Q network which is slow moving replica of self.Q
		self.optimizer = eval(optimizer_formula)

		self.train_num = 0
		self.model_file_name = self.params['load_saved_state']
		if self.model_file_name and os.path.isfile(self.model_file_name):
			print(f'Loading {self.model_file_name}')
			saved_state = torch.load(self.model_file_name)
			self.Q.load_state_dict(saved_state['module'])
			self.optimizer.load_state_dict(saved_state['optimizer'])
			self.train_num = saved_state['train_num']

		self.shared_state['Q_state_dict'] = self.state_dict_to_cpu(self.Q.state_dict())
		self.status_dict['Q_state_dict_stored'] = True

		self.last_Q_state_dict_id = 1
		self.status_dict['Q_state_dict_id'] = self.last_Q_state_dict_id
		self.status_dict['train_num'] = self.train_num

	def state_dict_to_cpu(self, state_dict):
		d = OrderedDict()
		for k, v in state_dict.items():
			d[k] = v.cpu()
		return d

	def compute_loss_and_priorities(self, batch_size):
		"""
		Computes the double-Q learning loss and the proportional experience priorities.
		:param xp_batch: list of experiences of type N_Step_Transition
		:return: double-Q learning loss and the proportional experience priorities
		"""
		indices, n_step_transition_batch, priorities = self.replay_memory.sample(batch_size)

		S = n_step_transition_batch.S.to(self.device)
		A = n_step_transition_batch.A.to(self.device) + self.offsets_for_action
		R = n_step_transition_batch.R.to(self.device)
		G = n_step_transition_batch.Gamma.to(self.device)
		S_last = n_step_transition_batch.S_last.to(self.device)

		with torch.no_grad():
			Qd = self.Q_double(S_last)
			Q = self.Q(S_last)
			actions = torch.argmax(Q, 1).squeeze() + self.offsets_for_action
			G_t = R + G * Qd.take(actions).squeeze()
		q = self.Q(S)
		batch_td_error = G_t - q.take(A).squeeze()
		loss = 1 / 2 * (batch_td_error)**2

		# Compute the new priorities of the experience
		priorities = batch_td_error.data.abs().cpu().numpy()
		self.replay_memory.set_priorities(indices, priorities)

		return loss.mean(), priorities, q

	def update_Q(self, loss):
		self.optimizer.zero_grad()
		loss.backward()
		self.optimizer.step()
		self.num_q_updates += 1

		if self.num_q_updates % self.q_target_sync_freq == 0:
			self.Q_double.load_state_dict(self.Q.state_dict())
			print(f'Target Q synchronized.')
			return True
		else:
			return False

	def add_experience_to_replay_mem(self):
		while self.shared_mem.qsize() or not self.shared_mem.empty():
			priorities, xp_batch = self.shared_mem.get()
			self.replay_memory.add(priorities, xp_batch)
			# print(f'replay_memory.length : {self.replay_memory.length}')

	def learn(self):
		t = tables.LearnerData()
		record_type = t.get_record_type()
		record_insert = t.get_insert()
		cur = self.cur
		param_set_id = self.param_set_id
		now = datetime.datetime.now
		step_num = 0
		target_sync_num = 0
		send_param_num = 0

		print('learner waiting for replay memory.')
		while self.replay_memory.size() <= self.params["min_replay_mem_size"]:
			self.add_experience_to_replay_mem()
			time.sleep(0.01)
		step_num = 0
		print('learner start')
		while not self.status_dict['quit']:
			self.add_experience_to_replay_mem()
			# 4. Sample a prioritized batch of transitions
			# 5. & 7. Apply double-Q learning rule, compute loss and experience priorities
			# 8. Update priorities
			loss, priorities, q = self.compute_loss_and_priorities(self.batch_size)
			if step_num % 10 == 0:
				print(f'loss : {loss}')
			#print("\nLearner: step_num=", step_num, "loss:", loss, "RPM.size:", self.replay_memory.size(), end='\r')
			# 6. Update parameters of the Q network(s)
			if self.update_Q(loss):
				target_sync_num += 1
			if step_num % 5 == 0:
				self.shared_state['Q_state_dict'] = self.state_dict_to_cpu(self.Q.state_dict())
				self.last_Q_state_dict_id += 1
				self.status_dict['Q_state_dict_id'] = self.last_Q_state_dict_id
				print('Send params to actors.')
				send_param_num += 1

			# 9. Periodically remove old experience from replay memory
			step_num += 1
			self.train_num += 1
			self.status_dict['train_num'] = self.train_num

			# DBへデータ登録
			r = record_type(param_set_id, now(), self.train_num, step_num, loss.item(), q[0].tolist(), priorities.tolist(),
			                target_sync_num, send_param_num)
			record_insert(cur, r)

		print('learner end')

		state_dict = {'module': self.Q.state_dict(), 'optimizer': self.optimizer.state_dict(), 'train_num': self.train_num}
		torch.save(state_dict, self.model_file_name)


if __name__ == "__main__":
	""" 
	Simple standalone test routine for Leaner class
	"""
	import json
	import actor

	with open('parameters.json', 'r') as f:
		params = json.load(f)
	params['actor']['T'] = 5000
	params['actor']['wait_shared_memory_clear'] = False

	param_set_id = db_initializer.initialize(params)

	mp_manager = mp.Manager()
	status_dict = mp_manager.dict()
	shared_state = mp_manager.dict()
	shared_mem = mp_manager.Queue()

	status_dict['quit'] = False
	status_dict['Q_state_dict_stored'] = False
	status_dict['request_quit'] = False

	l = Learner(params, param_set_id, status_dict, shared_state, shared_mem)

	actor = actor.Actor(params, param_set_id, 0, status_dict, shared_state, shared_mem)
	actor.run()

	l.learn()
