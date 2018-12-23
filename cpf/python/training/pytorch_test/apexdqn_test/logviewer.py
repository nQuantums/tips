import datetime
import json
import pandas as pd
import matplotlib.pyplot as plt
import psycopg2

import db
import tables

with open('parameters.json', 'r') as f:
	params = json.load(f)

db_conf = params['db']
connection_string = f'dbname={db_conf["dbname"]} user={db_conf["user"]}'
with psycopg2.connect(connection_string) as conn:
	conn.autocommit = True
	with conn.cursor() as cur:
		l = tables.LearnerData('ld')
		a = tables.ActorData('ad')

		s = db.select([l.train_num, l.loss, a.ep_reward]).\
			frm(l).\
			join(a).on(f'{l.train_num}={a.train_num}').\
			where(f'{l.param_set_id}=0 {a.actor_id}=0').\
			order_by([l.train_num, l.timestamp, a.timestamp])

		df = pd.read_sql(s.sql(), conn)
		plt.figure()
		df.plot(x='train_num')
		# print(df)
		plt.show()
		plt.savefig("image2.png")

		# fig1, ax1 = plt.subplots()
		# plt.savefig('graph.png')
		# for r in nodes.read(cur):
		# 	print(r)
