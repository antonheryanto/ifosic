# load data
# 3x1200
# target class, 
# target class patchcord zone, measurement zone, noise zone
# label data generated using boundaries

import msgpack
import torch
import torch.utils.data

def FromMessagePack(filename):
    with open(filename, "rb") as data_file:
        byte_data = data_file.read()
    data_loaded = msgpack.unpackb(byte_data)
    return data_loaded
   
def prepareItem(id = 1, start = 0, stop = 0, width = 1280):
    filename = f"C:\Projects\MMU\Ifosic\src\Python\Set0{id}.msgpack";
    data = FromMessagePack(filename)
    index = data["BoundaryIndexes"]
    traces = torch.tensor(data["Traces"]).transpose(0,1)
    h, w = traces.shape
    if stop == 0:
        stop =  width if w > width else w
    if w > width:
        inputs = traces[:, start:stop] 
    else:
        inputs = torch.zeros(h, width)
        inputs[:, start:stop] = traces
    labels = torch.zeros(h).long()
    labels[index[0]:index[-1]] = 1
    return inputs, labels

def prepareItems():
    i1, l1 = prepareItem(id=1)
    l1[653:659] = 2
    l1[672:685] = 2
    l1[719:723] = 3
    l1[748] = 2
    l1[814] = 2
    l1[878:879] = 2
    l1[913:919] = 2
    i2, l2 = prepareItem(id=2)
    l2[654:660] = 2
    l2[665:681] = 2
    l2[694] = 3
    l2[711:718] = 2
    l2[737:739] = 2
    l2[744:756] = 3
    l2[778:780] = 2
    l2[793:794] = 3
    l2[812:817] = 2
    l2[830:831] = 3
    l2[837:839] = 3
    l2[844:849] = 2
    i3, l3 = prepareItem(id=3, start=840, stop=2120)
    l3[668:685] = 2
    l3[780] = 2
    l3[838] = 2
    l3[904:908] = 2
    l3[972:974] = 3
    l3[1033:1040] = 2
    l3[1064] = 3
    l3[1071] = 3
    l3[1093] = 3
    l3[1098:1104] = 1
    l3[1105:1106] = 2
    inputs = torch.concat([i1, i2, i3])
    labels = torch.concat([l1, l2, l3])
    return inputs, labels

def data_slider(data: torch.tensor, win = 5):
    h,w = data.shape
    hw = win // 2
    slider = torch.zeros(data.shape[0], win, data.shape[1])
    for i in range(win):
        ss = 0 if i > hw else hw-i
        se = 0 if i < hw else i-hw
        ds = 0 if i < hw else i-hw
        de = 0 if i > hw else hw-i
        # print(ss, se, ds, de)
        slider[ss:h-se,i,:] = data[ds:h-de]
    return slider

def prepareFrame(inputs, labels, dst = 'dataset_multi.pth'):
    i1 = data_slider(inputs[0:1950])
    i2 = data_slider(inputs[1950:1950*2])
    i3 = data_slider(inputs[1950*2:1950*3])
    return torch.concat([i1, i2, i3])
    

def split(data):
    n = len(data)
    nt = int(n * 0.9)
    nv = n - nt
    # check using ratio
    train_set, val_set = torch.utils.data.random_split(data, [nt, nv]) 

if __name__ == "__main__":
    path = r'C:\\Projects\\MMU\\Ifosic\\src\\Python'
    inputs, labels =  prepareItems()
    sliders = prepareFrame(inputs, labels)
    torch.save((sliders, labels), f"{path}\\dataset.pth")
    