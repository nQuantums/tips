class Layer:

	def __init__(self, input):
		self.input = input


class Glue:

	def __init__(self, inputs, outputs):
		self.inputs = inputs
		self.outputs = outputs
		self.traced_outputs = set()

	def build(self, caller, depth, code, prebuild):
		if 2 <= len(self.outputs) and prebuild:
			self.traced_outputs.add(caller)
			return (self, depth)

		depth += 1

		glues = set()
		for i in self.inputs:
			g = i.build(self, depth, code, True)
			if g is not None:
				glues.add(g)

		if any(not g[0].is_ready() for g in glues):
			return glues

		glues_list = [g[0] for g in glues]
		glues_list.sort(key=lambda g: g[1])
		for g in reversed(glues_list):
			g.build(self, depth, code, False)

		for i in self.inputs:
			i.build(self, depth, code, False)

	def is_ready(self):
		return self.outputs.issubset(self.traced_outputs)
