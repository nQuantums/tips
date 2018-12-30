import psycopg2
import tables

def initialize(params):
	db_conf = params['db']
	env_conf = params['env']
	actor_params = params["actor"]
	learner_params = params["learner"]
	replay_params = params["replay_memory"]

	with psycopg2.connect(db_conf["connection_string"]) as conn:
		conn.autocommit = True
		with conn.cursor() as cur:
			ps = tables.ParamSet()
			ad = tables.ActorData()
			ld = tables.LearnerData()
			cur.execute(ps.get_create_statement())
			cur.execute(ad.get_create_statement())
			cur.execute(ld.get_create_statement())

			filter = lambda c: not c.type.is_serial
			record = ps.get_record_type(filter)
			insert = ps.get_insert(filter)
			find = ps.get_find([ps.param_set_id], filter)

			r = record(learner_params['model'], learner_params['optimizer'], learner_params['load_saved_state'],
			           env_conf['state_shape'], env_conf['action_dim'], env_conf['name'], actor_params['num_actors'],
			           actor_params['num_steps'], actor_params['epsilon'], actor_params['alpha'], actor_params['gamma'],
			           learner_params['q_target_sync_freq'], learner_params['min_replay_mem_size'],
			           learner_params['replay_sample_size'], replay_params['soft_capacity'], replay_params['priority_exponent'],
			           replay_params['importance_sampling_exponent'])

			found = find(cur, r)
			if found is None:
				insert(cur, r)
			found = find(cur, r)

			return found[0]
