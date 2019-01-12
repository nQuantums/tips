def run():
	reward_org = 0
	reward = 0
	last_positional_reward = None
	old_position_type = 0

	def adj_positional_reward_delta():
		nonlocal last_positional_reward, reward
		pr = 0
		delta = pr if last_positional_reward is None else pr - last_positional_reward
		reward += delta / 10
		last_positional_reward = pr

	adj_positional_reward_delta()
	
	reward_adj = eval('adj_positional_reward_delta')

	reward_adj()


run()
