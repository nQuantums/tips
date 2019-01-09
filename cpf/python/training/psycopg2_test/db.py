import datetime
from collections import namedtuple
import psycopg2


class Type:

	def __init__(self, type_name, is_serial=False):
		self.type_name = type_name
		self.is_serial = is_serial

	def __str__(self):
		return self.type_name

	def __repr__(self):
		return self.type_name


class Col:

	def __init__(self, name, type):
		self.name = name
		self.type = type

	def __str__(self):
		return f'{self.name} {self.type.type_name}'

	def __repr__(self):
		return f'{self.name} {self.type.type_name}'


class Idx:

	def __init__(self, tbl, cols):
		self.tbl = tbl
		self.cols = cols
		self.name = f'idx_{tbl._name}_{"_".join([c.name for c in cols])}'

	def get_create_statement(self):
		return f'CREATE INDEX IF NOT EXISTS {self.name} ON {self.tbl._name}({", ".join([c.name for c in self.cols])});'


class Tbl:

	def __init__(self, name):
		self._name = name

	def __setattr__(self, name, value):
		d = self.__dict__
		if not name.startswith('_') and isinstance(value, Type):
			if '_cols' not in d:
				d['_cols'] = []
			value = Col(name, value)
			d['_cols'].append(value)
		d[name] = value

	def get_primary_key_cols(self):
		if not hasattr(self, '_primary_key_cols'):
			self._primary_key_cols = []
		return self._primary_key_cols

	def get_indices(self):
		if not hasattr(self, '_indices'):
			self._indices = []
		return self._indices

	def pk(self, cols):
		self.get_primary_key_cols().extend(cols)

	def idx(self, cols):
		self.get_indices().append(Idx(self, cols))

	def get_cols(self, filter=None):
		d = self.__dict__
		if '_cols' in d:
			cols = d['_cols']
		else:
			cols = []
			d['_cols'] = cols
		if filter:
			return [c for c in cols if filter(c)]
		else:
			return cols

	def get_serial_cols(self):
		return self.get_cols(lambda c: c.type.is_serial)

	def get_record_type(self, filter=None):
		return namedtuple(f'{self._name}_record', [c.name for c in self.get_cols(filter)])

	def get_create_statement(self):
		col_defs = [f'{c.name} {c.type.type_name}' for c in self.get_cols()]
		col_defs = ','.join(col_defs)
		pk_defs = [c.name for c in self.get_primary_key_cols()]
		if len(pk_defs) != 0:
			pk_defs = f',PRIMARY KEY({",".join(pk_defs)})'
		else:
			pk_defs = ''
		if len(self.get_indices()) == 0:
			create_index_statement = ''
		else:
			create_index_statement = ';'.join([idx.get_create_statement() for idx in self.get_indices()]) + ';'
		return f'CREATE TABLE IF NOT EXISTS {self._name} ({col_defs}{pk_defs});{create_index_statement}'

	def get_insert(self, filter=None):
		cols = [c.name for c in self.get_cols(filter)]
		colps = ','.join(['%s' for _ in cols])
		cols = ','.join(cols)
		sql = f'INSERT INTO {self._name}({cols}) VALUES({colps});'

		def insert(cursor, record):
			cursor.execute(sql, record)

		return insert

	def get_inserts(self, filter=None):
		cols = [c.name for c in self.get_cols(filter)]
		cols = ','.join(cols)
		sql = f'INSERT INTO {self._name}({cols}) VALUES'

		def insert(cursor, records):
			values = []
			params = []
			for r in records:
				values.append(f'({",".join(["%s" for _ in r])})')
				params.extend(r)
			values = ",".join(values)
			cursor.execute(sql + values, params)

		return insert

	def get_is_exists(self, filter=None):
		condition = ' AND '.join([f'{c.name}=%s' for c in self.get_cols(filter)])
		sql = f'SELECT 1 FROM {self._name} WHERE {condition};'

		def is_exists(cursor, record):
			cursor.execute(sql, record)
			exists = False
			for _ in cursor:
				exists = True
			return exists

		return is_exists

	def get_find(self, select_cols, filter=None):
		return_record_type = namedtuple(f'{self._name}_found_record', [c.name for c in select_cols])
		select_col_names = ','.join([c.name for c in select_cols])
		condition = ' AND '.join([f'{c.name}=%s' for c in self.get_cols(filter)])
		sql = f'SELECT {select_col_names} FROM {self._name} WHERE {condition} LIMIT 1;'

		def find(cursor, record):
			cursor.execute(sql, record)
			found_record = None
			for r in cursor:
				found_record = return_record_type(*r)
			return found_record

		return find


serial32 = Type('serial', True)
serial64 = Type('bigserial', True)
int16 = Type('smallint')
int32 = Type('integer')
int64 = Type('bigint')
float32 = Type('real')
float64 = Type('double precision')
text = Type('text')
timestamp = Type('timestamp')
array_serial32 = Type('serial[]')
array_serial64 = Type('bigserial[]')
array_int16 = Type('smallint[]')
array_int32 = Type('integer[]')
array_int64 = Type('bigint[]')
array_float32 = Type('real[]')
array_float64 = Type('double precision[]')
array_text = Type('text[]')
array_timestamp = Type('timestamp[]')
