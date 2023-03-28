import torch
import torch.optim as optim
import torch.nn as nn
import time
from model import SignalNet, Dataset

device = torch.device("cuda:0" if torch.cuda.is_available() else "cpu") # device object

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
    path = r'C:\\Projects\\MMU\\Ifosic\\src\\Python'
    data = Dataset(f"{path}\\dataset.pth")
    # check using roi
    train_set = data
    indicies = torch.concat([torch.arange(640,940),torch.arange(1950+650,1950+860),torch.arange(2*1950+660,2*1950+1110)])
    val_set = torch.utils.data.Subset(data, indicies)
    print(len(train_set), len(val_set))
    train_loader = torch.utils.data.DataLoader(train_set, batch_size=32, shuffle=True, num_workers=2)
    test_loader = torch.utils.data.DataLoader(val_set, batch_size=32, shuffle=False, num_workers=2)
    model = SignalNet(in_channels=5).to(device)
    model_path = f"{path}\\model.pth"
    model.load_state_dict(torch.load(model_path))
    # train(train_loader, test_loader, model, model_path)
    print(test(test_loader, model))
    # x,y = next(iter(test_loader))
    # print(x.shape, y)
