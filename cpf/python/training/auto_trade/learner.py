#!/usr/bin/env python
import os
import time
import datetime
import multiprocessing as mp
from collections import OrderedDict
import torch
import torch.nn.functional as F
import numpy as np
from collections import namedtuple
import psycopg2

import trade_environment
import db_initializer
import tables
from replay import ReplayMemory
import model


class Learner(object):

	def __init__(self, params, param_set_id, status_dict, shared_state, remote_mem):
		self.params = params
		self.param_set_id = param_set_id
		self.status_dict = status_dict
		self.shared_state = shared_state
		self.remote_mem = remote_mem

		gpu = 0
		torch.cuda.set_device(gpu)

		ep = params['env']
		ap = params['actor']
		lp = params['learner']
		rmp = params["replay_memory"]

		model_formula = f'model.{lp["model"]}(self.state_shape, self.action_dim, hidden_size={lp["hidden_size"]}).to(self.device)'
		optimizer_formula = lp["optimizer"].format('self.Q.parameters()')

		self.conn = psycopg2.connect(params["db"]["connection_string"])
		self.conn.autocommit = True
		self.cur = self.conn.cursor()

		self.device = torch.device("cuda:{}".format(gpu) if 0 <= gpu and torch.cuda.is_available() else "cpu")
		self.frames_height_width = tuple(ep['frames_height_width'])
		self.state_shape = trade_environment.get_state_shape(self.frames_height_width)
		self.batch_size = lp['replay_sample_size']
		self.action_dim = trade_environment.action_num
		self.q_target_sync_freq = lp['q_target_sync_freq']
		self.num_q_updates = 0
		self.take_offsets = (torch.arange(self.batch_size) * self.action_dim).to(self.device)
		self.Q = eval(model_formula)
		self.Q_target = eval(model_formula) # Target Q network which is slow moving replica of self.Q
		self.optimizer = eval(optimizer_formula)
		self.replay_memory = ReplayMemory(rmp)

		self.train_num = 0
		self.model_file_name = db_initializer.get_state_dict_name(params)
		if self.model_file_name and os.path.isfile(self.model_file_name):
			print(f'Loading {self.model_file_name}')
			saved_state = torch.load(self.model_file_name)
			self.Q.load_state_dict(saved_state['module'])
			self.optimizer.load_state_dict(saved_state['optimizer'])
			self.train_num = saved_state['train_num']

		self.shared_state['Q_state_dict'] = self.state_dict_to_cpu(self.Q.state_dict()), self.state_dict_to_cpu(
		    self.Q_target.state_dict())
		self.status_dict['Q_state_dict_stored'] = True

		self.last_Q_state_dict_id = 1
		self.status_dict['Q_state_dict_id'] = self.last_Q_state_dict_id
		self.status_dict['train_num'] = self.train_num

		self.gamma_n = params['actor']['gamma']**params['actor']['num_steps']

	def state_dict_to_cpu(self, state_dict):
		d = OrderedDict()
		for k, v in state_dict.items():
			d[k] = v.cpu()
		return d

	def add_experience_to_replay_mem(self):
		while self.remote_mem.qsize():
			priorities, batch = self.remote_mem.get()
			self.replay_memory.add(priorities, batch)

	def compute_loss_and_priorities(self, batch_size):
		indices, n_step_transition_batch, before_priorities = self.replay_memory.sample(batch_size)

		s = n_step_transition_batch[0].to(self.device)
		a = n_step_transition_batch[1].to(self.device)
		r = n_step_transition_batch[2].to(self.device)
		a_latest = n_step_transition_batch[3].to(self.device)
		s_latest = n_step_transition_batch[4].to(self.device)
		terminal = n_step_transition_batch[5].to(self.device)

		q = self.Q(s)
		q_a = q.take(self.take_offsets + a).squeeze()

		with torch.no_grad():
			self.Q_target.eval()
			Gt = r + (1.0 - terminal) * self.gamma_n * self.Q_target(s_latest).take(self.take_offsets + a_latest).squeeze()
			td_error = Gt - q_a

		loss = F.smooth_l1_loss(q_a, Gt)
		# loss = td_error**2 / 2

		# Compute the new priorities of the experience
		after_priorities = td_error.data.abs().cpu().numpy()
		self.replay_memory.set_priorities(indices, after_priorities)

		return loss, q, before_priorities, after_priorities, indices

	def update_Q(self, loss):
		self.optimizer.zero_grad()
		loss.backward()
		self.optimizer.step()
		self.num_q_updates += 1

		if self.num_q_updates % self.q_target_sync_freq == 0:
			self.Q_target.load_state_dict(self.Q.state_dict())
			print(f'Target Q synchronized.')
			return True
		else:
			return False

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
		min_replay_mem_size = self.params['learner']["min_replay_mem_size"]

		print('learner waiting for replay memory.')
		while self.replay_memory.size() <= min_replay_mem_size:
			self.add_experience_to_replay_mem()
			time.sleep(0.01)
		step_num = 0
		print('learner start')
		while not self.status_dict['quit']:
			self.add_experience_to_replay_mem()
			# 4. Sample a prioritized batch of transitions
			# 5. & 7. Apply double-Q learning rule, compute loss and experience priorities
			# 8. Update priorities
			loss, q, before_priorities, after_priorities, indices = self.compute_loss_and_priorities(self.batch_size)
			if step_num % 10 == 0:
				print(f'loss : {loss}')
			#print("\nLearner: step_num=", step_num, "loss:", loss, "RPM.size:", self.replay_memory.size(), end='\r')
			# 6. Update parameters of the Q network(s)
			if self.update_Q(loss):
				target_sync_num += 1
			if step_num % 5 == 0:
				self.shared_state['Q_state_dict'] = self.state_dict_to_cpu(self.Q.state_dict()), self.state_dict_to_cpu(
				    self.Q_target.state_dict())
				self.last_Q_state_dict_id += 1
				self.status_dict['Q_state_dict_id'] = self.last_Q_state_dict_id
				print('Send params to actors.')
				send_param_num += 1

			# 9. Periodically remove old experience from replay memory
			step_num += 1
			self.train_num += 1
			self.status_dict['train_num'] = self.train_num

			# DBへデータ登録
			r = record_type(param_set_id, now(), self.train_num,
			                step_num, loss.item(), q[0].tolist(), before_priorities.tolist(), after_priorities.tolist(),
			                indices.tolist(), target_sync_num, send_param_num)
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
	params['actor']['T'] = 2000
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
