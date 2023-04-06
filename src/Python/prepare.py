# load data
# 3x1200
# target class, 
# target class patchcord zone, measurement zone, noise zone
# label data generated using boundaries

import msgpack
import torch
import torch.utils.data
import PIL.Image as Image
import numpy
import math

class NumberToGrid:
    def __init__(self, min = 0.0, max = 1.0, length = 10):
        self.length = length
        self.step = (max - min) / length
        self.values = numpy.linspace(min, max, self.length)

    def getClass(self, v):
        if v < self.values[0]:
            b = numpy.where(self.values == self.values[0])
        elif v > self.values[-1]:
            b = numpy.where(self.values == self.values[-1])
        else:
            b = numpy.where(numpy.logical_and(self.values > (v - self.step), self.values < (v + self.step)))
        return b[0]
    
    def item(self, v):
        idx = self.getClass(v)[0]
        return numpy.identity(self.length)[idx]
    
    def items(self, arr: numpy.ndarray):
        grid = torch.zeros(arr.shape[0], self.length)
        for i,v in enumerate(arr):
            grid[i] = torch.tensor(self.item(v))
        return grid

def FromMessagePack(filename):
    with open(filename, "rb") as data_file:
        byte_data = data_file.read()
    data_loaded = msgpack.unpackb(byte_data)
    return data_loaded
   
def prepareItem2(id = 1, start = 0, stop = 0, width = 1280):
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

def prepareItem(id = 1, width = 512):
    filename = f"C:\Projects\MMU\Ifosic\src\Python\Set0{id}.msgpack";
    data = FromMessagePack(filename)
    index = data["BoundaryIndexes"]
    traces = numpy.array(data["Traces"])
    tracesT = numpy.transpose(traces, (1,0))
    b, _ = tracesT.shape
    img = Image.fromarray(tracesT)
    imgR = img.resize((width, b))
    inputs = numpy.array(imgR)
    labels = torch.zeros(b).long()
    labels[index[0]:index[-1]] = 1
    return inputs, labels

def prepareItem2d(id = 1, height = 512, width = 32, min = -20, max = 30):
    inputs, labels = prepareItem(id, height)
    ng = NumberToGrid(min, max, width)
    grid = torch.zeros(inputs.shape[0], inputs.shape[1], ng.length)
    for i,v in enumerate(inputs):
        grid[i] = ng.items(v)
    return grid, labels 

def prepareItems(l1, l2, l3):
    l1[653:659] = 2
    l1[672:685] = 2
    l1[719:723] = 3
    l1[748] = 2
    l1[814] = 2
    l1[878:879] = 2
    l1[913:919] = 2
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
    return torch.concat([l1, l2, l3])

def make_grids(inputs: torch.tensor, labels: torch.tensor, g = 512):
    n = math.ceil(inputs.shape[1] / g)
    grids = torch.zeros(inputs.shape[0], g * n)
    grids[:,:inputs.shape[1]] = inputs
    shaped = grids.reshape(grids.shape[0], grids.shape[1] // g, g)
    lgrids = torch.zeros(labels.shape[0] * n).long()
    k = 0
    for j in range(labels.shape[0]):
        for i in range(n):
            lgrids[k] = labels[j]
            k += 1
    imgs = shaped.reshape(shaped.shape[0] * shaped.shape[1], shaped.shape[2])
    return imgs, lgrids

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

def label_slider(data:torch.tensor, win = 5):
    h,w = data.shape
    hw = win // 2
    slider = torch.zeros(data.shape[0], win)
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
    i1, l1 = prepareItem2d(1, width=64)
    i2, l2 = prepareItem2d(2, width=64)
    i3, l3 = prepareItem2d(3, width=64)
    labels = prepareItems(l1, l2, l3)
    inputs = torch.concat([i1, i2, i3]).unsqueeze(1)
    torch.save((inputs, labels), f"{path}\\dataset.pth")
    