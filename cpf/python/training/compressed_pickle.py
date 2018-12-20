import pickle
import bz2

def loads(comp):
	return pickle.loads(bz2.decompress(comp))

def dumps(obj):
	return bz2.compress(pickle.dumps(obj))

def load(fname):
	with bz2.BZ2File(fname, 'rb') as fin:
		return pickle.load(fin)

def dump(obj, fname, level=9):
	with bz2.BZ2File(fname, 'wb', compresslevel=level) as fout:
		pickle.dump(obj, fout)
