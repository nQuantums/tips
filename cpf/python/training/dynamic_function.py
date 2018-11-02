import types

class MyClass:
	def __init__(self):
		l = {}
		s = '''
def func(self, x):
	print(self, ".", x)
'''
		exec(s, globals(), l)
		self.func = types.MethodType(l['func'], self)

MyClass().func('test')
