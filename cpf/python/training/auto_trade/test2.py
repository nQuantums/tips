import trade_environment
from trade_environment import TradeEnvironment

env = TradeEnvironment('test.dat', 240, 100)
while True:
	env.reset()
	terminal = False

	print(f'Episode Start')
	step = 0

	while not terminal:
		next_state, reward_org, terminal, info = env.step(0)
		env.render()
		print(f'Step {step}')
		step += 1
