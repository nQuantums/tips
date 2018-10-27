import time
import concurrent.futures


def func1():
	while True:
		print("func1")
		time.sleep(1)


def func2():
	while True:
		print("func2")
		time.sleep(1)


if __name__ == "__main__":
	executor = concurrent.futures.ThreadPoolExecutor(max_workers=2)
	executor.submit(func1)
	executor.submit(func2)
