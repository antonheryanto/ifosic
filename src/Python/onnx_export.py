import torch
import torch.nn as nn
import torch.onnx
from model import SignalNet
from UNet import UNet

def model_save(model: nn.Module, model_path: str, onnx_path: str, height = 512, width = 64):
    state_dict = torch.load(model_path)
    layer_names = [i for i, j in state_dict.items()]
    in_channels = state_dict[layer_names[0]].shape[1]
    x = torch.rand(1, in_channels, height, width, requires_grad=True)
    model.load_state_dict(state_dict)
    model.eval()
    torch.onnx.export(model,
                      x,
                      onnx_path,
                      export_params=True,
                      verbose=True,
                      #opset_version=10,
                      do_constant_folding=True,
                      input_names=['input'],
                      output_names= ['output'],
                      dynamic_axes={
                          'input': {
                              0: 'batch_size',

                          },
                          'output': {
                              0: 'batch_size',
                          }
                      })
    
if __name__ == "__main__":
    path = r'C:\Projects\MMU\Ifosic\src\Python'
    height=512
    width=64
    model = UNet(height, width)
    model_save(model, f"{path}\model_{height}_{width}.pth", f"{path}\model_{height}_{width}.onnx", height, width)