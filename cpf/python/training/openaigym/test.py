import random
import matplotlib.pyplot as plt
import gym
from gym import envs
import cv2
from skimage.color import rgb2gray
from skimage.transform import resize

all_envs = envs.registry.all()
env_ids = [env_spec.id for env_spec in all_envs]
for env_id in env_ids:
	print(env_id)

env = gym.make('MsPacman-v4')
num_actions = env.action_space.n

figx_r, ax_r = plt.subplots()
figx_g, ax_g = plt.subplots()
figx_b, ax_b = plt.subplots()

env.reset()
while True:
	observation, reward, done, info = env.step(random.randrange(0, num_actions))

	gray = cv2.cvtColor(observation, cv2.COLOR_BGR2GRAY)
	cv2.imshow('gray', gray)
	cv2.waitKey(100)

	print(reward)

	# chs = observation.transpose(2, 0, 1)
	# cv2.imshow('r', chs[0])
	# cv2.imshow('g', chs[1])
	# cv2.imshow('b', chs[2])
	# cv2.waitKey(1)

	# print(observation.shape)
	# cv2.imshow('observation', observation)
	# cv2.waitKey(1)
	# env.render()
	if done:
		env.reset()