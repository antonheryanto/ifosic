import torch.nn as nn
import torch.utils.data
from collections import OrderedDict

class SignalNet(nn.Module):
    def __init__(self, in_channels = 1, out_channels = 4, features = 8):
        super(SignalNet, self).__init__()
        # fix input size 1280
        self.feature = nn.Sequential(
            SignalNet._block(in_channels, features, name ="enc1"),
            nn.MaxPool1d(kernel_size=2, stride=2),
            nn.Dropout(0.1),
            SignalNet._block(features, features * 2, name ="enc2"),
            nn.MaxPool1d(kernel_size=2, stride=2),
            nn.Dropout(0.1)
        )
        self.fc = nn.Sequential(
            nn.Flatten(),
            nn.Linear(16 * 128, 256),
            nn.Dropout(0.1),
            # target class: noise, signal, transition, leak
            nn.Linear(256, out_channels)
        )

    def forward(self, x):
        x = self.feature(x)
        x = self.fc(x)
        return x
    
    @staticmethod
    def _block(in_channels, features, name):
        return nn.Sequential(
            OrderedDict(
                [
                    (
                        name + "conv1",
                        nn.Conv1d(
                            in_channels=in_channels,
                            out_channels=features,
                            kernel_size=3,
                            padding=1,
                            bias=False,
                        ),
                    ),
                    (name + "norm1", nn.BatchNorm1d(num_features=features)),
                    (name + "relu1", nn.ReLU(inplace=True)),
                    (
                        name + "conv2",
                        nn.Conv1d(
                            in_channels=features,
                            out_channels=features,
                            kernel_size=3,
                            padding=1,
                            bias=False,
                        ),
                    ),
                    (name + "norm2", nn.BatchNorm1d(num_features=features)),
                    (name + "relu2", nn.ReLU(inplace=True)),
                ]
            )
        )   
    
class Dataset(torch.utils.data.Dataset):
    def __init__(self, filename: str):
        self.inputs, self.labels = torch.load(filename)

    def __getitem__(self, index):
        return self.inputs[index], self.labels[index]
    
    def __len__(self):
        return len(self.labels)
    