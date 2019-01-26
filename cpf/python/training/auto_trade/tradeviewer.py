import sys
import datetime
import json
import numpy as np
import pandas as pd
import matplotlib.pyplot as plt
import psycopg2

import parameters
import trade_environment
import action_suggester

params = parameters.load()
dbp = params['db']
ep = params['env']
ap = params['actor']
conn = psycopg2.connect(dbp['connection_string'])
conn.autocommit = True
cur = conn.cursor()

if __name__ == "__main__":
	if sys.argv[1] == 'latest':
		df = pd.read_sql(f'SELECT max(param_set_id) FROM param_set', conn)
		param_set_id = int(df['max'][0])
	else:
		param_set_id = int(sys.argv[1])
	print(f'param_set_id: {param_set_id}')

	actor_id = int(sys.argv[2]) if 2 < len(sys.argv) else 0
	print(f'actor_id: {actor_id}')

	if sys.argv[3] == 'latest':
		df = pd.read_sql(f'SELECT max(ep_count) FROM actor_data WHERE param_set_id={param_set_id} AND actor_id={actor_id}',
		                 conn)
		ep_count = int(df['max'][0])
	else:
		ep_count = int(sys.argv[3])
	print(f'ep_count: {ep_count}')

	df = pd.read_sql(
	    f'SELECT episode_index FROM actor_data WHERE param_set_id={param_set_id} AND actor_id={actor_id} AND ep_count={ep_count} LIMIT 1',
	    conn)
	episode_index = int(df['episode_index'][0])
	print(f'episode_index: {episode_index}')

	# param_set_id = 1
	# actor_id = 7
	# ep_count = 1
	# episode_index = 145

	ps = pd.read_sql(f'SELECT * FROM param_set WHERE param_set_id={param_set_id}', conn)
	title = f"{ps['state_dict_prefix'][0]} {ps['model'][0]} hidden_size: {ps['hidden_size'][0]} {ps['action_suggester'][0]} {ps['reward_adjuster'][0]} {ps['policy'][0]}"

	plt.style.use('seaborn-whitegrid')

	fig = plt.figure()
	fig.suptitle(title, fontsize=12)
	ax = fig.add_subplot(1, 1, 1)

	df = pd.read_sql(
	    f'SELECT index_in_episode, action, q_action, position_index_in_episode, position_action, position_q_action, reward FROM actor_data WHERE param_set_id={param_set_id} AND actor_id={actor_id} AND ep_count={ep_count}',
	    conn)

	data_cur = df['index_in_episode'].values, df['action'].values, df['q_action'].values
	data_entry = df['position_index_in_episode'].values, df['position_action'].values, df['position_q_action'].values
	reward = df['reward'].values
	print(df)

	env = trade_environment.TradeEnvironment('test.dat', ep['window_size'], ep['frames_height_width'][1:])
	suggester = action_suggester.TpActionSuggester(env)

	env.reset(episode_index)
	suggester.start_episode()

	values = env.episode_values
	o = values[:, 0]
	h = values[:, 1]
	l = values[:, 2]
	c = values[:, 3]
	x = np.arange(len(c))
	ax.plot(x, c, label='close')
	ax.plot(x[suggester.tp_indices], c[suggester.tp_indices], label='tp')

	def plot(data, label, color_eq, color_neq):
		i = data[0]
		a = data[1]
		q = data[2]
		eq = np.nonzero(a == q)[0]
		neq = np.nonzero(a != q)[0]

		def plot1(ci, label_q, color):
			e = a[ci]
			buy_indices = i[ci[np.nonzero(e == 1)[0]]]
			sell_indices = i[ci[np.nonzero(e == 2)[0]]]
			exit_indices = i[ci[np.nonzero(e == 3)[0]]]
			ax.plot(x[buy_indices], c[buy_indices], label=f'{label} {label_q} buy', color=color, linestyle='None', marker='^')
			ax.plot(x[sell_indices], c[sell_indices], label=f'{label} {label_q} sell', color=color, linestyle='None', marker='v')
			ax.plot(x[exit_indices], c[exit_indices], label=f'{label} {label_q} exit', color=color, linestyle='None', marker='.')

		plot1(eq, 'Q Action', color_eq)
		plot1(neq, 'S Action', color_neq)

	plot(data_cur, 'Cur', '#3355aa', '#33aa55')
	plot(data_entry, 'Entry', '#3355ff', '#33ff55')

	reward_indices = data_cur[0][np.nonzero(reward)[0]]
	ax.plot(x[reward_indices], c[reward_indices] + reward, label='reward', linestyle='None', marker='x')

	plt.legend()
	plt.show()
