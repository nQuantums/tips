import db


class ParamSet(db.Tbl):

	def __init__(self):
		super().__init__('param_set')
		self.param_set_id = db.serial64
		self.model = db.text
		self.optimizer = db.text
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

	def __init__(self, alias=None):
		super().__init__('actor_data', alias)
		self.param_set_id = db.int16
		self.actor_id = db.int16
		self.timestamp = db.timestamp
		self.train_num = db.int32
		self.ep_len = db.int32
		self.ep_reward = db.float32
		self.q = db.array_float32
		self.action = db.int16
		self.priorities = db.array_float32

		self.idx([self.param_set_id])
		self.idx([self.actor_id])
		self.idx([self.timestamp])
		self.idx([self.train_num])
		self.idx([self.param_set_id, self.timestamp])


class LearnerData(db.Tbl):

	def __init__(self, alias=None):
		super().__init__('learner_data', alias)
		self.param_set_id = db.int16
		self.timestamp = db.timestamp
		self.train_num = db.int32
		self.step_num = db.int32
		self.loss = db.float32
		self.q = db.array_float32
		self.before_priorities = db.array_float32
		self.after_priorities = db.array_float32
		self.indices = db.array_int32
		self.target_sync_num = db.int32
		self.send_param_num = db.int32

		self.idx([self.param_set_id])
		self.idx([self.train_num])
		self.idx([self.timestamp])
		self.idx([self.param_set_id, self.timestamp])
