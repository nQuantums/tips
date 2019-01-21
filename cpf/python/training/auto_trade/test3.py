import typing
import numpy as np
import matplotlib.pyplot as plt
import trade_environment
import action_suggester

env = trade_environment.TradeEnvironment('test.dat', 30, (100, 110))
act_sug = action_suggester.TpRewardAdjuster(env)

env.reset(False)
act_sug.start_episode()

values = env.episode_values
c = values[:, 3]

plt.style.use('seaborn-whitegrid')

fig = plt.figure()
ax = fig.add_subplot(1, 1, 1)
x = np.arange(len(c))

ax.plot(x, c, label='close')
ax.plot(x[act_sug.tp_indices], c[act_sug.tp_indices], label='tp', marker='o')

plt.legend()
plt.show()
