using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.Mvvm.Messaging;
using MMU.Ifosic.Neubrex;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using TechApps.ViewModels;
using NPOI.OpenXmlFormats.Dml.WordProcessing;

namespace MMU.Ifosic.WPF.ViewModels;

public partial class ProjectViewModel : ViewModelBase
{
    [ObservableProperty] private OpticalSwitch _switch = new ();
    [ObservableProperty] private SessionRunner _runner = new ();


    void UpdateSequence()
    {
        var ports = Switch.GetPorts();
        if (Runner.Sequences.Count > 0)
            Runner.Sequences.Clear();

        foreach (var port in ports)
        {
            Runner.Sequences.Add(new SessionSequence { Port = port });
        }
    }

    public ProjectViewModel()
    {
        UpdateSequence();
        Switch.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName != "Ports")
                return;
            UpdateSequence();
        };
    }

    [RelayCommand]
    private async Task Run()
    {
        //await Switch.RunSerial();
        Runner.Calculate(async (i) => await Switch.ToPortAsync(i));
        // ProgressViewModel.Init(() =>
        // {
        //     Runner.Calculate(async (i) => await Switch.ToPortAsync(i));
        // }, () =>
        // {
        //     System.Diagnostics.Debug.WriteLine("Finish");
        // });
    }


    [RelayCommand]
    private void Browse(SessionSequence? sequence)
    {
        sequence.Path = WeakReferenceMessenger.Default.Send(new RequestMessage<string>()).Response;
    }
}
