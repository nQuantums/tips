class Layer:

	def __init__(self, input, name):
		self.input = input
		self.name = name
		self.is_built = False

	def collect_glue(self, output, depth, glue_set):
		if self.input is None:
			return
		self.input.collect_glue(self, depth + 1, glue_set)

	def build(self, depth, code):
		if self.is_built:
			return
		if self.input is not None:
			self.input.build(depth + 1, code)
		code.append(self.name)
		self.is_built = True


class Glue:

	def __init__(self, inputs, name):
		self.inputs = inputs
		self.outputs = []
		self.name = name
		self.is_built = False

		for i in inputs:
			if isinstance(i, Glue):
				i.outputs.append(self)

	def output_layer(self, name):
		layer = Layer(self, name)
		self.outputs.append(layer)
		return layer

	def collect_glue(self, output, depth, glue_set):
		if 2 <= len(self.outputs):
			glue_set.add((self, depth))

	def build(self, depth, code):
		if self.is_built:
			return None

		depth += 1

		glue_set = set()
		for i in self.inputs:
			i.collect_glue(self, depth, glue_set)
		glues_list = [g for g in glue_set]
		glues_list.sort(key=lambda g: g[1])
		for g in reversed(glues_list):
			g[0].build(g[1], code)

		for i in self.inputs:
			i.build(depth, code)
		code.append(self.name)
		self.is_built = True


l1 = Layer(None, "l1")
l2 = Layer(None, "l2")
g1 = Glue([l1, l2], "g1")
l3 = g1.output_layer("l3")
l5 = Layer(l3, "l5")
a = Layer(None, "a")
b = Layer(None, "b")
g3 = Glue([a, b, g1], "g3")
l4 = g3.output_layer("l4")
l6 = Layer(l4, "l6")
c = g3.output_layer("c")
i1 = Layer(None, "i1")
i2 = Layer(None, "i2")
g4 = Glue([g3, i1, i2], "g4")
g2 = Glue([l5, l6, c, g4], "g2")
l7 = g2.output_layer("l7")

code = []
l7.build(0, code)

print(code)
