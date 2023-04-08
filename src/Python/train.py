import torch
import torch.optim as optim
import torch.nn as nn
import time
from model import SignalNet, Dataset
from UNet import UNet

# Get cpu, gpu or mps device for training.
device = (
    "cuda"
    if torch.cuda.is_available()
    else "mps"
    if torch.backends.mps.is_available()
    else "cpu"
)
# print(f"Using {device} device")

def train2(dataloader, model, loss_fn, optimizer):
    size = len(dataloader.dataset)
    model.train()
    for batch, (X, y) in enumerate(dataloader):
        X, y = X.to(device), y.to(device)
        
        # Compute prediction error
        pred = model(X)
        loss = loss_fn(pred, y)

        # Backpropagation
        optimizer.zero_grad()
        loss.backward()
        optimizer.step()

        # if batch % 1950 == 0:
        #     current = (batch + 1) * len(X)
        #     print(f"loss: {loss.item():>7f} [{current:>5d}/{size:>5d}]")



def train(train_loader, test_loader, model, output_path="model.pth"):
    num_epochs = 50   #(set no of epochs)
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
            # Compute prediction error
            preds = model(inputs)
            loss = criterion(preds, labels)

            # Backpropagation
            optimizer.zero_grad()
            loss.backward()
            optimizer.step()

            # statistic
            train_loss += loss.item()
            train_corrects += (preds.argmax(1) == labels).type(torch.float).sum().item()
            
        epoch_loss = train_loss / len(train_loader.dataset)
        train_acc = train_corrects / len(train_loader.dataset) * 100.        
        test_acc = test(test_loader, model, criterion)
        print(f'Epoch [{epoch:04d} / num_epochs] Loss: {epoch_loss:.4f} Train Acc: {train_acc:.4f}% Test Acc: {test_acc:.4f}% Time: {(time.time() -start_time):.4f}s')    
        if max_acc < test_acc:
            max_acc = test_acc
            torch.save(model.state_dict(), output_path)
        if max_acc == 100:
            return

def test(dataloader, model, loss_fn):
    model.eval()
    test_loss = 0
    correct = 0
    with torch.no_grad():
        for X, y in dataloader:
            X, y = X.to(device), y.to(device)
            pred = model(X)
            test_loss += loss_fn(pred, y).item()
            correct += (pred.argmax(1) == y).type(torch.float).sum().item()
    test_loss /= len(dataloader)
    correct /= len(dataloader.dataset)
    # print(f"Test Error: \n Accuracy: {(100*correct):>0.1f}%, Avg loss: {test_loss:>8f} \n")
    return correct * 100

def run():
    optimizer = optim.Adam(model.parameters(), lr=1e-4)
    criterion = nn.CrossEntropyLoss()
    epochs = 5
    for t in range(epochs):
        print(f"Epoch {t+1}\n-------------------------------")
        train(train_loader, model, criterion, optimizer)
        test(val_loader, model, criterion)
    print("Done!")


if __name__ == "__main__":
    path = r'C:\\Projects\\MMU\\Ifosic\\src\\Python'
    # data = Dataset(f"{path}\\dataset.pth")
    data = Dataset(f"{path}\\dataset_all.pth")
    # check using roi
    # train_set = data
    i1 = torch.arange(640,940)
    i2 = torch.arange(650,860) 
    i3 = torch.arange(660,1110)
    indicies = torch.concat([
        i1+1950*0,
        i2+1950*1,
        i3+1950*2,
        i1+1950*3,
        i2+1950*4,
        i3+1950*5,
        i1+1950*6,
        i2+1950*7,
        i3+1950*8,
        i2+1950*9,
    ])
    val_set = torch.utils.data.Subset(data, indicies)
    print(len(data), len(val_set))
    train_loader = torch.utils.data.DataLoader(data, batch_size=50, shuffle=True, num_workers=2)
    val_loader = torch.utils.data.DataLoader(val_set, batch_size=50, shuffle=False, num_workers=2)    
    model = UNet(width=64).to(device)
    model_path = f"{path}\\model.pth"
    # model = torch.load(model_path)
    # model.load_state_dict(torch.load(model_path))
    train(train_loader, val_loader, model, model_path)
    # print(test(val_loader, model))
    # x,y = next(iter(test_loader))
    # print(x.shape, y)
