import os
import multiprocessing as mp
import numpy as np
import time


def func1(a, b, c, q):
	print(a, b, c, q)

if __name__ == '__main__':
	t = np.arange(32, dtype=np.float32)
	q = mp.Queue(100)
	p = mp.Process(target=func1, args=(1, '2', t, q))
	p.start()
	p.join()
