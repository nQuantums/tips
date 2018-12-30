import datetime
from collections import deque
import json
import psycopg2
import db
import tables

with psycopg2.connect("host=192.168.1.2 dbname=apexdqn user=postgres password=Passw0rd!") as conn:
	conn.autocommit = True
	with conn.cursor() as cur:
		ad = tables.ActorData()
