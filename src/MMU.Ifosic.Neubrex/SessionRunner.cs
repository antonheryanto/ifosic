using CommunityToolkit.Mvvm.ComponentModel;
using Neubrex.Neubrescope.UserProgramming;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
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

    public void Calculate(Action<int> changePort)
    {
        var neubrescope = new NbxNeubrescope("MMU Ifosic", Address, Port);
        for (int i = 0; i < RepeatCount; i++) {
            foreach (var sequence in Sequences)
            {
                if (!sequence.IsMeasure)
                    continue;
                if (neubrescope.Measurement.IsMeasuring())
                    neubrescope.Measurement.WaitForFinish();
                neubrescope.Session.Open(sequence.Path);
                //var os = new NbxOpticalSwitchSettings { PortNumber = sequence.Port };
                //neubrescope.Session.Route.SetOpticalSwitchSettings(os);
                //neubrescope.Measurement.ExecutionStarted += (s, e) => changePort(sequence.Port);
                neubrescope.Measurement.ExecutionFinished += (s, e) => GetResult(neubrescope);
                changePort(sequence.Port);
                neubrescope.Measurement.StartRoute();
                neubrescope.Measurement.WaitForFinish();
                //neubrescope.Measurement.ExecutionStarted -= (s, e) => changePort(sequence.Port);
                //neubrescope.Measurement.ExecutionFinished -= (s, e) => GetResult(neubrescope);
            }
        }
        neubrescope?.Dispose();
    }

    void GetResult(NbxNeubrescope neubrescope)
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
