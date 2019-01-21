import typing
import numpy as np
import trade_environment


def detect_turning_points(values: np.ndarray, gap: int) -> typing.Tuple[np.ndarray, np.ndarray]:
	"""指定数列の折返しポイントの地点を検出する.

	Args:
		values: 数列.
		gap: 折返し判定閾値、この値を超えて反転したら折返しと判断する.

	Returns:
		(折返しインデックス, 検出途中に生成した一定値以上距離を保って付いてくる値の数列) のタプル.
	"""
	indices = []
	stalkers = np.empty((len(values),), dtype=np.int32)
	last_value = int(values[0])
	stalker = last_value
	stalkers[0] = stalker
	last_i = 0
	for i in range(1, len(values)):
		v = int(values[i])
		up = last_value < stalker and stalker <= v
		down = stalker < last_value and v <= stalker
		if up or down:
			delta_array = values[last_i:i + 1]
			tpi = last_i + int(np.argmin(delta_array) if up else np.argmax(delta_array))
			tpv = int(values[tpi])
			indices.append(tpi)
			last_i = i
			stalker = tpv - gap if up else tpv + gap
			# indices.append(i - 1)
			# stalker = v - gap if up else v + gap
		else:
			d = v - stalker
			if d < -gap:
				stalker = v + gap
			elif gap < d:
				stalker = v - gap
		stalkers[i] = stalker
		last_value = v
	return np.array(indices, dtype=np.int32), stalkers


class TpActionSuggester:
	"""予め折り返し点を探索し、それを用いて指定環境での状態からお勧めアクションを提示するクラス.
	"""

	def __init__(self, env: trade_environment.TradeEnvironment, spread_adj: int = 1) -> None:
		self.env = env # トレード用環境
		self.threshould = int(np.rint(env.spread * spread_adj).item()) # エントリーするかどうか判断する閾値、現在値と折返し値の差がこの値以下ならエントリーしない
		self.tp_indices = np.empty((0,), dtype=np.int32) # 折返し点のエピソード内インデックス一覧
		self.tp_values = np.empty((0,), dtype=np.int32) # 折返し点の値一覧

	def start_episode(self) -> None:
		"""トレード用環境のエピソード開始直後に呼び出す必要がある."""
		values = self.env.episode_values
		c = values[:, 3]
		self.tp_indices, _ = detect_turning_points(c, self.threshould)
		self.tp_values = c[self.tp_indices]

	def get_next_turning_index(self) -> int:
		"""次の折返しインデックスの取得."""
		i1 = np.where(self.env.index_in_episode <= self.tp_indices)[0][:1]
		return i1.item() if i1.size else -1

	def get_suggested_action(self) -> int:
		"""現状の状態でのお勧めアクションの取得."""
		tp_indices = self.tp_indices # 折返し点インデックス列
		tp_values = self.tp_values # 折り返し点値列
		value = self.env.get_value() # 現在値
		tp_idx = self.get_next_turning_index() # 未来の直近折り返し点インデックス
		tp_delta = None # 現在値から次の折返し点の値への差
		on_tp = False # 現在折り返し点上かどうか
		if 0 <= tp_idx:
			# 現在が丁度折り返し点なら次の折返し点が目標となる
			if tp_indices[tp_idx] == self.env.index_in_episode:
				on_tp = True
				tp_idx += 1

			# まだこれから折り返し点があるなら現在値との差分を計算
			if tp_idx < len(tp_values):
				tp_delta = tp_values[tp_idx] - value

			threshould = self.threshould

		suggested_action = 0 # 基本何もしない

		if self.env.position_type == 0:
			# ポジション持っておらず、次の折返し値との差が閾値より大きいなら売買する
			if tp_delta is not None:
				if threshould < tp_delta:
					suggested_action = 1
				elif tp_delta < -threshould:
					suggested_action = 2
		else:
			# 既にポジション持っている際の処理
			if on_tp:
				# 現在が折り返し点上の場合は次の折返しに備える
				suggested_action = 3
				if tp_delta is not None:
					if threshould < tp_delta:
						suggested_action = 1
					elif tp_delta < -threshould:
						suggested_action = 2
			else:
				# 現在が折り返し点間の場合は必要に応じてポジションを調整する
				suggested_action = 0
				if tp_delta is not None:
					if threshould < tp_delta and self.env.position_type != 1:
						suggested_action = 1
					elif tp_delta < -threshould and self.env.position_type != -1:
						suggested_action = 2

		return suggested_action


class TpRewardAdjuster:
	"""TpActionSuggester 用の指定アクションから報酬調整処理を行うクラス.
	"""

	def __init__(self, action_suggester: TpActionSuggester):
		self.action_suggester = action_suggester
		self.env = action_suggester.env
		self.threshould = action_suggester.threshould
		self.idle_decrease_rate = 0.03 # チャンスを無駄にした際に報酬から減衰させる比率

	def adjust_reward(self, action) -> float:
		"""現状の状態で指定のアクションを行った際の報酬調整料の取得."""
		tp_indices = self.action_suggester.tp_indices # 折返し点インデックス列
		tp_values = self.action_suggester.tp_values # 折り返し点値列
		value = self.env.get_value() # 現在値
		tp_idx = self.action_suggester.get_next_turning_index() # 未来の直近折り返し点インデックス
		tp_delta = None # 現在値から次の折返し点の値への差
		on_tp = False # 現在折り返し点上かどうか
		if 0 <= tp_idx:
			# 現在が丁度折り返し点なら次の折返し点が目標となる
			if tp_indices[tp_idx] == self.env.index_in_episode:
				on_tp = True
				tp_idx += 1

			# まだこれから折り返し点があるなら現在値との差分を計算
			if tp_idx < len(tp_values):
				tp_delta = tp_values[tp_idx] - value

		reward = 0.0

		if 1 <= action and action <= 3 and self.env.position_type != 0:
			# 決済するなら残りの損益から報酬を調整する
			if tp_delta is not None and not on_tp:
				reward -= 0.01 * self.env.position_type * tp_delta

		if action == 0:
			# チャンスがある状態で何もしていないなら報酬を減衰させる
			if tp_delta is not None and self.threshould < abs(tp_delta) and self.env.position_type == 0:
				reward -= self.idle_decrease_rate * abs(tp_delta)
		elif action == 1 or action == 2:
			# 売買の方向と次の折り返し点への差分から報酬を調整する
			if tp_delta is not None:
				reward += 0.01 * (1.0 if action == 1 else -1.0) * tp_delta

		return reward
