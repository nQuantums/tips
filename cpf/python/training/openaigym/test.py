import random
import gym
from gym import envs
import cv2

all_envs = envs.registry.all()
env_ids = [env_spec.id for env_spec in all_envs]
for env_id in env_ids:
	print(env_id)

env = gym.make('MsPacman-v4')
num_actions = env.action_space.n

env.reset()
while True:
	observation, reward, done, info = env.step(random.randrange(0, num_actions))
	print(observation.shape)
	cv2.imshow('observation', observation)
	cv2.waitKey(1)
	env.render()
	if done:
		env.reset()

