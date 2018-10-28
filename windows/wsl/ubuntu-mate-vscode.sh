sudo apt-get update
sudo apt-get upgrade
sudo apt-get install build-essential
sudo apt install gdb
git clone https://github.com/yyuu/pyenv ~/.pyenv
echo 'export PYENV_ROOT="$HOME/.pyenv"' >> ~/.bashrc
echo 'export PATH="$PYENV_ROOT/bin:$PATH"' >> ~/.bashrc
echo 'eval "$(pyenv init -)"' >> ~/.bashrc
source ~/.bashrc

pyenv install --list | grep anaconda

pyenv install anaconda3-5.3.0
pyenv versions
pyenv global anaconda3-5.3.0
pyenv rehash

conda update conda
conda update --all

# TensorFlow使うなら
conda install python=3.6

# 日本語化
sudo apt install language-pack-ja
sudo update-locale LANG=ja_JP.UTF-8
sudo dpkg-reconfigure tzdata

# MATEデスクトップ環境インストール
sudo apt install mate-desktop-environment-extras ubuntu-mate-core ubuntu-mate-desktop -y
echo 'export DISPLAY=localhost:0.0' >> ~/.bashrc
echo 'export LIBGL_ALWAYS_INDIRECT=1' >> ~/.bashrc

# VSCode
curl https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.gpg
sudo install -o root -g root -m 644 microsoft.gpg /etc/apt/trusted.gpg.d/
sudo sh -c 'echo "deb [arch=amd64] https://packages.microsoft.com/repos/vscode stable main" > /etc/apt/sources.list.d/vscode.list'

sudo apt-get install apt-transport-https
sudo apt-get update
sudo apt-get install code # or code-insiders

# OpenAI
sudo apt install -y python3-dev cmake zlib1g-dev libjpeg-dev xvfb xorg-dev python3-gdbm python3-opengl libboost-all-dev libsdl2-dev swig ffmpeg
pip install --user gym
pip install --user "gym[atari]"

# 深層学習
pip install --user tensorflow
pip install --user keras
