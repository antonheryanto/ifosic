using Neubrex.Neubrescope.UserProgramming;
using System;
using System.Diagnostics;
using System.IO;

namespace MMU.Ifosic.Neubrex;

public class Session : IDisposable
{
    private readonly NbxNeubrescope neubrescope;
    private int _port = 1;
    public Session()
    {
        neubrescope = new ("MMU Ifosic", "127.0.0.1", 8315);
        neubrescope.Measurement.ExecutionStarted += Measurement_ExecutionStarted;
        neubrescope.Measurement.ExecutionFinished += Measurement_ExecutionFinished;

        var ms = new NbxMeasurementSettings
        {
            MeasurementFeature = NbxMeasurementFeature.Standard,
            MeasurementMethod = NbxMeasurementMethod.TwCotdr,
            CapturedData = NbxCapturedData.Spectrum,
            AtModeMeasurementType = NbxAtModeMeasurementType.Initial,
            DataProcessTiming = NbxDataProcessTiming.RealTime
        };
        neubrescope.Session.Route.SetMeasurementSettings(ms);
        var hs = new NbxHardwareSettings
        {
            DistanceRange = NbxDistanceRange.Range50m,
            SpatialResolution = NbxSpatialResolution.Resolution20cm,
        };
        hs.FrequencyRange.Start_GHz = 194000.000;
        hs.FrequencyRange.Step_GHz = 0.25;
        hs.FrequencyRange.Count = 1201;
        neubrescope.Session.Route.SetHardwareSettings(NbxMeasurementType.TwCotdr, hs);        
    }

    private void Measurement_ExecutionFinished(object sender, NbxExecutionFinishedEventArgs e)
    {
        Debug.WriteLine("ExecutionFinished");
        // saving
        var ss = new NbxSaveMeasurementResultSettings();
        var name = $"F{_port}";
        // if not exist run this first
        //ss.BaseName = $"{name}_Initial";
        //ss.SetIsSaved(NbxMeasurementType.TwCotdr, true);
        // after initial set basename
        var date = neubrescope.Result.MeasurementResult.GetResultProperties(NbxMeasurementType.TwCotdr).StartTime;
        ss.BaseName = $"{name}_{date:yyyyMMdd-HHmmss}";
        ss.SetIsSaved(NbxAnalysisType.TwCotdrFrequencyDifference, true);
        neubrescope.Result.SaveMeasurementResult(ss);
        Debug.WriteLine("ResultSaved:");
    }

    private void Measurement_ExecutionStarted(object sender, NbxExecutionStartedEventArgs e)
    {
        Debug.WriteLine("ExecutionStarted");
    }

    public void Calculate(int port = 1)
    {
        _port = port;
        var os = new NbxOpticalSwitchSettings { PortNumber = port };
        neubrescope.Session.Route.SetOpticalSwitchSettings(os);
        var rawPath = neubrescope.Session.GetSessionSettings().RawDataDirectory;
        var ans = new NbxAnalysisSettings();
        ans.TwCotdr.InitialFilePath = Path.Combine(rawPath, $"F{port}_Initial_MEAS.rgb");
        neubrescope.Session.Route.SetAnalysisSettings(ans);
        // Calling the StartRoute method in the NbxNeubrescope.Measurement object
        // starts the measurement for the configured route.
        neubrescope.Measurement.StartRoute();
    }

    public void Dispose()
    {
        neubrescope.Measurement.ExecutionStarted -= Measurement_ExecutionStarted;
        neubrescope.Measurement.ExecutionFinished -= Measurement_ExecutionFinished;
        neubrescope?.Dispose();
    }
}
