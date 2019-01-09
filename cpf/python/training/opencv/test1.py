import numpy as np
import cv2

img = np.zeros((256, 256), dtype=np.float32)

pts = np.array([[10, 5], [20, 30], [70, 20], [50, 10]], np.int32)
pts = pts.reshape((-1, 1, 2))
cv2.polylines(img, [pts], True, 1.0)

cv2.imshow('afe', img)
cv2.waitKey(0)
