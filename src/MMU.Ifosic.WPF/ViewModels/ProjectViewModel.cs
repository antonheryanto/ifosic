﻿using CommunityToolkit.Mvvm.ComponentModel;
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

    private void UpdateSequence()
    {
        var ports = Switch.GetPorts();
        if (Runner.Sequences.Count > 0)
            Runner.Sequences.Clear();

        foreach (var port in ports)
        {
            Runner.Sequences.Add(new SessionSequence { Port = port, Path = $@"E:\MMU_PRSB_20230614\MMU PRSB 20230614_F{port}" });
        }
    }

    public ProjectViewModel()
    {
        var p = Workspace.Instance.Project;
        if (p is not null)
        {
            Switch = p.Switch;
            Runner = p.Runner;
        }
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
        //Runner.Calculate(async (i) => await Switch.ToPortAsync(i));
        ProgressViewModel.Init(() =>
        {
            Runner.Calculate((i) => Switch.ToPort(i));
        }, () =>
        {
            System.Diagnostics.Debug.WriteLine("Finish");
        });
    }


    [RelayCommand]
    private void Browse(SessionSequence sequence)
    {
        sequence.Path = WeakReferenceMessenger.Default.Send(new RequestMessage<string>()).Response;
    }
}
