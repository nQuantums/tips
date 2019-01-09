import os
from collections import deque
import dnn
from dnn import np
from dnn import chainer
from dnn import F
from dnn import L
from dnn import Variable
import model

class Learner():
    def __init__(self, batch_size, lr, history_size, replay_size, hidden_size, action_size, update_cycle, log_interval, actor_num):
        self.batch_size = batch_size
        self.history_size = history_size
        self.replay_size = replay_size
        self.width = model.image_width
        self.height = model.image_height
        self.hidden_size = hidden_size
        self.action_size = action_size
        self.update_cycle = update_cycle
        self.log_interval = log_interval
        self.actor_num = actor_num
        self.alpha = 0.7
        self.beta_init = 0.4
        self.beta = self.beta_init
        self.beta_increment = 1e-6
        self.e = 1e-6
        self.dis = 0.99
        self.start_epoch = 0
        self.mainDQN = model.dqn(True, lr, self.history_size, self.hidden_size, self.action_size)
        self.targetDQN = model.dqn(False, lr, self.history_size, self.hidden_size, self.action_size)
        self.update_target_model()
        self.replay_memory = deque(maxlen=self.replay_size)
        self.priority = deque(maxlen=self.replay_size)

    def update_target_model(self):
        self.targetDQN.load_state_dict(self.mainDQN.state_dict())

    def save_model(self, train_epoch):
        model_dict = {'state_dict': self.mainDQN.state_dict(),
                      'optimizer_dict': self.optimizer.state_dict(),
                      'train_epoch': train_epoch}
        torch.save(model_dict, self.log + 'model.pt')
        print('Learner: Model saved in ', self.log + 'model.pt')

    def load_model(self):
        if os.path.isfile(self.log + 'model.pt'):
            model_dict = torch.load(self.log + 'model.pt')
            self.mainDQN.load_state_dict(model_dict['state_dict'])
            self.optimizer.load_state_dict(model_dict['optimizer_dict'])
            self.update_target_model()
            self.start_epoch = model_dict['train_epoch']
            print("Learner: Model loaded from {}(epoch:{})".format(self.log + 'model.pt', str(self.start_epoch)))
        else:
            raise "=> Learner: no model found at '{}'".format(self.log + 'model.pt')

    def load_memory(self, simnum):
        if os.path.isfile(self.log + 'memory{}.pt'.format(simnum)):
            try:
                memory_dict = torch.load(self.log + 'memory{}.pt'.format(simnum))
                self.replay_memory.extend(memory_dict['replay_memory'])
                self.priority.extend(memory_dict['priority'])
                print('Memory loaded from ', self.log + 'memory{}.pt'.format(simnum))
                memory_dict['replay_memory'].clear()
                memory_dict['priority'].clear()
                torch.save(memory_dict, self.log + 'memory{}.pt'.format(simnum))
            except:
                time.sleep(10)
                memory_dict = torch.load(self.log + 'memory{}.pt'.format(simnum))
                self.replay_memory.extend(memory_dict['replay_memory'])
                self.priority.extend(memory_dict['priority'])
                print('Memory loaded from ', self.log + 'memory{}.pt'.format(simnum))
                memory_dict['replay_memory'].clear()
                memory_dict['priority'].clear()
                torch.save(memory_dict, self.log + 'memory{}.pt'.format(simnum))
        else:
            print("=> Learner: no memory found at ", self.log + 'memory{}.pt'.format(simnum))

    def sample(self):
        priority = (np.array(self.priority) + self.e) ** self.alpha
        weight = (len(priority) * priority) ** -self.beta
        # weight = map(lambda x: x ** -self.beta, (len(priority) * priority))
        weight /= weight.max()
        self.weight = torch.tensor(weight, dtype=torch.float)
        priority = torch.tensor(priority, dtype=torch.float)
        return torch.utils.data.sampler.WeightedRandomSampler(priority, self.batch_size, replacement=True)

    def main(self):
        train_epoch = self.start_epoch
        self.save_model(train_epoch)
        is_memory = False
        while len(self.replay_memory) < self.batch_size * 100:
            print("Memory not enough")
            for i in range(self.actor_num):
                is_memory = os.path.isfile(self.log + '/memory{}.pt'.format(i))
                if is_memory:
                    self.load_memory(i)
                time.sleep(1)
        while True:
            self.optimizer.zero_grad()
            self.mainDQN.train()
            self.targetDQN.eval()
            x_stack = torch.zeros(0, self.history_size, self.height, self.width).to(self.device)
            y_stack = torch.zeros(0, self.action_size).to(self.device)
            w = []
            self.beta = min(1, self.beta_init + train_epoch * self.beta_increment)
            sample_idx = self.sample()
            for idx in sample_idx:
                history, action, reward, next_history, end = self.replay_memory[idx]
                history = history.to(self.device)
                next_history = next_history.to(self.device)
                Q = self.mainDQN(history)
                if end:
                    tderror = reward - Q[0, action]
                    Q[0, action] = reward
                else:
                    qval = self.mainDQN(next_history)
                    tderror = reward + self.dis * self.targetDQN(next_history)[0, torch.argmax(qval, 1)] - Q[0, action]
                    Q[0, action] = reward + self.dis * self.targetDQN(next_history)[0, torch.argmax(qval, 1)]
                x_stack = torch.cat([x_stack, history.data], 0)
                y_stack = torch.cat([y_stack, Q.data], 0)
                w.append(self.weight[idx])
                self.priority[idx] = tderror.abs().item()
            pred = self.mainDQN(x_stack)
            w = torch.tensor(w, dtype=torch.float, device=self.device)
            loss = torch.dot(F.smooth_l1_loss(pred, y_stack.detach(), reduce=False).sum(1), w.detach())
            loss.backward()
            self.optimizer.step()
            loss /= self.batch_size
            self.writer.add_scalar('loss', loss.item(), train_epoch)
            train_epoch += 1
            gc.collect()
            if train_epoch % self.log_interval == 0:
                print('Train Epoch: {} \tLoss: {}'.format(train_epoch, loss.item()))
                self.writer.add_scalar('replay size', len(self.replay_memory), train_epoch)
                if (train_epoch // self.log_interval) % args.actor_num == 0:
                    self.save_model(train_epoch)
                self.load_memory((train_epoch // self.log_interval) % args.actor_num)

            if train_epoch % self.update_cycle == 0:
                self.update_target_model()


if __name__ == "__main__":
    learner = Learner()
    learner.main()

