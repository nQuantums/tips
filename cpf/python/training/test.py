from collections import deque
from sumtree import SumTree

st = SumTree(10)

st.add(1.0, 1)
st.add(2.0, 2)
st.add(3.0, 3)
print(st.data)
print(st.tree)
i, p, d = st.get(1)
st.update(i, 4)
print(st.data)
print(st.tree)

# q = deque()

# for i in range(10):
# 	q.append(i)
# 	print(q)
# 	if 3 < len(q):
# 		q.popleft()

# import numpy as np

# a = np.arange(1 * 84 * 84, dtype=np.int32).reshape((1, 84, 84))
# a = np.concatenate((a, np.arange(1 * 84 * 84, dtype=np.int32).reshape((1, 84, 84))), axis=0)
# a = np.concatenate((a, np.arange(1 * 84 * 84, dtype=np.int32).reshape((1, 84, 84))), axis=0)
# a = np.concatenate((a, a[1:]), axis=0)

# print(a)
# print(np.take(a, i))
# print(a[i])



# import time
# import filelock


# def interlocked(lock_file_name, func, timeout=None):
# 	try:
# 		with filelock.FileLock(lock_file_name).acquire(timeout=timeout):
# 			func()
# 			return True
# 	except filelock.Timeout:
# 		return False

# def file_write():
# 	with open('file', 'w') as f:
# 		f.write('afe')
# 		time.sleep(10)


# print('begin')
# print(interlocked('lock', file_write, timeout=1))
# print('end')


# import io
# import pickle
# import compressed_pickle
# import numpy as np
# from collections import namedtuple

# print(np.prod((1, 2, 3)).item())


# class Transition:

# 	def __init__(self, state, action, reward, next_state, terminal, q, next_q):
# 		self.state = state
# 		self.action = action
# 		self.reward = reward
# 		self.next_state = next_state
# 		self.terminal = terminal
# 		self.q = q
# 		self.next_q = next_q

# 	@staticmethod
# 	def func1(text):
# 		print(text)

# 	def func2(self, text):
# 		self.func1(text)

# t = Transition(1, 2, 3, 4, 5, 6, 7)
# t.func2('afefefe')

# N_Step_Transition = namedtuple('N_Step_Transition', ['S_t', 'A_t', 'R_ttpB', 'Gamma_ttpB', 'qS_t', 'S_tpn', 'qS_tpn', 'key'])

# file = io.BytesIO()
# pickle.dump(Transition(1, 2, 3, 4, 5, 6, 7), file)
# file.seek(0)
# obj = pickle.load(file)
# print(obj)

# a = np.array([[1, 2], [3, 4]])
# binary = pickle.dumps(a)
# print(binary)

# b = pickle.loads(binary)

# d = {'a': a, 'b': b, 'c': 'afefefe'}
# binary = pickle.dumps(d)
# e = pickle.loads(binary)

# a = np.arange(65536, dtype=np.float32).reshape((256, 256))

# binary = pickle.dumps(a)
# binaryc = compressed_pickle.dumps(a)
# print(a.size * a.itemsize, len(binary), len(binaryc))

