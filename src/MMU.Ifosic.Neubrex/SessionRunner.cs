using CommunityToolkit.Mvvm.ComponentModel;
using Neubrex.Neubrescope.UserProgramming;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MMU.Ifosic.Neubrex;

public partial class SessionSequence : ObservableObject
{
    [ObservableProperty] private string _path = "";
    [ObservableProperty] private string _method = "TW";
    [ObservableProperty] private int _port = 1;
    [ObservableProperty] private bool _isMeasure = true;
}

public partial class SessionRunner : ObservableObject
{
    [ObservableProperty] private string _address = "127.0.0.1";
    [ObservableProperty] private int _port = 8315;
    [ObservableProperty] private int _repeatCount = 1;
    [ObservableProperty] private TimeSpan _repeatInterval = new(0, 0, 0);
    [ObservableProperty] private ObservableCollection<SessionSequence> _sequences = new();
    [ObservableProperty] private string _basename = "PF";

    private NbxNeubrescope? neubrescope = null;

    public void Start(SessionSequence sequence)
    {
        if (!sequence.IsMeasure)
            return;
        neubrescope ??= Init();
        if (neubrescope.Measurement.IsMeasuring())
            neubrescope.Measurement.WaitForFinish();
        neubrescope.Session.Open(sequence.Path);
        neubrescope.Measurement.StartRoute();
        neubrescope.Measurement.WaitForFinish();
    }

    private NbxNeubrescope Init()
    {
        NbxNeubrescope neubrescope = new("MMU Ifosic", Address, Port);
        neubrescope.Measurement.ExecutionFinished += Measurement_ExecutionFinished;
        foreach (var sequence in Sequences)
        {
            neubrescope.Session.Open(sequence.Path);
            var port = neubrescope.Session.Route.GetOpticalSwitchSettings().PortNumber;
            if (port != sequence.Port)
            {
                neubrescope.Session.Route.SetOpticalSwitchSettings(new NbxOpticalSwitchSettings { PortNumber = sequence.Port });
                neubrescope.Session.Save();
            }
        }
        return neubrescope;
    }

    ~SessionRunner()
    {
        if (neubrescope is null)
            return;
        neubrescope.Measurement.ExecutionFinished -= Measurement_ExecutionFinished;
        neubrescope.Dispose();
    }


    private void Measurement_ExecutionFinished(object sender, NbxExecutionFinishedEventArgs e)
    {
        if (neubrescope is not null)
            GetResult(neubrescope);
    }

    private void GetResult(NbxNeubrescope neubrescope)
    {
        Debug.WriteLine("ExecutionFinished");
        // saving
        var ss = new NbxSaveMeasurementResultSettings();
        // if not exist run this first
        //ss.SetIsSaved(NbxMeasurementType.TwCotdr, true);
        var date = neubrescope.Result.MeasurementResult.GetResultProperties(NbxMeasurementType.TwCotdr).StartTime;
        ss.BaseName = $"{Basename}_{date:yyyyMMdd-HHmmss}";
        ss.SetIsSaved(NbxAnalysisType.TwCotdrFrequencyDifference, true);
        neubrescope.Result.SaveMeasurementResult(ss);
        Debug.WriteLine("ResultSaved:");
    }

}
