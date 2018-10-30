"""ディープラーニング関係ヘルパ、テンソルのCPU↔GPU変換処理など.
"""
import numpy as np, cupy as cp, chainer, chainer.functions as F, chainer.links as L
from chainer import Variable
from chainer.link import Link
from chainer import cuda
xp = None
test = False

class Layer(chainer.Chain):
    """レイヤ、学習対象の重み、活性化関数、入力レイヤへの参照を持つ.

    Args:
            link: 学習対象の重みを持つ chainer.links.Convolution2D など chainer.link.Link を継承するもの.
            activator: 活性化関数、chainer.functions.relu など.
            inputLayer: Layer を継承する入力レイヤまたは None.
    """

    def __init__(self, link, activator, inputLayer):
        if not isinstance(link, Link):
            raise TypeError('cannot register a non-link object as a child')
        super().__init__()
        self.inputLayer = inputLayer
        with self.init_scope():
            self.link = link
            self.activator = activator

    def __call__(self, x=None):
        """計算を実行する.

        Args:
                x: 入力値.

        Returns:
                結果.
        """
        if self.inputLayer is not None:
            x = self.inputLayer(x)
        x = self.link(x)
        if self.activator is not None:
            x = self.activator(x)
        return x


class Model(chainer.Chain):
    """モデル、 input() 、 glue() 、 output() により複数のレイヤを保持する事が可能.

    Args:
            optimizer: 重み最適化処理方法、 chainer.optimizer.Optimizerr を継承するもの.
    """

    def __init__(self, optimizer):
        super().__init__()
        self.layerNamePrefix = 'layer'
        self.layerNameCount = 0
        self.inputLayer = None
        self.outputLayer = None
        self.optimizer = optimizer

    def input(self, link, activator):
        """入力レイヤを作成する.

        Args:
                link: 学習対象の重みを持つ chainer.links.Convolution2D など chainer.link.Link を継承するもの.
                activator: 活性化関数、chainer.functions.relu など.

        Returns:
                レイヤ.
        """
        layer = Layer(link, activator, None)
        del self.inputLayer
        with self.init_scope():
            self.inputLayer = layer
        return layer

    def glue(self, name, link, activator, inputLayer):
        """中間レイヤを作成する.

        Args:
                link: 学習対象の重みを持つ chainer.links.Convolution2D など chainer.link.Link を継承するもの.
                activator: 活性化関数、chainer.functions.relu など.
                inputLayer: Layer を継承する入力レイヤ.

        Returns:
                レイヤ.
        """
        layerNameCount = self.layerNameCount
        if name is None:
            layerNameCount += 1
            name = self.layerNamePrefix + str(layerNameCount)
        layer = Layer(link, activator, inputLayer)
        self.add_link(name, layer)
        self.layerNameCount = layerNameCount
        return layer

    def output(self, link, activator, inputLayer):
        """出力レイヤを作成する.

        Args:
                link: 学習対象の重みを持つ chainer.links.Convolution2D など chainer.link.Link を継承するもの.
                activator: 活性化関数、chainer.functions.relu など.
                inputLayer: Layer を継承する入力レイヤ.

        Returns:
                レイヤ.
        """
        layer = Layer(link, activator, inputLayer)
        del self.outputLayer
        with self.init_scope():
            self.outputLayer = layer
        return layer

    def __call__(self, x):
        """計算を実行する.

        Args:
                x: 入力値.

        Returns:
                結果.
        """
        return self.outputLayer(x)

    def compile(self):
        """モデルの構築が完了後、計算準備の最終処理を行う.
        """
        self.optimizer.setup(self)


def startup(gpu, train=True):
    """環境を初期化する.

    Args:
            gpu: 使用するGPUインデックス、負数ならGPU未使用となる.
    """
    global test
    global xp
    if xp is not None:
        return
    test = not train
    chainer.config.train = train
    if not train:
        chainer.config.no_backprop_mode()
    if 0 <= gpu:
        print(('Using cuda device {}.').format(gpu))
        cuda.get_device(gpu).use()
        xp = cp
    else:
        print('Using numpy.')
        xp = np


def to_gpu(x):
    """GPU利用可能状態ならGPUメモリオブジェクトに変換する.

    Args:
            x: 変換対象オブジェクト.
    Returns:
            変換後のオブジェクト.
    """
    if xp is cp:
        if isinstance(x, Model):
            m = x.to_gpu()
            m.optimizer = x.optimizer
            return m
        if isinstance(x, chainer.Link):
            return x.to_gpu()
        if isinstance(x, chainer.Optimizer):
            return x
        if isinstance(x, chainer.Variable):
            return x.to_gpu()
        if isinstance(x, tuple):
            return tuple([to_gpu(e) for e in x])
        if isinstance(x, list):
            return [to_gpu(e) for e in x]
        return cuda.to_gpu(x)
    else:
        return x


def to_cpu(x):
    """GPU利用可能状態ならCPUメモリオブジェクトに変換する.

    Args:
            x: 変換対象オブジェクト.
    Returns:
            変換後のオブジェクト.
    """
    if xp is cp:
        if isinstance(x, Model):
            m = x.to_cpu()
            m.optimizer = x.optimizer
            return m
        if isinstance(x, chainer.Link):
            return x.to_cpu()
        if isinstance(x, chainer.Optimizer):
            return x
        if isinstance(x, chainer.Variable):
            return x.to_cpu()
        if isinstance(x, tuple):
            return tuple([to_cpu(e) for e in x])
        if isinstance(x, list):
            return [to_cpu(e) for e in x]
        return cuda.to_cpu(x)
    else:
        return x


def save(file_name, model):
    model = to_cpu(model)
    chainer.serializers.save_npz(file_name + '.mdl', model)
    chainer.serializers.save_npz(file_name + '.opt', model.optimizer)


def load(file_name, model):
    model = to_cpu(model)
    chainer.serializers.load_npz(file_name + '.mdl', model)
    chainer.serializers.load_npz(file_name + '.opt', model.optimizer)
    return model


if __name__ == '__main__':
    startup(0)
