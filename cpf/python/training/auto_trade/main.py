#!/usr/bin/env python
import time
import multiprocessing as mp
import json
from argparse import ArgumentParser
import psycopg2

import db_initializer
from replay import ReplayMemory
from actor import Actor
from learner import Learner

arg_parser = ArgumentParser(prog="main.py")
arg_parser.add_argument(
    "--params-file",
    default="parameters.json",
    type=str,
    help="Path to json file defining the parameters for the Actor, Learner and Replay memory",
    metavar="PARAMSFILE")
args = arg_parser.parse_args()


def learner(params, param_set_id, status_dict, shared_state, shared_mem):
	learner = Learner(params, param_set_id, status_dict, shared_state, shared_mem)
	learner.learn()


def actor(params, param_set_id, i, status_dict, shared_state, shared_mem):
	actor = Actor(params, param_set_id, i, status_dict, shared_state, shared_mem)
	actor.run()


if __name__ == "__main__":
	with open(args.params_file, 'r') as f:
		params = json.load(f)

	param_set_id = db_initializer.initialize(params)

	mp_manager = mp.Manager()
	status_dict = mp_manager.dict()
	shared_state = mp_manager.dict()
	shared_mem = mp_manager.Queue()

	status_dict['quit'] = False
	status_dict['Q_state_dict_stored'] = False
	status_dict['request_quit'] = False

	# A learner is started before the Actors so that the shared_state is populated with a Q_state_dict
	learner_proc = mp.Process(target=learner, args=(params, param_set_id, status_dict, shared_state, shared_mem))
	learner_proc.start()
	while not status_dict['Q_state_dict_stored']:
		time.sleep(0.001)

	#  TODO: Test with multiple actors
	actor_procs = []
	for i in range(params["actor"]["num_actors"]):
		p = mp.Process(target=actor, args=(params, param_set_id, i, status_dict, shared_state, shared_mem))
		p.start()
		actor_procs.append(p)

	[actor_proc.join() for actor_proc in actor_procs]
	print('actor all join')
	status_dict['quit'] = True
	learner_proc.join()
	print("Main: replay_mem.size:", shared_mem.qsize())
