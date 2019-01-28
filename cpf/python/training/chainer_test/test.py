import dnn
from dnn import np
from dnn import chainer
from dnn import Variable

dnn.startup(0)
xp = dnn.xp

a = Variable(xp.array([[1, 2, 3, 4], [1, 2, 3, 4]], dtype=xp.float32))
b = Variable(xp.array([[10], [20]], dtype=xp.float32))
print(a + b)

# x = np.arange(1 * 1 * 32 * 32, dtype=np.float32).reshape((1, 1, 32, 32))
# l = chainer.links.Convolution2D(1, 1, ksize=8, stride=4, pad=0, dilate=1)
# with chainer.no_backprop_mode():
# 	y = l(x)
# print(y.shape)
