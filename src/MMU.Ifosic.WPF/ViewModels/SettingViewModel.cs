using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechApps.ViewModels;

namespace MMU.Ifosic.WPF.ViewModels;

public partial class SettingViewModel : ViewModelBase
{
    [ObservableProperty] private string _opticalSwitchAddress = "192.168.1.1";
    [ObservableProperty] private string _opticalSwitchPort = "3082";
    [ObservableProperty] private string _incomingPort = "9";
    [ObservableProperty] private string _outgoingPorts = "1,2,3";
}
