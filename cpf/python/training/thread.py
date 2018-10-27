from logging import (getLogger, StreamHandler, INFO, Formatter)
from threading import (Event, Thread)
import time

# ログの設定
handler = StreamHandler()
handler.setLevel(INFO)
handler.setFormatter(Formatter("[%(asctime)s] [%(threadName)s] %(message)s"))
logger = getLogger()
logger.addHandler(handler)
logger.setLevel(INFO)

event = Event()

def event_example1():
	logger.info("スレッド開始")
	event.wait()
	logger.info("スレッド終了")


thread = Thread(target=event_example1)
thread.start()
time.sleep(3)
logger.info("イベント発生")
event.set()
