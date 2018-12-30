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
with psycopg2.connect(dbp['connection_string']) as conn:
	conn.autocommit = True
	with conn.cursor() as cur:
		l = tables.LearnerData('ld')
		a = tables.ActorData('ad')

		s = db.select([l.train_num, a.ep_reward]).\
			frm(l).\
			join(a).on(f'{l.train_num}={a.train_num}').\
			where(f'{l.param_set_id}=2 AND {a.actor_id}=2').\
			order_by([l.train_num, l.timestamp, a.timestamp])
		df1 = pd.read_sql(s.sql(), conn)

		s = db.select([l.train_num, a.ep_reward]).\
			frm(l).\
			join(a).on(f'{l.train_num}={a.train_num}').\
			where(f'{l.param_set_id}=2 AND {a.actor_id}=3').\
			order_by([l.train_num, l.timestamp, a.timestamp])
		df2 = pd.read_sql(s.sql(), conn)

		plt.figure()

		ax = df1.plot(x='train_num')
		df2.plot(x='train_num', ax=ax)

		# print(df)
		plt.show()
		plt.savefig("image2.png")

		# fig1, ax1 = plt.subplots()
		# plt.savefig('graph.png')
		# for r in nodes.read(cur):
		# 	print(r)
