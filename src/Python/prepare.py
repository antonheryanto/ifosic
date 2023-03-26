# load data
# 3x1200
# target class, 
# target class patchcord zone, measurement zone, noise zone
# label data generated using boundaries

import msgpack
import time
import torch
import torch.utils.data
import torch.nn as nn
import torch.optim as optim
import torchvision

def FromMessagePack(filename):
    with open(filename, "rb") as data_file:
        byte_data = data_file.read()
    data_loaded = msgpack.unpackb(byte_data)
    return data_loaded

def ToMessagePack(data, filename):
    with open(filename, "wb") as outfile:
        packed = msgpack.packb(data)
        outfile.write(packed)

class Dataset(torch.utils.data.Dataset):
    def __init__(self, filename: str):
        # boundaryIndexes = [654,686,750,814,879,913]
        # generate label noise or measurement
        # generate base 16 input
        data = FromMessagePack(filename)
        index = data["BoundaryIndexes"]
        self.traces = torch.tensor(data["Traces"])
        
        print(self.traces.shape)
        self.traces = torchvision.transforms.Resize(self.traces, self.traces.shape[0].item(), 1024)
        print(self.traces.shape)
        self.labels = torch.zeros(self.traces.shape[0]).long()
        self.labels[index[0]:index[-1]] = 1

    def __getitem__(self, index):
        return self.traces[index].unsqueeze(0), self.labels[index]
    
    def __len__(self):
        return len(self.labels)

class FreqCNN(nn.Module):
    def __init__(self, features = 8):
        super(FreqCNN, self).__init__()
        self.feature = nn.Sequential(
            nn.Conv1d(1, features, 3),
            nn.BatchNorm1d(num_features=features),
            nn.ReLU(inplace=True),
            nn.Conv1d(features, features * 2, 3),
            nn.BatchNorm1d(num_features= features * 2),
            nn.ReLU(inplace=True),
        )
        self.fc = nn.Sequential(
            nn.Flatten(),
            nn.Linear(16 * 1946, 256),
            nn.Linear(256, 2),
        )

    def forward(self, x):
        x = self.feature(x)
        x = self.fc(x)
        return x

device = torch.device("cuda:0" if torch.cuda.is_available() else "cpu") # device object

def train(train_loader, test_loader, model, output_path="model.pth"):
    num_epochs = 5   #(set no of epochs)
    optimizer = optim.Adam(model.parameters(), lr=1e-4)
    criterion = nn.CrossEntropyLoss()
    start_time = time.time() #(for showing time)
    max_acc = 0
    for epoch in range(num_epochs): #(loop for every epoch)
        """ Training Phase """
        model.train()    #(training model)
        train_loss = 0.   #(set loss 0)
        train_corrects = 0 
        # load a batch data of images    
        for i, (inputs, labels) in enumerate(train_loader):
            inputs = inputs.to(device)
            labels = labels.to(device) 
            # forward inputs and get output
            optimizer.zero_grad()
            outputs = model(inputs)
            _, preds = torch.max(outputs, 1)
            loss = criterion(outputs, labels)
            # get loss value and update the network weights
            loss.backward()
            optimizer.step()
            train_loss += loss.item() * inputs.size(0)
            train_corrects += torch.sum(preds == labels.data).item()
        epoch_loss = train_loss / len(train_loader.dataset)
        train_acc = train_corrects / len(train_loader.dataset) * 100.
        
        test_acc = test(test_loader, model)
        print(f'Epoch [{epoch:04d} / num_epochs] Loss: {epoch_loss:.4f} Train Acc: {train_acc:.4f}% Test Acc: {test_acc:.4f}% Time: {(time.time() -start_time):.4f}s')    
        if max_acc < test_acc:
            max_acc = test_acc
            torch.save(model.state_dict(), output_path)

def test(test_loader, model):
    """ Testing Phase """
    model.eval()
    test_acc = 0
    with torch.no_grad():
        test_corrects = 0
        for inputs, labels in test_loader:
            inputs = inputs.to(device)
            labels = labels.to(device)
            outputs = model(inputs)
            _, preds = torch.max(outputs, 1)
            test_corrects += torch.sum(preds == labels.data).item()
        test_acc = test_corrects / len(test_loader.dataset) * 100.
    return test_acc
   

if __name__ == "__main__":
    inputFile = "C:\Projects\MMU\Ifosic\src\Python\Set01.msgpack";
    # data = Dataset(inputFile)
    # train_loader = torch.utils.data.DataLoader(data, batch_size=16, shuffle=True, num_workers=2)
    # test_loader = torch.utils.data.DataLoader(data, batch_size=16, shuffle=False, num_workers=2)
    # model = FreqCNN().to(device)
    # model.load_state_dict(torch.load('model.pth'))
    # # train(train_loader, test_loader, model)
    # x,y = next(iter(test_loader))
    # x = x.to(device)
    # print(x.shape)    
    # o = model(x)
    # _, pred = torch.max(o, 1)
    # print(pred, y)
