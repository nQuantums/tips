import torch
import torch.nn.functional as F
from torch.utils import data
import numpy as np

a = torch.tensor([1,2,3.], requires_grad = True)
out = a.sigmoid()
c = out.detach()
# c.zero_()
print(out)

# a = torch.arange(1 * 10 * 4 * 4, dtype=torch.float32).view(1, 10, 4, 4)
# print(a.shape)
# a = a[0].sum(dim=0)
# print(a.shape)

# a = torch.tensor([1, 2, 3], dtype=torch.int8)
# b = torch.tensor([1, 2, 3], dtype=torch.int64)
# print(b - a.type_as(b))
# a = torch.tensor([[0.1], [0.1]], dtype=torch.float32)
# b = torch.tensor([[1.2], [0.2]], dtype=torch.float32)
# print(F.smooth_l1_loss(b, a))


# a = torch.arange(4 * 3).reshape(4, 3).numpy()
# print(a.size)
# offset = torch.arange(4) * 3
# i = torch.tensor([2, 1, 0, 2], dtype=torch.int64)
# print((a - a * 2).abs())
# print(a.argmax(dim=1))
# print(a.take(offset + i).abs())



# s = torch.tensor([[[1, 2], [3, 4]]]).view(-1, 1).squeeze()
# print(s)
# print(s.shape)

# priorities = torch.arange(1, 11, dtype=torch.float32)
# indices = torch.multinomial(priorities, 3, replacement=False).long()
# selected_priorities = torch.gather(priorities, 0, indices)



# print(priorities)
# print(indices)
# print(selected_priorities)


# a = torch.tensor([[1, 2, 3, 4], [1, 2, 3, 4]], dtype=torch.float32)
# b = torch.tensor([[10], [20]], dtype=torch.float32)

# print(a)
# print(a + b)

# import pickle
# import bz2

# import dnn
# from dnn import np
# from dnn import torch
# import apexdqn_test.model as model

# def load(fname):
# 	with bz2.BZ2File(fname, 'rb') as fin:
# 		return pickle.load(fin)

# def dump(obj, fname, level=9):
# 	with bz2.BZ2File(fname, 'wb', compresslevel=level) as fout:
# 		pickle.dump(obj, fout)


# m = model.dqn(0.1, (1, 160, 210), 10, 32)
# x = torch.tensor(np.arange(3 * 1 * 160 * 210, dtype=np.float32).reshape((3, 1, 160, 210)))
# y = m(x)
# print(y)

# d = m.state_dict()
# dnn.save('model.pt', d)
# with open('model.pk', mode='wb') as f:
# 	pickle.dump(d, f)

# d = dnn.load('model.pt')

# with open('model.pk', mode='rb') as f:
# 	d = pickle.load(f)

# print(d)
# dump(d, 'dump.pk')






# # a = np.append(a[1:, :, :], np.arange(16, dtype=np.float32).reshape((1, 16, 2)), axis=0)
# a = torch.tensor(a)
# torch.nn.init.normal_(a, 0, 1)
# print(a)
# maxs, indices = torch.max(a, 1)
# print(maxs, indices)
# print(torch.sum(a, 1))

# ones = torch.eye(16)
# indices = torch.tensor([1, 2, 3, 5, 7])
# print(ones.index_select(0, indices))

# v = torch.tensor(np.asarray([10], dtype=np.float32), requires_grad=True)
# adv = torch.tensor(np.arange(0, 16, dtype=np.float32), requires_grad=True)
# y = torch.cat([v.data, adv.data])
# # print(torch.sum(adv.data))

# print(one_hot(1, 16))

# a = np.arange(17, dtype=np.float32).reshape((1, 17))
# a[0, 0] = 10

# a = torch.tensor(a)

# v = a[:, 0]
# adv = a[:, 1:]

# print(v.expand_as(adv))

# import numpy as np
# import torch
# import torch.nn as nn

# # テンソルを作成
# # requires_grad=Falseだと微分の対象にならず勾配はNoneが返る
# x = torch.tensor(1.0, requires_grad=True)
# w = torch.tensor(2.0, requires_grad=True)
# b = torch.tensor(3.0, requires_grad=True)

# # 計算グラフを構築
# # y = 2 * x + 3
# y = w * x + b

# # 勾配を計算
# y.backward()

# # 勾配を表示
# print(x.grad)  # dy/dx = w = 2
# print(w.grad)  # dy/dw = x = 1
# print(b.grad)  # dy/db = 1
