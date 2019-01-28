import sys
import datetime
import json
import numpy as np
import pandas as pd
import matplotlib.pyplot as plt
import psycopg2

with open('parameters.json', 'r') as f:
	params = json.load(f)

dbp = params['db']
ap = params['actor']
with psycopg2.connect(dbp['connection_string']) as conn:
	conn.autocommit = True
	with conn.cursor() as cur:
		ax = None

		param_set_id = int(sys.argv[1])
		min_actor_id = int(sys.argv[2]) if 2 < len(sys.argv) else 0
		max_actor_id = int(sys.argv[3]) if 3 < len(sys.argv) else 7
		min_train_num = sys.argv[4] if 4 < len(sys.argv) else 0
		index = sys.argv[5] if 5 < len(sys.argv) else 'train_num'

		ps = pd.read_sql(f'SELECT * FROM param_set WHERE param_set_id={param_set_id}', conn)
		title = f"{ps['state_dict_prefix'][0]} {ps['model'][0]} hidden_size: {ps['hidden_size'][0]} {ps['action_suggester'][0]} {ps['reward_adjuster'][0]} {ps['policy'][0]}"

		plt.style.use('seaborn-whitegrid')

		fig = plt.figure()
		fig.suptitle(title, fontsize=12)
		ax_action = fig.add_subplot(4, 1, 1)
		ax_action.set_title('Reward Q action')
		ax_q_action = fig.add_subplot(4, 1, 2)
		ax_q_action.set_title('Reward random action')
		ax_sum = fig.add_subplot(4, 1, 3)
		ax_sum.set_title('Reward sum')
		ax_q_action_number = fig.add_subplot(4, 1, 4)
		ax_q_action_number.set_title('Q action')

		for i in range(min_actor_id, max_actor_id + 1):
			cond = f'param_set_id={param_set_id} AND actor_id={i} AND {min_train_num}<={index}'

			df = pd.read_sql(f'SELECT sum(reward) FROM actor_data WHERE {cond} AND action=q_action', conn)
			if df.shape[0] != 0:
				ax_action.set_title(f'Reward Q action : sum={df["sum"][0]}')

			df = pd.read_sql(f'SELECT {index}, reward FROM actor_data WHERE {cond} AND action=q_action ORDER BY {index}', conn)
			if df.shape[0] != 0:
				df['reward'] = np.cumsum(df['reward'].values)
				df.plot(x=index, ax=ax_action)

			df = pd.read_sql(f'SELECT sum(reward) FROM actor_data WHERE {cond} AND action<>q_action', conn)
			if df.shape[0] != 0:
				ax_q_action.set_title(f'Reward Q action : sum={df["sum"][0]}')

			df = pd.read_sql(f'SELECT {index}, reward FROM actor_data WHERE {cond} AND action<>q_action ORDER BY {index}', conn)
			if df.shape[0] != 0:
				df['reward'] = np.cumsum(df['reward'].values)
				df.plot(x=index, ax=ax_q_action)

			df = pd.read_sql(f'SELECT {index}, sum_reward FROM actor_data WHERE {cond} ORDER BY {index}', conn)
			if df.shape[0] != 0:
				df.plot(x=index, ax=ax_sum)

			df = pd.read_sql(f'SELECT {index}, action FROM actor_data WHERE {cond} AND action=q_action ORDER BY {index}', conn)
			if df.shape[0] != 0:
				df.plot(x=index, ax=ax_q_action_number, linestyle='None', marker='.')
			df = pd.read_sql(f'SELECT {index}, action FROM actor_data WHERE {cond} AND action<>q_action ORDER BY {index}', conn)
			if df.shape[0] != 0:
				df.plot(x=index, ax=ax_q_action_number, linestyle='None', marker='.')

		# print(df)
		plt.show()
		plt.savefig("image2.png")

		# fig1, ax1 = plt.subplots()
		# plt.savefig('graph.png')
		# for r in nodes.read(cur):
		# 	print(r)
