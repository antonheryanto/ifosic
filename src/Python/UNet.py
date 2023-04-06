from collections import OrderedDict
import torch
import torch.nn as nn

class UNet(nn.Module):
    def __init__(self, width = 64, in_channels=1, out_channels=1, nf=8):
        super(UNet, self).__init__()
        
        self.encoder = nn.Sequential(
            UNet._block(in_channels, nf, name="enc1"),
            nn.MaxPool2d(kernel_size=2, stride=2),
            UNet._block(nf, nf * 2, name="enc2"),
            nn.MaxPool2d(kernel_size=2, stride=2),
            UNet._block(nf * 2, nf * 4, name="enc3"),
            nn.MaxPool2d(kernel_size=2, stride=2),
            UNet._block(nf * 4, nf * 8, name="enc4"),        
            nn.MaxPool2d(kernel_size=2, stride=2),
        )

        # self.bottleneck = UNet._block(nf * 8, nf * 16, name="bottleneck")
        self.fc = nn.Sequential(
            nn.Flatten(),
            nn.Linear(64 * width * 2, 256),
            nn.Linear(256, 4)
        )

        # self.decoder = nn.Sequential(
        #     nn.ConvTranspose2d(nf * 16, nf * 8, kernel_size=2, stride=2),
        #     UNet._block(nf * 8, nf * 8, name="dec4"),
        #     nn.ConvTranspose2d(nf * 8, nf * 4, kernel_size=2, stride=2),
        #     UNet._block(nf * 4, nf * 4, name="dec3"),
        #     nn.ConvTranspose2d(nf * 4, nf * 2, kernel_size=2, stride=2),
        #     UNet._block(nf * 2, nf * 2, name="dec2"),
        #     nn.ConvTranspose2d(nf * 2, nf, kernel_size=2, stride=2),
        #     UNet._block(nf, nf, name="dec1"),
        #     nn.Conv2d(in_channels=nf, out_channels=out_channels, kernel_size=1),
        #     nn.Sigmoid(),
        # )

    def forward(self, x):
        x = self.encoder(x)
        # x = self.bottleneck(x)
        x = self.fc(x)
        # x = self.decoder(x)
        return x        
    

    @staticmethod
    def _block(in_channels, features, name):
        return nn.Sequential(
            OrderedDict(
                [
                    (
                        name + "conv1",
                        nn.Conv2d(
                            in_channels=in_channels,
                            out_channels=features,
                            kernel_size=3,
                            padding=1,
                            bias=False,
                        ),
                    ),
                    (name + "norm1", nn.BatchNorm2d(num_features=features)),
                    (name + "relu1", nn.ReLU(inplace=True)),
                    (
                        name + "conv2",
                        nn.Conv2d(
                            in_channels=features,
                            out_channels=features,
                            kernel_size=3,
                            padding=1,
                            bias=False,
                        ),
                    ),
                    (name + "norm2", nn.BatchNorm2d(num_features=features)),
                    (name + "relu2", nn.ReLU(inplace=True)),
                ]
            )
        )