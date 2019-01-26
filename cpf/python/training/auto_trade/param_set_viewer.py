import sys
import datetime
import json
import numpy as np
import pandas as pd
import matplotlib.pyplot as plt
import psycopg2

import parameters

params = parameters.load()

dbp = params['db']
ap = params['actor']
conn = psycopg2.connect(dbp['connection_string'])
conn.autocommit = True
cur = conn.cursor()

if __name__ == "__main__":
	df = pd.read_sql(f'SELECT * FROM param_set', conn)
	print(df)
