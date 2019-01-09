import numpy as np
import tensorflow as tf
from keras.models import Model
from keras.layers import Conv2D, Flatten, Dense, Input, Lambda, concatenate
from keras import backend as K

sess = tf.InteractiveSession()


a = np.arange(32, dtype=np.float32).reshape((2, 16))
# # a[0, 0] = 10

# # print(np.expand_dims(a[:, 0], -1) + a[:, 1:])

# # b = np.arange(16)
# # print(np.concatenate((a, b)))
# # print(np.expand_dims(a, axis=0).shape)
# # b = np.arange(16)
# # a = a.reshape((2, 8))
# # b = b.reshape((2, 8))

# a = tf.one_hot(5, 10, 1.0, 0.0)
# b = tf.one_hot(6, 10, 1.0, 0.0)
# # value = tf.reduce_sum(a)
# # # value = K.expand_dims(a[:, 0], -1)
# # # value = K.expand_dims(a[:, 0], -1) + a[:, 1:] - tf.stop_gradient(K.mean(a[:,1:], keepdims=True))
# # print(sess.run(a))
print(sess.run(tf.reduce_sum(tf.convert_to_tensor(a), reduction_indices=1)))


# b = tf.convert_to_tensor(b)

# c = concatenate([a, b])

# a = a[:, 0]
# print(a, a.shape)

# a = K.expand_dims(a, -1)

# print(a)
# print(a, a.shape)

