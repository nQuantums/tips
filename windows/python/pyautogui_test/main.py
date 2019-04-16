# -*- coding: utf-8 -*-

##PyAutoGUIのモジュール
import pyautogui

##プロセスを制御するためにOS周りのモジュール
import re
import os
import subprocess
import sys
import time
import array

##Win32のUI情報と制御用モジュール
import win32api
import win32gui
import win32con

##MacやLinuxの場合は、↑３つの代わりに
#import re
#import subprocess
#を利用して以下のようにすると
#
#from subprocess import Popen, PIPE
#cmd = "xwininfo -name (ウインドウ名)"
#p = Popen(cmd.split(' '),stdout = PIPE, stderr = PIPE)
#ret = str(p.communicate())
#coord = re.search('X:\s+(\d+)[^Y]+Y:\s+(\d+)',ret)
#appwin_x,appwin_y = coord.groups()
#
##アプリウインドウの左上(appwin_x,appwin_y)が取れます

def activate_by_click(target_hwnd):
	rect = win32gui.GetWindowRect(target_hwnd)
	pos = pyautogui.position()
	pyautogui.moveTo(rect[0], rect[1], duration=0)
	pyautogui.click()
	pyautogui.moveTo(pos.x, pos.y, duration=0)
	# curmx, curmy = mouselib.get_cursorpos()
	# mouselib.set_cursorpos(rect.xpos, rect.ypos)
	# mouselib.left_click()
	# # restore cursor pos.
	# mouselib.set_cursorpos(curmx, curmy)

def activate_by_attach(hwnd):
	import win32process
	import win32api
	import win32gui
	import ctypes

	forehwnd = win32gui.GetForegroundWindow()
	fore_threadid, processid = win32process.GetWindowThreadProcessId(forehwnd)
	current_threadid = win32api.GetCurrentThreadId()

	# foreground なスレッドにアタッチする
	if fore_threadid != current_threadid:
		try:
			# たまに error:87 が起きるので吸収.
			win32process.AttachThreadInput(current_threadid, fore_threadid, True)
		except:
			pass

	try:
		ctypes.windll.user32.BringWindowToTop(hwnd)
	except:
		pass

	if fore_threadid != current_threadid:
		try:
			win32process.AttachThreadInput(current_threadid, fore_threadid, False)
		except:
			pass

def activete(target_hwnd):
	activate_by_attach(target_hwnd)
	activate_by_click(target_hwnd)


#以下、メインルーチン
if __name__ == "__main__":
	#実行前の待機(秒)
	time.sleep(1)
	#画面サイズの取得
	screen_x,screen_y = pyautogui.size()

	#マウスを(1,1)に移動しておく
	# pyautogui.moveTo(1, 1, duration=1)

	#win32guiを使ってウインドウタイトルを探す
	#Windowのハンドル取得('クラス名','タイトルの一部')で検索クラスがわからなかったらNoneにする
	#有名どころで('#32770',"名前を付けて保存")かな
	parent_handle = win32gui.FindWindow(None, "電卓")

	#ハンドルIDが取れなかったら、電卓を起動する
	if parent_handle == 0 :
		cmd = 'start C:\Windows\System32\calc.exe'
		subprocess.Popen(cmd, shell=True)
		time.sleep(3)
		parent_handle = win32gui.FindWindow(None, "電卓")

	if parent_handle == 0 :
		print(u"アプリの起動に失敗したみたい、中断します")
		sys.exit()

	#ハンドルが取れたら、ウインドウの左上と右下の座標と画面のアクティブ化
	#ちなみに、アプリ内のボタンとか入力窓も頑張ればとれるけど、win32guiでやると複雑になりすぎる
	#おとなしく、アプリの座標とトップレベルウインドウの情報だけ使う
	if parent_handle > 0 :
		win_x1,win_y1,win_x2,win_y2 = win32gui.GetWindowRect(parent_handle)
		print(u"アプリの座標:"+str(win_x1)+"/"+str(win_y1))
		apw_x = win_x2 - win_x1
		apw_y = win_y2 - win_y1
		print(u"アプリの画面サイズ:"+str(apw_x)+"/"+str(apw_y))
		print(u"アプリを最前面に持ってくるよ")
		# win32gui.SetForegroundWindow(parent_handle)
		activete(parent_handle)
		#ウインドウの完全な情報を取ってくる、FindWindowで部分一致だったりした場合の補完用
		titlebar = win32gui.GetWindowText(parent_handle)
		classname = win32gui.GetClassName(parent_handle)


	#Drag/Drop関数があるけれど、実はmouseDownとmoveToを使ったほうがいい
	#アプリのマウスオーバー挙動はD&Dでは反応しない(逆に反応させないように使う場合はdragTo/dragRelがいい)
	print(u"移動して")
	pyautogui.moveTo(win_x1+40,win_y1+4, duration=1)
	print(u"掴んで")
	pyautogui.mouseDown(win_x1+40,win_y1+4, button='left')
	print(u"移動して")
	pyautogui.moveTo(100,200, duration=1)
	print(u"移動して")
	pyautogui.moveTo(110,100, duration=1)
	print(u"移動して")
	pyautogui.moveTo(120,300, duration=1)
	print(u"移動して")
	pyautogui.moveTo(130,100, duration=1)
	print(u"移動して")
	pyautogui.moveTo(140,300, duration=1)
	print(u"移動して")
	pyautogui.moveTo(150,100, duration=1)
	print(u"移動して")
	pyautogui.moveTo(160,300, duration=1)
	print(u"移動して")
	pyautogui.moveTo(170,100, duration=1)
	print(u"移動して")
	pyautogui.moveTo(180,300, duration=1)
	print(u"移動して")
	pyautogui.moveTo(190,100, duration=1)
	print(u"移動して")
	pyautogui.moveTo(200,300, duration=1)
	print(u"放す")
	pyautogui.mouseUp(210,200, button='left')
	print(u"おしまい。とじていいよ")
	time.sleep(30)
