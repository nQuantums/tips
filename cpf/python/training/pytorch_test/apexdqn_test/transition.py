class Transition:
	__slots__ = ['S', 'A', 'R', 'Gamma', 'q']

	def __init__(self, S, A, R, Gamma, q):
		self.S = S # 遷移前状態
		self.A = A # 行ったアクション
		self.R = R # 報酬
		self.Gamma = Gamma # 報酬減衰係数
		self.q = q # Q値

class NStepTransition:
	__slots__ = ['S', 'A', 'R', 'Gamma', 'S_last', 'max_q']

	def __init__(self, S, A, R, Gamma, S_last, max_q):
		self.S = S # 遷移前状態
		self.A = A # 行ったアクション
		self.R = R # 累積報酬
		self.Gamma = Gamma # 報酬減衰係数
		self.S_last = S_last # 最後の状態
		self.max_q = max_q # 最後のQ値の最大値
