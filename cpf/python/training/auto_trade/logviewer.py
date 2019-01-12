import datetime
import json
import pandas as pd
import matplotlib.pyplot as plt
import psycopg2

import db
import tables

with open('parameters.json', 'r') as f:
	params = json.load(f)

dbp = params['db']
ap = params['actor']
with psycopg2.connect(dbp['connection_string']) as conn:
	conn.autocommit = True
	with conn.cursor() as cur:
		l = tables.LearnerData('ld')
		a = tables.ActorData('ad')

		ax = None
		df = []

		param_sets = pd.read_sql('select * from param_set', conn)
		print(param_sets)

		for i in range(ap['num_actors']):
			s = db.select([l.train_num, a.sum_reward]).\
				frm(l).\
				join(a).on(f'{l.train_num}={a.train_num}').\
				where(f'{l.param_set_id}=1 AND {a.actor_id}={i}').\
				order_by([l.train_num, l.timestamp, a.timestamp])
			df = pd.read_sql(s.sql(), conn)
			if ax is None:
				ax = df.plot(x='train_num')
			else:
				df.plot(x='train_num', ax=ax)

		# print(df)
		plt.show()
		plt.savefig("image2.png")

		# fig1, ax1 = plt.subplots()
		# plt.savefig('graph.png')
		# for r in nodes.read(cur):
		# 	print(r)
