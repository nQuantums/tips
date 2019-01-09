import datetime
from collections import deque
import json
import psycopg2
import db


class ParamSet(db.Tbl):

	def __init__(self):
		super().__init__('param_set')
		self.param_set_id = db.serial64
		self.data_file_name = db.text
		self.state_shape = db.array_int32
		self.action_dim = db.int64
		self.env_name = db.text
		self.num_actors = db.int64
		self.num_steps = db.int64
		self.epsilon = db.float64
		self.alpha = db.float64
		self.gamma = db.float64
		self.q_target_sync_freq = db.int64
		self.min_replay_mem_size = db.int64
		self.replay_sample_size = db.int64
		self.soft_capacity = db.int64
		self.priority_exponent = db.float64
		self.importance_sampling_exponent = db.float64

		self.pk(self.get_cols())


class ActorData(db.Tbl):

	def __init__(self):
		super().__init__('actor_data')
		self.param_set_id = db.int16
		self.actor_id = db.int16
		self.timestamp = db.timestamp
		self.step_num = db.int32
		self.ep_len = db.int32
		self.ep_reward = db.float32
		self.max_q = db.float32
		self.action = db.int16

		self.idx([self.param_set_id])
		self.idx([self.actor_id])
		self.idx([self.timestamp])
		self.idx([self.param_set_id, self.timestamp])

class LearnerData(db.Tbl):

	def __init__(self):
		super().__init__('actor_data')
		self.param_set_id = db.int16
		self.timestamp = db.timestamp
		self.sum_step_num = db.int32
		self.step_num = db.int32
		self.loss = db.float32
		self.target_sync_num = db.int32
		self.send_param_num = db.int32

		self.idx([self.param_set_id])
		self.idx([self.sum_step_num])
		self.idx([self.timestamp])
		self.idx([self.param_set_id, self.timestamp])


with open('params.json', 'r') as f:
	params = json.load(f)

env = params['Env']
actor = params['Actor']
learner = params['Learner']
replay_memory = params['Replay_Memory']

with psycopg2.connect("dbname=apexdqn user=postgres") as conn:
	conn.autocommit = True
	with conn.cursor() as cur:
		ps = ParamSet()
		ad = ActorData()
		cur.execute(ps.get_create_statement())
		cur.execute(ad.get_create_statement())
		filter = lambda c: not c.type.is_serial
		record = ps.get_record_type(filter)
		insert = ps.get_insert(filter)
		is_exists = ps.get_is_exists(filter)
		find = ps.get_find([ps.param_set_id], filter)
		actor_record = ad.get_record_type()
		actor_inserts = ad.get_inserts()

		r = record('model21.pt', env['state_shape'], env['action_dim'], env['name'], actor['num_actors'], actor['num_steps'], actor['epsilon'], actor['alpha'], actor['gamma'], learner['q_target_sync_freq'], learner['min_replay_mem_size'], learner['replay_sample_size'], replay_memory['soft_capacity'], replay_memory['priority_exponent'], replay_memory['importance_sampling_exponent'])

		if not is_exists(cur, r):
			insert(cur, r)
		found_r = find(cur, r)
		print(found_r)

		q = deque()

		for i in range(1000000):
			q.append(actor_record(0, 1, datetime.datetime.now(), 1, i, i, 2))
			if 10 <= len(q):
				actor_inserts(cur, q)
				q.clear()

		# cur.execute("CREATE TABLE test (id serial PRIMARY KEY, num integer, data varchar);")
		# cur.execute("INSERT INTO test (num, data) VALUES (%s, %s)",(100, "abc'def"))

# 		cur.execute('''
# SELECT count(*) FROM param_set
# WHERE
# 	data_file_name=%s
# 	AND state_shape=%s
# 	AND action_dim=%s
# 	AND env_name=%s
# 	AND num_actors=%s
# 	AND num_steps=%s
# 	AND epsilon=%s
# 	AND alpha=%s
# 	AND gamma=%s
# 	AND q_target_sync_freq=%s
# 	AND min_replay_mem_size=%s
# 	AND replay_sample_size=%s
# 	AND soft_capacity=%s
# 	AND priority_exponent=%s
# 	AND importance_sampling_exponent=%s
# ;''',
# 			('model.pt',
# 			env['state_shape'],
# 			env['action_dim'],
# 			env['name'],
# 			actor['num_actors'],
# 			actor['num_steps'],
# 			actor['epsilon'],
# 			actor['alpha'],
# 			actor['gamma'],
# 			learner['q_target_sync_freq'],
# 			learner['min_replay_mem_size'],
# 			learner['replay_sample_size'],
# 			replay_memory['soft_capacity'],
# 			replay_memory['priority_exponent'],
# 			replay_memory['importance_sampling_exponent']))
# 		for r in cur:
# 			print(r)
# 		# cur.fetchone()
# 		# conn.commit()
