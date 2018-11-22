"""共通のイメージ処理.
"""
import math
import numpy as np
import cv2
import dnn

def read_bgr(file):
	"""指定ファイルからBGRイメージとして読み込む.
	# Args:
		file: イメージファイル名.
	# Returns:
		成功したらイメージ、失敗したら None.
	"""
	return cv2.imread(file, cv2.IMREAD_COLOR)

def read_gray(file):
	"""指定ファイルからグレースケールイメージとして読み込む.
	# Args:
		file: イメージファイル名.
	# Returns:
		成功したらイメージ、失敗したら None.
	"""
	return cv2.imread(file, cv2.IMREAD_GRAYSCALE)

def write_bgr(file, bgr):
	"""指定ファイルへBGRイメージを書き込む.
	# Args:
		file: イメージファイル名.
		bgr: BGRイメージ.
	"""
	return cv2.imwrite(file, bgr)

def show_bgr(bgr, caption="image"):
	"""指定BGRイメージをウィンドウへ表示する.
	# Args:
		bgr: BGRイメージ.
	"""
	cv2.imshow(caption, bgr)
	cv2.waitKey(0)

def resize_if_larger(img, size):
	"""指定イメージが指定サイズを超えているなら縮小する、アスペクト比は維持される.
	# Args:
		img: 元イメージ.
		size: 目標サイズ.
	# Returns:
		イメージ.
	"""
	# リサイズ後の画像を作成
	shape = img.shape
	r = size / max(shape[0], shape[1])
	if 1.0 <= r:
		return img

	w = max(int(math.ceil(shape[1] * r)), 1)
	h = max(int(math.ceil(shape[0] * r)), 1)
	return cv2.resize(img, (w, h), interpolation=cv2.INTER_AREA)

def bgr_to_pm(bgr):
	"""BGRイメージを(3, h, w)形状且つレンジ-1...+1に変換する.
	"""
	pm = bgr.transpose(2, 0, 1).astype(dnn.dtype)
	pm -= 127.5
	pm /= 127.5
	return pm

def bgr_to_pm_batch(bgr):
	"""BGRイメージを(1, 3, h, w)形状且つレンジ-1...+1に変換する.
	"""
	pm = bgr.transpose(2, 0, 1)
	pm = pm.reshape((1,) + pm.shape).astype(dnn.dtype)
	pm -= 127.5
	pm /= 127.5
	return pm

def pm_to_bgr(pm):
	"""(3, h, w)形状レンジ-1...+1をBGRイメージに変換する.
	"""
	pm = pm * 127.5
	pm += 127.5
	np.clip(pm, 0, 255, pm)
	bgr = pm.astype(np.uint8)
	bgr = bgr.transpose(1, 2, 0)
	return bgr
