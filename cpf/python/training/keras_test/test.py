import numpy as np
import tensorflow as tf
from keras.models import Model
from keras.layers import Conv2D, Flatten, Dense, Input, Lambda, concatenate
from keras import backend as K


a = np.arange(16)
b = np.arange(16)
a = a.reshape((2, 8))
b = b.reshape((2, 8))

a = tf.convert_to_tensor(a)
b = tf.convert_to_tensor(b)

c = concatenate([a, b])

a = a[:, 0]
print(a, a.shape)

a = K.expand_dims(a, -1)

print(a)
print(a, a.shape)

