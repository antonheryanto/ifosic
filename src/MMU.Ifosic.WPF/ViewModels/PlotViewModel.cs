using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using MMU.Ifosic.Models;
using NPOI.SS.Formula.Functions;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Documents;
using TechApps;
using TechApps.ViewModels;

namespace MMU.Ifosic.WPF.ViewModels;

public partial class PlotViewModel : ViewModelBase
{
    [ObservableProperty] private PlotModel _timeModel = new();
    [ObservableProperty] private PlotModel _distanceModel = new();
    [ObservableProperty] private PlotModel _heatmapModel = new();
    [ObservableProperty] private PlotModel _coefficientModel = new();
    [ObservableProperty] private PlotController _timeController = new();
    [ObservableProperty] private PlotController _distanceController = new();
    [ObservableProperty] private PlotController _heatmapController = new();
    [ObservableProperty] private PlotController _coefficientController = new();
    [ObservableProperty] private ObservableCollection<string> _indexes = new();
    [ObservableProperty] private int _index;
    [ObservableProperty] private int _timeIndex = 0;
    [ObservableProperty] private int _distanceIndex = 232;
    [ObservableProperty] private int _distanceStart = 220;
    [ObservableProperty] private int _distanceStop = 350;
    [ObservableProperty] private double _min = -50;
    [ObservableProperty] private double _max = 10;


    public Project Project => Workspace.Instance.Project ?? new();
    private FrequencyShiftDistance? Item;

    public PlotViewModel()
    {
        if (Project.Runner.Sequences.Count > 0 && Project.Items.Count != Project.Runner.Sequences.Count)
        {
            foreach (var sequence in Project.Runner.Sequences)
            {
                var fdd = FrequencyShiftDistance.LoadFolder(sequence.Path);
                if (fdd is not null)
                    Project.Items.Add(fdd);
            }
        }
        if (Project.Items.Count > 0)
            Item = Project.Items[Index];
        TimeController.Unbind(PlotCommands.SnapTrack);
        TimeController.BindMouseDown(OxyMouseButton.Left, new DelegatePlotCommand<OxyMouseDownEventArgs>((view, controller, args) =>
        {
            if (Item is null || Item.MeasurementStart.Count == 0)
                return;
            var p = GetPosition(view, controller, args);
            var d = DateTimeAxis.ToDateTime(p.X);
            TimeIndex = 0;
            for (var i = 1; i < Item.MeasurementStart.Count; i++)
            {
                if (Item.MeasurementStart[i] > d)
                {
                    var extr = d - Item.MeasurementStart[i - 1];
                    var diff = (Item.MeasurementStart[i] - Item.MeasurementStart[i - 1]) / 2;
                    TimeIndex = extr < diff ? i - 1 : i;
                    break;
                }
            }
            DistanceModel = Item.Traces[TimeIndex].PlotLine(Item.Distance, min: Min, max: Max, start: DistanceStart, stop: DistanceStop);
            UpdateTimeIndex(TimeIndex);
        }));
        DistanceController.Unbind(PlotCommands.SnapTrack);
        DistanceController.BindMouseDown(OxyMouseButton.Left, new DelegatePlotCommand<OxyMouseDownEventArgs>((view, controller, args) =>
        {
            if (Item is null || Item.MeasurementStart.Count == 0)
                return;
            var p = GetPosition(view, controller, args);
            var DistanceIndex = 0;
            var less = Item.Distance.Count - DistanceStop;
            for (var i = DistanceStart; i < Item.Distance.Count - less; i++)
            {
                if (Item.Distance[i] > p.X)
                {
                    var extr = p.X - Item.Distance[i - 1];
                    var diff = (Item.Distance[i] - Item.Distance[i - 1]) / 2;
                    DistanceIndex = extr < diff ? i - 1 : i;
                    break;
                }
            }
            TimeModel = Item.Traces.PlotScatter(Item.MeasurementStart, index: DistanceIndex, min: Min, max: Max);
            UpdateDistanceIndex(DistanceIndex - DistanceStart);
        }));
        HeatmapController.Unbind(PlotCommands.SnapTrack);
        HeatmapController.BindMouseDown(OxyMouseButton.Left, new DelegatePlotCommand<OxyMouseDownEventArgs>((view, controller, args) =>
        {
            if (Item is null)
                return;
            var p = GetPosition(view, controller, args);
            DistanceIndex = (int)Math.Round(p.X) + DistanceStart;
            TimeIndex = (int)Math.Round(p.Y);
            TimeModel = Item.Traces.PlotScatter(Item.MeasurementStart, index: DistanceIndex, min: Min, max: Max);
            DistanceModel = Item.Traces[TimeIndex].PlotLine(Item.Distance, min: Min, max: Max, start: DistanceStart, stop: DistanceStop);
        }));

        if (Item is null)
            return;
        Item.BoundaryIndexes = new() { DistanceStart, DistanceStop };
        var date = new DateTime(2023, 06, 23, 11, 40, 0);
        Item.References["Temperature"] = new()
        {
            (date, 26),
            (date.AddMinutes(20), 30),
            (date.AddMinutes(40), 30),
            (date.AddMinutes(60), 40),
            (date.AddMinutes(80), 40),
            (date.AddMinutes(100), 50),
            (date.AddMinutes(120), 50),
            (date.AddMinutes(140), 60),
            (date.AddMinutes(160), 60),
        };
        Plot();
        OnTimeIndexChanged(TimeIndex);
        OnDistanceIndexChanged(DistanceIndex);
    }

