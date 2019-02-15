import json

def load():
	with open('parameters.json', 'r') as f:
		return json.load(f)

if __name__ == "__main__":
	print(load())
