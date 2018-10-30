class MyClass1:
    pass


class MyClass2(MyClass1):
    pass


class MyClass3:
    def __init__(self):
        self.mc1 = MyClass1()
        self.mc2 = MyClass2()


def enumerateSubclass(obj, superClassType):
    return {k: v for k, v in vars(obj).items() if isinstance(v, superClassType)}


obj = MyClass3()
obj.mc3 = MyClass1()
setattr(obj, 'mc4', MyClass1())
for k, v in enumerateSubclass(obj, MyClass1).items():
	print(k, ":", v)
