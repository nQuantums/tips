import time
import subprocess
import pyautogui

# explorer.exe 起動してコピー
subprocess.Popen('start explorer.exe /select,g:\\work\\path.txt', shell=True)
time.sleep(3)
pyautogui.keyDown('ctrlleft')
pyautogui.keyDown('c')
pyautogui.keyUp('c')
pyautogui.keyUp('ctrlleft')
time.sleep(3)
pyautogui.keyDown('altleft')
pyautogui.keyDown('f4')
pyautogui.keyUp('f4')
pyautogui.keyUp('altleft')

# explorer.exe 起動してペースト
subprocess.Popen('start explorer.exe c:\\work', shell=True)
time.sleep(3)
pyautogui.keyDown('ctrlleft')
pyautogui.keyDown('v')
pyautogui.keyUp('v')
pyautogui.keyUp('ctrlleft')
time.sleep(3)
pyautogui.keyDown('altleft')
pyautogui.keyDown('f4')
pyautogui.keyUp('f4')
pyautogui.keyUp('altleft')
