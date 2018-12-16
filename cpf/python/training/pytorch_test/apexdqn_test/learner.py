#!/usr/bin/env python
import torch
import time
import numpy as np
from collections import namedtuple
from duelling_network import DuellingDQN
from replay import ReplayMemory

N_Step_Transition = namedtuple('N_Step_Transition', ['S_t', 'A_t', 'R_ttpB', 'Gamma_ttpB', 'qS_t', 'S_tpn', 'qS_tpn', 'key'])


class Learner(object):

	def __init__(self, env_conf, replay_params, status_dict, learner_params, shared_state, shared_mem):
		self.status_dict = status_dict
		self.state_shape = env_conf['state_shape']
		action_dim = env_conf['action_dim']
		self.params = learner_params
		self.shared_state = shared_state
		self.Q = DuellingDQN(self.state_shape, action_dim)
		self.Q_double = DuellingDQN(self.state_shape, action_dim) # Target Q network which is slow moving replica of self.Q
		self.shared_mem = shared_mem
		self.replay_memory = ReplayMemory(replay_params)
		self.optimizer = torch.optim.RMSprop(self.Q.parameters(), lr=0.00025 / 4, weight_decay=0.95, eps=1.5e-7)
		self.num_q_updates = 0

		if self.params['load_saved_state']:
			try:
				saved_state = torch.load(self.params['load_saved_state'])
				self.Q.load_state_dict(saved_state['module'])
				self.optimizer.load_state_dict(saved_state['optimizer'])
			except FileNotFoundError:
				print("WARNING: No trained model found. Training from scratch")

		self.shared_state["Q_state_dict"] = self.Q.state_dict()

	def compute_loss_and_priorities(self, xp_batch):
		"""
		Computes the double-Q learning loss and the proportional experience priorities.
		:param xp_batch: list of experiences of type N_Step_Transition
		:return: double-Q learning loss and the proportional experience priorities
		"""
		n_step_transitions = N_Step_Transition(*zip(*xp_batch))
		# Convert tuple to numpy array; Convert observations(S_t and S_tpn) to c x w x h torch Tensors (aka Variable)
		S_t = torch.from_numpy(np.array(n_step_transitions.S_t)).float().requires_grad_(True)
		S_tpn = torch.from_numpy(np.array(n_step_transitions.S_tpn)).float().requires_grad_(True)
		rew_t_to_tpB = torch.tensor(n_step_transitions.R_ttpB)
		gamma_t_to_tpB = torch.tensor(n_step_transitions.Gamma_ttpB)
		A_t = np.array(n_step_transitions.A_t)

		with torch.no_grad():
			G_t = rew_t_to_tpB + gamma_t_to_tpB * \
                         self.Q_double(S_tpn)[2].gather(1, torch.argmax(self.Q(S_tpn)[2], 1).view(-1, 1)).squeeze()
		Q_S_A = self.Q(S_t)[2].gather(1, torch.from_numpy(A_t).reshape(-1, 1)).squeeze()
		batch_td_error = G_t.float() - Q_S_A
		loss = 1 / 2 * (batch_td_error)**2
		# Compute the new priorities of the experience
		priorities = {k: v for k in n_step_transitions.key for v in abs(batch_td_error.detach().data.numpy())}

		return loss.mean(), priorities

	def update_Q(self, loss):
		self.optimizer.zero_grad()
		loss.backward()
		self.optimizer.step()
		self.num_q_updates += 1

		if self.num_q_updates % self.params['q_target_sync_freq']:
			self.Q_double.load_state_dict(self.Q.state_dict())

	def add_experience_to_replay_mem(self):
		while self.shared_mem.qsize() or not self.shared_mem.empty():
			priorities, xp_batch = self.shared_mem.get()
			self.replay_memory.add(priorities, xp_batch)
		print('add_experience_to_replay_mem', self.replay_memory.size())

	def learn(self):
		while self.replay_memory.size() <= self.params["min_replay_mem_size"]:
			self.add_experience_to_replay_mem()
			time.sleep(1)
		t = 0
		print('learner start')
		while not self.status_dict['quit']:
			print('learn')
			self.add_experience_to_replay_mem()
			# 4. Sample a prioritized batch of transitions
			prioritized_xp_batch = self.replay_memory.sample(int(self.params['replay_sample_size']))
			# 5. & 7. Apply double-Q learning rule, compute loss and experience priorities
			loss, priorities = self.compute_loss_and_priorities(prioritized_xp_batch)
			#print("\nLearner: t=", t, "loss:", loss, "RPM.size:", self.replay_memory.size(), end='\r')
			# 6. Update parameters of the Q network(s)
			self.update_Q(loss)
			self.shared_state['Q_state_dict'] = self.Q.state_dict()
			# 8. Update priorities
			self.replay_memory.set_priorities(priorities)

			# 9. Periodically remove old experience from replay memory
			if t % self.params['remove_old_xp_freq'] == 0:
				self.replay_memory.remove_to_fit()
			t += 1

		print('learner end')

		state_dict = {
		    'module': self.Q.state_dict(),
		    'optimizer': self.optimizer.state_dict(),
		}
		torch.save(state_dict, 'model.pt')


if __name__ == "__main__":
	""" 
	Simple standalone test routine for Leaner class
	"""
	import multiprocessing as mp
	from actor import Actor

	env_conf = {"state_shape": (1, 84, 84), "action_dim": 4, "name": "Breakout-v0"}
	replay_params = {"soft_capacity": 100000, "priority_exponent": 0.6, "importance_sampling_exponent": 0.4}
	learner_params = {"remove_old_xp_freq": 100, "q_target_sync_freq": 100, "min_replay_mem_size": 20, "replay_sample_size": 32, "load_saved_state": False}
	actor_params = {"local_experience_buffer_capacity": 10, "epsilon": 0.4, "alpha": 7, "gamma": 0.99, "num_actors": 2, "n_step_transition_batch_size": 5, "Q_network_sync_freq": 10, "num_steps": 3, "T": 500}
	dummy_q = DuellingDQN(env_conf['state_shape'], env_conf['action_dim'])
	mp_manager = mp.Manager()

	status_dict = mp_manager.dict()
	shared_state = mp_manager.dict()
	shared_state["Q_state_dict"] = dummy_q.state_dict()
	shared_replay_mem = mp_manager.Queue()

	status_dict['quit'] = False

	learner = Learner(env_conf, replay_params, status_dict, learner_params, shared_state, shared_replay_mem)
	actor = Actor(1, env_conf, shared_state, shared_replay_mem, actor_params)
	actor.run()

	learner.learn()

	print("Main: replay_mem.size:", shared_replay_mem.qsize())
	for i in range(shared_replay_mem.qsize()):
		p, xp_batch = shared_replay_mem.get()
		print("priority:", p)
