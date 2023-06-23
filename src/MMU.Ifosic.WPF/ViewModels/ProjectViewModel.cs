﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using MMU.Ifosic.Models;
using MMU.Ifosic.Neubrex;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TechApps.ViewModels;

namespace MMU.Ifosic.WPF.ViewModels;

public partial class ProjectViewModel : ViewModelBase
{
    [ObservableProperty] private OpticalSwitch _switch = new ();
    [ObservableProperty] private SessionRunner _runner = new ();
    [ObservableProperty] private bool _isStopped = true;
    [ObservableProperty] private int _count = 0;
    private CancellationTokenSource _token = new();

    public Project? Project => Workspace.Instance.Project;
    public List<string> Layouts { get; set; }
    public List<string> Measurements { get; set; }

    public ProjectViewModel()
    {
        Layouts = Project.Layouts.Values.ToList();
        Layouts.Insert(0, "Please choose");
        Measurements = Project.Measurements.Values.ToList();
        Measurements.Insert(0, "Please choose");
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

    private void UpdateSequence()
    {
        var ports = Switch.GetPorts();
        if (Runner.Sequences.Count > 0)
            Runner.Sequences.Clear();

        foreach (var port in ports)
        {
            Runner.Sequences.Add(new SessionSequence { Port = port,
#if DEBUG
                Path = $@"E:\MMU_PRSB_20230614\MMU PRSB 20230614_F{port}"
#endif
            });
        }
    }

    [RelayCommand]
    private void Start()
    {
        //await Switch.RunSerial();
        IsStopped = false;
        _token = new();
        Count = 0;
        for (int i = 0; i < Runner.RepeatCount; i++)
        {
            Count++;
            if (_token.Token.IsCancellationRequested)
                break;
            foreach (var sequence in Runner.Sequences)
            {
                if (string.IsNullOrEmpty(sequence.Path))
                    continue;
                //if (_token.Token.IsCancellationRequested || !Switch.ToPort(sequence.Port))
                //    break;
                //ProgressViewModel.Init(() => Runner.Start(sequence), cancel: _token.Cancel);
                Switch.OutgoingPort = sequence.Port;
                ProgressViewModel.Init(() => Task.Delay(100).Wait(),() => Switch.Logs.Add($"{DateTime.Now}, Running {Count} {sequence.Port}"), cancel: _token.Cancel);
            }
        }
        IsStopped = true;
    }

    [RelayCommand]
    private void Browse(SessionSequence sequence)
    {
        sequence.Path = WeakReferenceMessenger.Default.Send(new RequestMessage<string>()).Response;
    }
}
