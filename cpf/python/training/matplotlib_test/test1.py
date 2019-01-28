import numpy as np
import matplotlib.pyplot as plt

a = np.arange(0, 16)

fig = plt.figure()
ax1 = fig.add_subplot(1, 2, 1)
ax2 = fig.add_subplot(1, 2, 2)
ax1.plot(a, a)
ax2.plot(a, a)
plt.pause(5)
