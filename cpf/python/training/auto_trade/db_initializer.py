import psycopg2
import tables

def get_state_dict_name(params):
	ep = params['env']
	ap = params["actor"]
	lp = params["learner"]
	fhw = ep['frames_height_width']
	return f'{lp["state_dict_prefix"]}.{lp["model"]}.{fhw[0]}_{fhw[1]}_{fhw[2]}.{lp["hidden_size"]}.{lp["optimizer"]}.{ap["action_suggester"]}.{ap["reward_adjuster"]}.{ap["policy"]}.pt'

def initialize(params):
	db_conf = params['db']
	ep = params['env']
	ap = params["actor"]
	lp = params["learner"]
	rp = params["replay_memory"]

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

			r = record(lp['model'], lp['hidden_size'], lp['optimizer'], lp['state_dict_prefix'], ep['window_size'],
			           ep['frames_height_width'], ap['num_actors'], ap['num_steps'], ap['epsilon'], ap['alpha'], ap['gamma'],
			           lp['q_target_sync_freq'], lp['min_replay_mem_size'], lp['replay_sample_size'],
			           rp['soft_capacity'], rp['priority_exponent'],
			           rp['importance_sampling_exponent'], ap['action_suggester'], ap['reward_adjuster'], ap['policy'])

			found = find(cur, r)
			if found is None:
				insert(cur, r)
			found = find(cur, r)

			return found[0]

def override_parameters_from_db(params, param_set_id):
	db_conf = params['db']
	ep = params['env']
	ap = params["actor"]
	lp = params["learner"]
	rp = params["replay_memory"]

	with psycopg2.connect(db_conf["connection_string"]) as conn:
		conn.autocommit = True
		with conn.cursor() as cur:
			ps = tables.ParamSet()

			record = ps.get_record_type()

			cur.execute('SELECT * FROM param_set WHERE param_set_id=%s', [param_set_id])
			for r in cur:
				ps = record(*r)
				lp['model'] = ps.model
				lp['hidden_size'] = ps.hidden_size
				lp['optimizer'] = ps.optimizer
				lp['state_dict_prefix'] = ps.state_dict_prefix
				ep['window_size'] = ps.window_size
				ep['frames_height_width'] = ps.frames_height_width
				ap['num_actors'] = ps.num_actors
				ap['num_steps'] = ps.num_steps
				ap['epsilon'] = ps.epsilon
				ap['alpha'] = ps.alpha
				ap['gamma'] = ps.gamma
				lp['q_target_sync_freq'] = ps.q_target_sync_freq
				lp['min_replay_mem_size'] = ps.min_replay_mem_size
				lp['replay_sample_size'] = ps.replay_sample_size
				rp['soft_capacity'] = ps.soft_capacity
				rp['priority_exponent'] = ps.priority_exponent
				rp['importance_sampling_exponent'] = ps.importance_sampling_exponent
				ap['action_suggester'] = ps.action_suggester
				ap['reward_adjuster'] = ps.reward_adjuster
				ap['policy'] = ps.policy
