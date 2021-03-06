import numpy as np
import torch

class ReplayMemory(object):

	def __init__(self, params):
		self.capacity = params['soft_capacity']
		self.priority_exponent = params['priority_exponent']
		self.importance_sampling_exponent = params['importance_sampling_exponent']
		self.start = 0
		self.end = 0
		self.length = 0
		self.priorities = np.zeros((self.capacity,), dtype=np.float32)
		self.transitions = np.zeros((self.capacity,), dtype=np.object)

	def sample(self, sample_size):
		# 有効データ部位取得
		if self.length < self.capacity:
			priorities = self.priorities[:self.end]
			transitions = self.transitions[:self.end]
		else:
			priorities = self.priorities
			transitions = self.transitions

		# 優先度を元にインデックス番号を抽出
		indices = torch.multinomial(torch.tensor(priorities, dtype=torch.float32), sample_size, replacement=False).numpy()

		# 取得されたインデックスを元に tensor 作成
		transitions = transitions[indices]
		s = torch.tensor([t[0] for t in transitions], dtype=torch.float32)
		a = torch.tensor([t[1].astype(np.int64) for t in transitions], dtype=torch.int64)
		r = torch.tensor([t[2] for t in transitions], dtype=torch.float32)
		a_latest = torch.tensor([t[3].astype(np.int64) for t in transitions], dtype=torch.int64)
		s_latest = torch.tensor([t[4] for t in transitions], dtype=torch.float32)
		terminal = torch.tensor([t[5].astype(np.float32) for t in transitions], dtype=torch.float32)

		return indices, (s, a, r, a_latest, s_latest, terminal), priorities[indices]

	def set_priorities(self, indices, priorities):
		self.priorities[indices] = (priorities + self.importance_sampling_exponent)**self.priority_exponent 

	def add(self, priorities, n_step_transitions):
		l = len(priorities)
		cap = self.capacity

		# 入力データが容量を超える様なら最後尾側を優先し、バッファ全体を更新となる
		if cap < l:
			start = l - cap
			priorities = priorities[start:]
			n_step_transitions = n_step_transitions[start:]
			l = cap
			self.end = 0

		priorities = (priorities + self.importance_sampling_exponent)**self.priority_exponent 

		# バッファへコピー、その際バッファ終端を跨ぐなら２回に分けて行う
		pos = self.end
		end = pos + l
		if cap < end:
			n = cap - pos
			self.priorities[pos:] = priorities[:n]
			self.transitions[pos:] = n_step_transitions[:n]
			s = n
			n = end - cap
			self.priorities[:n] = priorities[s:]
			self.transitions[:n] = n_step_transitions[s:]
		else:
			self.priorities[pos:end] = priorities
			self.transitions[pos:end] = n_step_transitions

		# 有効データ数更新
		self.length += l
		if cap < self.length:
			self.length = cap

		# 有効データ範囲更新
		self.end = end % cap
		self.start = (end + cap - self.length) % cap

	def size(self):
		return self.length
