#!/usr/bin/env python
import time
import multiprocessing as mp
import json
from replay import ReplayMemory
from actor import Actor
from learner import Learner
from argparse import ArgumentParser

arg_parser = ArgumentParser(prog="main.py")
arg_parser.add_argument("--params-file", default="parameters.json", type=str, help="Path to json file defining the parameters for the Actor, Learner and Replay memory", metavar="PARAMSFILE")
args = arg_parser.parse_args()


def learner(env_conf, replay_params, status_dict, learner_params, shared_state, shared_mem):
	learner = Learner(env_conf, replay_params, status_dict, learner_params, shared_state, shared_mem)
	learner.learn()


def actor(i, env_conf, shared_state, shared_mem, actor_params):
	actor = Actor(i, env_conf, shared_state, shared_mem, actor_params)
	actor.run()


if __name__ == "__main__":
	params = json.load(open(args.params_file, 'r'))
	env_conf = params['env_conf']
	actor_params = params["Actor"]
	learner_params = params["Learner"]
	replay_params = params["Replay_Memory"]
	print("Using the params:\n env_conf:{} \n actor_params:{} \n learner_params:{} \n, replay_params:{}".format(env_conf, actor_params, learner_params, replay_params))

	mp_manager = mp.Manager()
	status_dict = mp_manager.dict()
	shared_state = mp_manager.dict()
	shared_mem = mp_manager.Queue()

	status_dict['quit'] = False

	l = None

	# A learner is started before the Actors so that the shared_state is populated with a Q_state_dict
	learner_proc = mp.Process(target=learner, args=(env_conf, replay_params, status_dict, learner_params, shared_state, shared_mem))
	learner_proc.start()
	time.sleep(1)

	#  TODO: Test with multiple actors
	actor_procs = []
	for i in range(actor_params["num_actors"]):
		p = mp.Process(target=actor, args=(i, env_conf, shared_state, shared_mem, actor_params))
		p.start()
		actor_procs.append(p)

	if l:
		l.learn()

	[actor_proc.join() for actor_proc in actor_procs]
	print('actor all join')
	status_dict['quit'] = True
	if not l:
		learner_proc.join()

	print("Main: replay_mem.size:", shared_mem.qsize())
