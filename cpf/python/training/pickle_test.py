import pickle


class Sample(object):

	def __init__(self, filename):
		"""非 Pickle 化されるときは呼ばれない"""

		# 文字列は Pickle 化できる
		self.filename = filename

		# ファイルオブジェクトは Pickle 化できない
		self.file = open(filename, mode='rb')

	def __getstate__(self):
		"""Pickle 化されるとき呼ばれる"""

		# オブジェクトの持つ属性をコピーする
		state = self.__dict__.copy()

		# Pickle 化できない属性を除去する
		del state['file']

		# Pickle 化する属性を返す
		return state

	def __setstate__(self, state):
		"""非 Pickle 化されるとき呼ばれる"""

		# オブジェクトの持つ属性を復元する
		self.__dict__.update(state)

		# Pickle 化できなかった属性を作りなおす
		self.file = open(self.filename, mode='rb')


def main():
	obj = Sample('c:/work')
	binary = pickle.dumps(obj)
	restored_obj = pickle.loads(binary)
	print(restored_obj.filename)
	print(restored_obj.file)


if __name__ == '__main__':
	main()
