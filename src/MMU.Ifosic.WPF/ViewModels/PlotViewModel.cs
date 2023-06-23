using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.Mvvm.Messaging;
using MMU.Ifosic;
using MMU.Ifosic.Models;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechApps.ViewModels;
using TechApps;
using System.IO;

namespace MMU.Ifosic.WPF.ViewModels;

public partial class PlotViewModel : ViewModelBase
{
    [ObservableProperty] PlotModel _model = new();

    public PlotViewModel()
    {
        LoadFile();
    }

    private void LoadFile()
    {
        var file = WeakReferenceMessenger.Default.Send(new FileDialogMessage()).Response.FirstOrDefault();
        if (string.IsNullOrEmpty(file))
            return;
        var fdd = FrequencyShiftDistance.Load(file);
        if (fdd is null)
            return;
        if (Path.GetExtension(file) == ".zip")
            fdd?.Save(@$"C:\Projects\MMU\projects\{Path.GetFileNameWithoutExtension(file)}.bin");
        Model = fdd.PlotHeatmap();
    }

    private void LoadFolder()
    {
        var path = WeakReferenceMessenger.Default.Send(new RequestMessage<string>()).Response;
        if (string.IsNullOrEmpty(path))
            return;
    }

    private void Load()
    {
        // 653
        // 913
        var fdd = FrequencyShiftDistance.Load(@"C:\Projects\MMU\projects\1.bin");
        var data = new double[fdd.Distance.Count, fdd.Traces.Count];
        var min = double.MaxValue;
        var max = double.MinValue;
        for (int i = 0; i < data.GetLength(0); i++)
        {
            for (int j = 0; j < data.GetLength(1); j++)
            {
                var v = fdd.Traces[j][i];
                data[i, j] = v;
                //if (j < 653 || j > 913)
                //    continue;
                if (max < v)
                    max = v;
                if (min > v)
                    min = v;
            }
        }
        var boundries = new double[] { 33.57, 35.21, 38.45, 41.74, 45.12, 46.82 };
        var indexes = new int[boundries.Length];
        for (int i = 0, j = 0; i < fdd.Distance.Count; i++)
        {
            if (j == indexes.Length)
                break;
            double v = boundries[j];
            if (fdd.Distance[i] > v)
            {
                indexes[j] = i;
                j++;
            }
        }
        //var maxAbs = Math.Max(Math.Abs(max), Math.Abs(min));
        var maxAbs = 10;
        Model = PlotHeatmap(data, maxAbs, indexes);
    }

    private static readonly OxyPalette _palette = OxyPalettes.Rainbow(500);

    public PlotModel PlotHeatmap(double[,] data, double max, int[] boundries, string title = "Plot Heatmap", string titleXAxis = "x")
    {
        var model = new PlotModel { Title = title  };
        model.Axes.Add(new LinearColorAxis
        {
            Key = "linear",
            Minimum = -max,
            Maximum = max,
            Palette = _palette,
            RenderAsImage = true,
            Position = AxisPosition.None
        });
        model.Axes.Add(new LinearAxis
        {
            Key = "x",
            AbsoluteMinimum = 0,
            AbsoluteMaximum = data.GetLength(0) - 1,
            Title = titleXAxis,
            //TicklineColor = OxyColors.White,
            //TextColor = OxyColors.White,
            Position = AxisPosition.Bottom
        });
        model.Axes.Add(new LinearAxis
        {
            Key = "y",
            AbsoluteMinimum = 0,
            AbsoluteMaximum = data.GetLength(1) - 1,
            //Title = titleYAxis,
            StartPosition = 1,
            EndPosition = 0,
            //TicklineColor = OxyColors.White,
            //TextColor = OxyColors.White,
            Position = AxisPosition.Left
        });
        //// for display only
        model.Axes.Add(new LinearColorAxis
        {
            Key = "reverse",
            Minimum = -max,
            Maximum = max,
            //MajorStep = 10,
            Palette = OxyPalettes.Rainbow(512),
            RenderAsImage = false,
            Position = AxisPosition.Right
        });
        //model.Axes.Add(new LinearAxis
        //{
        //    Key = "axis_bottom",
        //    Minimum = 0,
        //    Maximum = data.GetLength(0) * xScale,
        //    Position = AxisPosition.Bottom,
        //});
        //model.Axes.Add(new LinearAxis
        //{
        //    Key = "axis_left",
        //    Minimum = 0,
        //    Maximum = data.GetLength(1) * yScale,
        //    StartPosition = 1,
        //    EndPosition = 0,
        //    Position = AxisPosition.Left,
        //});
        model.Series.Add(new HeatMapSeries
        {
            XAxisKey = "x",
            //YAxisKey = "y",
            ColorAxisKey = "linear",
            X0 = 0,
            X1 = data.GetLength(0),
            Y0 = 0,
            Y1 = data.GetLength(1),
            Interpolate = true,
            RenderMethod = HeatMapRenderMethod.Bitmap,
            Data = data,
        });
        for (int i = 1; i < boundries.Length; i++)
        {
            var a = new RectangleAnnotation
            {
                Fill = OxyColor.FromAColor(50, i % 2 == 0 ? OxyColors.Magenta : OxyColors.Yellow),
                MinimumY = 0,
                MaximumY = data.GetLength(0) - 1,
                MinimumX = boundries[i - 1],
                MaximumX = boundries[i],
                Layer = AnnotationLayer.AboveSeries
            };
            model.Annotations.Add(a);
        }
        model.InvalidatePlot(true);
        return model;
    }
}
