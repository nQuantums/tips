class Model:

	def __init__(self):
		self.input_layer = None
		self.output_layer = None
		self.nodes = set()
		self.layers = []
		self.glues = []
		self.local_variable_ids = set()
		self.code = []
		self.dot_code = []

	def input(self):
		layer = Layer(self, None, 'input_layer')
		self.input_layer = layer
		self.nodes.add(layer)
		return layer

	def assign_output(self, output):
		self.output_layer = output
		output.name = 'output_layer'

	def layer(self, input):
		layer = Layer(self, input, 'layer_' + str(len(self.layers)))
		self.layers.append(layer)
		self.nodes.add(layer)
		return layer

	def glue(self, inputs):
		glue = Glue(self, inputs, 'glue_' + str(len(self.glues)))
		self.glues.append(glue)
		self.nodes.add(glue)
		return glue

	def new_local_variable_id(self):
		ids = self.local_variable_ids
		id = 1
		while id in ids:
			id += 1
		ids.add(id)
		return id

	def del_local_variable_id(self, id):
		if id == 0:
			return
		self.local_variable_ids.remove(id)

	def get_local_variable_name(self, id):
		if id == 0:
			return 'x'
		if id in self.local_variable_ids:
			return 'x' + str(id)
		else:
			raise LookupError('There is no variable with the specified ID ' + str(id) + '.')

	def build(self):
		self.code.append('def func(self, x)')
		self.dot_code.insert(0, 'digraph {\n')
		self.output_layer.build(0)
		self.dot_code.append('}')


class Layer:

	def __init__(self, model, input, name):
		self.model = model
		self.input = input
		self.output_variable_id = None
		self.name = name
		self.is_uner_build = False
		self.is_built = False

	def __str__(self):
		return self.name

	def __repr__(self):
		return self.name

	def get_output_variable_id(self, layer):
		if self.output_variable_id is not None:
			return self.output_variable_id
		else:
			self.output_variable_id = self.model.new_local_variable_id()
			return self.output_variable_id

	def collect_glue(self, output, depth, glue_set):
		if self.input is None:
			return
		self.input.collect_glue(self, depth + 1, glue_set)

	def build(self, depth):
		if self.is_built:
			return
		if self.is_uner_build:
			raise Exception()
		self.is_uner_build = True

		if self.input is None:
			in_id = 0
		else:
			self.input.build(depth + 1)
			in_id = self.input.get_output_variable_id(self)
		in_name = self.model.get_local_variable_name(in_id)
		self.model.del_local_variable_id(in_id)

		out_id = self.model.new_local_variable_id()
		out_name = self.model.get_local_variable_name(out_id)
		self.output_variable_id = out_id

		self.model.code.append('\t{} = self.{}({})'.format(out_name, self.name, in_name))
		if self.input is not None:
			self.model.dot_code.append('{} -> {} [label="{}"];'.format(self.input.name, self.name, in_name))

		self.is_built = True
		self.is_uner_build = False


class Glue:

	def __init__(self, model, inputs, name):
		for i in inputs:
			if not isinstance(i, Layer):
				raise TypeError("Invalid argument. 'inputs' element must be Layer.")

		self.model = model
		self.inputs = inputs
		self.outputs = []
		self.output_variable_ids = {}
		self.name = name
		self.is_built = False
		self.is_uner_build = False

	def __str__(self):
		return self.name

	def __repr__(self):
		return self.name

	def output(self):
		layer = self.model.layer(self)
		self.outputs.append(layer)
		return layer

	def get_output_variable_id(self, layer):
		if layer in self.output_variable_ids:
			return self.output_variable_ids[layer]
		else:
			id = self.model.new_local_variable_id()
			self.output_variable_ids[layer] = id
			return id

	def collect_glue(self, output, depth, glue_set):
		if 2 <= len(self.outputs):
			glue_set.add((self, depth))

	def build(self, depth):
		if self.is_built:
			return
		if self.is_uner_build:
			raise Exception()
		self.is_uner_build = True

		depth += 1

		glue_set = set()
		for i in self.inputs:
			i.collect_glue(self, depth, glue_set)
		glues_list = [g for g in glue_set]
		glues_list.sort(key=lambda g: g[1])
		for g in reversed(glues_list):
			g[0].build(g[1])

		in_ids = []
		for i in self.inputs:
			i.build(depth)
			in_ids.append(i.get_output_variable_id(self))

		out_ids = [self.get_output_variable_id(o) for o in self.outputs]

		in_names = ', '.join([self.model.get_local_variable_name(id) for id in in_ids])
		out_names = ', '.join([self.model.get_local_variable_name(id) for id in out_ids])
		self.model.code.append('\t{} = self.{}(({}))'.format(out_names, self.name, in_names))

		for i in self.inputs:
			self.model.dot_code.append('{} -> {} [label="{}"];'.format(i.name, self.name, self.model.get_local_variable_name(i.get_output_variable_id(self))))

		for id in in_ids:
			self.model.del_local_variable_id(id)

		self.is_built = True
		self.is_uner_build = False


m = Model()

g = m.glue([m.input()])

l1 = g.output()
l1 = m.layer(l1)
l1 = m.layer(l1)

l2 = g.output()
l2 = m.layer(l2)
l2 = m.layer(l2)

l3 = g.output()
l3 = m.layer(l3)

g = m.glue([l1, l2, l3])
g = m.glue([g.output()])

m.assign_output(g.output())

m.build()

with open("test.dot", mode='w') as f:
	f.write("\n".join(m.dot_code))

print('\n'.join(m.code))
