using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechApps.ViewModels;

namespace MMU.Ifosic.WPF.ViewModels;

public partial class ProjectViewModel : ViewModelBase
{
    [ObservableProperty] private OpticalSwitch _switch = new ();

    [RelayCommand]
    private async Task Run()
    {
        await Switch.RunSerial();
    }
}
