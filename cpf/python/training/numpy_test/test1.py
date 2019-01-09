import numpy as np
from numpy.lib.stride_tricks import as_strided

x = np.arange(100, dtype=np.float64)
view = as_strided(x, (3, 3), (16, 8))
print(view.max(axis=1))
