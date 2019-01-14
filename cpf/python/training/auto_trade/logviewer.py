import sys
import datetime
import json
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
		df = []

		param_set_id = int(sys.argv[1])
		min_actor_id = int(sys.argv[2]) if 2 < len(sys.argv) else 0
		max_actor_id = int(sys.argv[3]) if 3 < len(sys.argv) else 7
		min_train_num = int(sys.argv[4]) if 4 < len(sys.argv) else 0

		param_set = pd.read_sql(f'SELECT * FROM param_set WHERE param_set_id={param_set_id}', conn)
		title = f"{param_set['model'][0]} hidden_size: {param_set['hidden_size'][0]} {param_set['policy'][0]} {param_set['reward_adj'][0]}"

		fig = plt.figure()
		fig.suptitle(title, fontsize=16)
		ax_action = fig.add_subplot(3, 1, 1)
		ax_action.set_title('action=q_action')
		ax_q_action = fig.add_subplot(3, 1, 2)
		ax_q_action.set_title('action<>q_action')
		ax_sum = fig.add_subplot(3, 1, 3)
		ax_sum.set_title('sum reward')

		for i in range(min_actor_id, max_actor_id + 1):
			df = pd.read_sql(
			    f'SELECT train_num, reward FROM actor_data WHERE param_set_id={param_set_id} AND actor_id={i} AND {min_train_num}<=train_num AND action=q_action ORDER BY train_num',
			    conn)
			if df.shape[0] != 0:
				df.plot(x='train_num', ax=ax_action)

			df = pd.read_sql(
			    f'SELECT train_num, reward FROM actor_data WHERE param_set_id={param_set_id} AND actor_id={i} AND {min_train_num}<=train_num AND action<>q_action ORDER BY train_num',
			    conn)
			if df.shape[0] != 0:
				df.plot(x='train_num', ax=ax_q_action)

			df = pd.read_sql(
			    f'SELECT train_num, sum_reward FROM actor_data WHERE param_set_id={param_set_id} AND actor_id={i} AND {min_train_num}<=train_num ORDER BY train_num',
			    conn)
			if df.shape[0] != 0:
				df.plot(x='train_num', ax=ax_sum)

		# print(df)
		plt.show()
		plt.savefig("image2.png")

		# fig1, ax1 = plt.subplots()
		# plt.savefig('graph.png')
		# for r in nodes.read(cur):
		# 	print(r)