    // TODO still not working on
    void UpdateTimeIndex(int value)
    {
        if (TimeModel.Annotations[0] is PointAnnotation tpa && TimeModel.Series[0] is ScatterSeries ss)
        {
            tpa.X = ss.Points[value].X;
            tpa.Y = ss.Points[value].Y;
        }
        TimeModel.InvalidatePlot(true);
    }

    // TODO still not working on
    void UpdateDistanceIndex(int value)
    {
        if (DistanceModel.Annotations[0] is PointAnnotation dpa && DistanceModel.Series[0] is LineSeries ls)
        {
            dpa.X = ls.Points[value].X;
            dpa.Y = ls.Points[value].Y;
        }
        DistanceModel.InvalidatePlot(true);
    }


    private static DataPoint GetPosition(IPlotView view, IController controller, OxyMouseDownEventArgs args)
    {
        controller.AddMouseManipulator(view, new TrackerManipulator(view)
        {
            //FiresDistance = 2.0,
            CheckDistanceBetweenPoints = true,
        }, args);
        var x = view.ActualModel.Axes.FirstOrDefault(w => w.Key == "x");
        var y = view.ActualModel.Axes.FirstOrDefault(w => w.Key == "y");
        return Axis.InverseTransform(args.Position, x, y);
    }

    partial void OnIndexChanged(int value)
    {
        Item = Project.Items[value];
        Plot();
    }

    [RelayCommand]
    private void LoadFile()
    {
        var file = WeakReferenceMessenger.Default.Send(new FileDialogMessage()).Response.FirstOrDefault();
        if (string.IsNullOrEmpty(file))
            return;
        var fdd = FrequencyShiftDistance.Load(file);
        if (fdd is null)
            return;
        // convert old data
        if (Path.GetExtension(file) == ".zip")
            fdd.Save(@$"C:\Projects\MMU\projects\{Path.GetFileNameWithoutExtension(file)}.bin");
        Item = fdd;
        Plot();
    }

    [RelayCommand]
    private void LoadFolder()
    {
        var path = WeakReferenceMessenger.Default.Send(new RequestMessage<string>()).Response;
        if (string.IsNullOrEmpty(path))
            return;
        var fdd = FrequencyShiftDistance.LoadFolder(path);
        if (fdd is null)
            return;
        //fdd.Save(@$"C:\Projects\MMU\projects\{Path.GetFileNameWithoutExtension(path)}.bin");
        Item = fdd;
        Plot();
    }

    private void Plot()
    {
        if (Item is null)
            return;
        HeatmapModel = Item.PlotHeatmap(max: Max, min: Min, start:DistanceStart, stop: DistanceStop);
        DistanceModel = Item.Traces[TimeIndex].PlotLine(Item.Distance, max: Max, min: Min, start: DistanceStart, stop: DistanceStop);
        TimeModel = Item.Traces.PlotScatter(Item.MeasurementStart, index: DistanceIndex, max: Max, min: Min);
        CoefficientModel = CoefficientPlot();
    }

    private PlotModel CoefficientPlot(string measurement = "Temperature")
    {
        var data = new Characterisation(Item, measurement: measurement);
        var model = new PlotModel { Title = "Fiber Coefficient" };
        var line = new LineSeries
        {
            XAxisKey = "x",
            Color = OxyColors.Black
        };
        var scatter = new ScatterSeries
        {
            MarkerType = MarkerType.Diamond,
            MarkerFill = OxyColors.White,
            MarkerStroke = OxyColors.Green,
            MarkerStrokeThickness = 1,
            MarkerSize = 4,
        };

        for (int j = 0; j < data.RegressionPoints.Count; j++)
        {
            var d = data.RegressionPoints[j];
            line.Points.Add(new DataPoint(d[0], d[1]));
        }

        var min = data.ReferencePoints.Count == 0 ? 0 : data.ReferencePoints[0][0] - 10;
        var max = data.ReferencePoints.Count == 0 ? 0 : data.ReferencePoints[^1][0] + 10;

        foreach (var p in line.Points)
            scatter.Points.Add(new(p.X, p.Y));
        
        model.Series.Add(line);
        model.Series.Add(scatter);
        model.Axes.Add(new LinearAxis
        {
            Key = "x",
            Title = measurement,
            Minimum = min,
            Maximum = max,
            Position = AxisPosition.Bottom
        });
        model.Axes.Add(new LinearAxis
        {
            Key = "y",
            Title = "Frequency Shift (GHz)",
            MajorStep = 10,
            MinorStep = 2,
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = OxyColors.LightGray,
            Minimum = -50,
            Maximum = 20,
            Position = AxisPosition.Left
        });
        if (line.Points.Count == 0)
            return model;

        var tap = line.Points[line.Points.Count / 2];
        model.Annotations.Add(new TextAnnotation
        {
            Text = $"Fiber Coefficient = {data.Slope:0.00}",
            TextPosition = new DataPoint(tap.X + 5, tap.Y + 5),
        });
        return model;
    }
}
