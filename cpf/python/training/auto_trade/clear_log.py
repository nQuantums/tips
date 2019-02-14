import json
import psycopg2

import tables

with open('parameters.json', 'r') as f:
	params = json.load(f)

dbp = params['db']
with psycopg2.connect(dbp['connection_string']) as conn:
	conn.autocommit = True
	with conn.cursor() as cur:
		ps = tables.ParamSet()
		ad = tables.ActorData()
		ld = tables.LearnerData()
		rad = tables.RewardAdjData()
		cur.execute(ps.get_drop_statement())
		cur.execute(ad.get_drop_statement())
		cur.execute(ld.get_drop_statement())
		cur.execute(rad.get_drop_statement())
