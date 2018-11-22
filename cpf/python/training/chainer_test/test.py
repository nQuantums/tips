import dnn
from dnn import np
from dnn import chainer
from dnn import F
from dnn import L
from dnn import Variable

x = np.arange(16, dtype=np.float32)
print(x)
x = x.reshape((16 // 8, 8))
print(x)
m = F.average(x, 1, keepdims=True)
print(m)
print(F.tile(m, tuple([t // s for t, s in zip(x.shape, m.shape)])))



# x = np.array([1, 2, 3])
# print(x)
# y = F.tile(x, (2, 1))
# print(y)
