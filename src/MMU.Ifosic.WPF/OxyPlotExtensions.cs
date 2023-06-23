using MMU.Ifosic.Models;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.SkiaSharp.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
//using OxyPlot.WindowsForms;

namespace MMU.Ifosic;

public static class OxyPlotExtensions
{
	public static PlotView PlotScatter(this PlotView view, IList<Group> data, OxyColor? color = null)
	{
		var model = view.Model ?? new PlotModel { Title = "Freq vs Time" };
		color ??= OxyColors.Red;
		var scatters = new ScatterSeries
		{
			MarkerType = MarkerType.Circle,
			MarkerFill = OxyColors.White,
			MarkerStroke = color.Value,
			MarkerStrokeThickness = 1,
			MarkerSize = 1,
		};

		// get freq on certain distanct for range of time
		for (int j = 0; j < data.Count; j++)
		{
			scatters.Points.Add(new ScatterPoint(data[j].Start, data[j].Stop, 1, data[j].Id));
		}

		model.Series.Add(scatters);
		if (view.Model is not null)
			return view;

		model.Axes.Add(new LinearAxis
		{
			Title = "Times",
			Minimum = 0,
			Position = AxisPosition.Bottom
		});
		model.Axes.Add(new LinearAxis
		{
			Title = "Freq",
			Minimum = -5,
			Maximum = 30,
			Position = AxisPosition.Left
		});

		view.Model = model;
		//view.Dock = DockStyle.Fill;
		return view;
	}

	public static PlotView PlotScatter(this PlotView view, IList<double[]> data, OxyColor? color = null)
	{
		var model = view.Model ?? new PlotModel { Title = "Freq vs Time" };
		color ??= OxyColors.Red;
		var scatters = new ScatterSeries
		{
			MarkerType = MarkerType.Circle,
			MarkerFill = OxyColors.White,
			MarkerStroke = color.Value,
			MarkerStrokeThickness = 1,
			MarkerSize = 3,
		};

		// get freq on certain distanct for range of time
		for (int j = 0; j < data.Count; j++)
		{			
			scatters.Points.Add(new ScatterPoint(data[j][0], data[j][1]));
		}

		model.Series.Add(scatters);
		if (view.Model is not null)
			return view;

		model.Axes.Add(new LinearAxis
		{
			Title = "Times",
			Minimum = 0,
			Position = AxisPosition.Bottom
		});
		model.Axes.Add(new LinearAxis
		{
			Title = "Freq",
			Minimum = -5,
			Maximum = 30,
			Position = AxisPosition.Left
		});

		view.Model = model;
		//view.Dock = DockStyle.Fill;
		return view;
	}

	public static PlotModel PlotScatter(this double[] y, int[]? x = null, OxyColor? color = null)
    {
		var model = new PlotModel { Title = "Freq vs Time" };
		color ??= OxyColors.Red;
		var scatters = new ScatterSeries
		{
			MarkerType = MarkerType.Circle,
			MarkerFill = OxyColors.White,
			MarkerStroke = color.Value,
			MarkerStrokeThickness = 1,
			MarkerSize = 3,
		};

		// get freq on certain distanct for range of time
		for (int j = 0; j < y.Length; j++)
		{
			var v = x is not null && x.Length == y.Length ? x[j] : j;
			scatters.Points.Add(new ScatterPoint(v, y[j]));
		}

		model.Series.Add(scatters);
		if (view.Model is not null)
            return view;

		model.Axes.Add(new LinearAxis
		{
			Title = "Times",
            Minimum = 0,
			Position = AxisPosition.Bottom
		});
		model.Axes.Add(new LinearAxis
		{
			Title = "Freq",
            Minimum = -10,
            Maximum = 10,
			Position = AxisPosition.Left
		});

        view.Model = model;
		//view.Dock = DockStyle.Fill;
        return view;
    }

    public static PlotView PlotBoundary(this PlotView view, IList<double> boundaries)
    {
        var model = view.Model;
        var max = model.GetAxis("y").Maximum;
        for (int i = 1; i < boundaries.Count; i++)
        {
            var a = new RectangleAnnotation
            {
                Fill = OxyColor.FromAColor(50, i % 2 == 0 ? OxyColors.Magenta : OxyColors.Yellow),
                MinimumY = -max,
                MaximumY = max,
                MinimumX = boundaries[i - 1],
                MaximumX = boundaries[i],
                Layer = AnnotationLayer.AboveSeries
            };
            model.Annotations.Add(a);
        }
        return view;
    }

	public static PlotView PlotBoundary(this PlotView view, IList<int> boundaries)
	{
		var model = view.Model;
		var max = model.GetAxis("y").Maximum;
		for (int i = 1; i < boundaries.Count; i++)
		{
			var a = new RectangleAnnotation
			{
				Fill = OxyColor.FromAColor(50, i % 2 == 0 ? OxyColors.Magenta : OxyColors.Yellow),
				MinimumY = -max,
				MaximumY = max,
				MinimumX = boundaries[i - 1],
				MaximumX = boundaries[i],
				Layer = AnnotationLayer.AboveSeries
			};
			model.Annotations.Add(a);
		}
		return view;
	}

	public static PlotView PlotLine(this PlotView view, IList<double> x, IList<double>? y = null, double max = 0, double min=-9999)
    {
        var model = view.Model ?? new PlotModel { Title = "Freq vs Distance" };        
        var line = new LineSeries { };
        for (int i = 0; i < x.Count; i++)
        {
            if (y is null)
				line.Points.Add(new DataPoint(i, x[i]));
            else
				line.Points.Add(new DataPoint(y[i], x[i]));
		}
        model.Series.Add(line);
        
        if (max == 0)
            max = x.Select(Math.Abs).Max();
        if (min == -9999)
            min = -max;

        if (view.Model is not null)
            return view;

		model.Axes.Add(new LinearAxis
        {
            Key = "x",
            Title = "Distance (m)",            
            Position = AxisPosition.Bottom
        });
        model.Axes.Add(new LinearAxis
        {
            Key = "y",
            Title = "Frequency Shift",
            Minimum = min,
            Maximum = max,
            Position = AxisPosition.Left
        });

		view.Model = model;
		//view.Dock = DockStyle.Fill;
        return view;
    }

    private static readonly OxyPalette _palette = OxyPalettes.Rainbow(500);

    public static PlotView PlotHeatmap(this PlotView view, double[][] image, double max = 30)
    {
        var model = new PlotModel { Title = "Image" };
        var data = new double[image.Length, image[0].Length];
        for (int i = 0; i < image.Length; i++)
        {
            for (int j = 0; j < data.GetLength(1); j++)
            {
                var v = image[i][j];
                data[i, j] = v;
            }
        }

        model.Axes.Add(new LinearColorAxis
        {
            Key = "linear",
            Minimum = -max,
            Maximum = max,
            Palette = _palette,
            RenderAsImage = true,
            Position = AxisPosition.None
        });

        model.Series.Add(new HeatMapSeries
        {
            //XAxisKey = "x",
            //YAxisKey = "y",
            //ColorAxisKey = "linear",
            X0 = 0,
            X1 = data.GetLength(0),
            Y0 = 0,
            Y1 = data.GetLength(1),
            Interpolate = true,
            RenderMethod = HeatMapRenderMethod.Bitmap,
            Data = data,
        });
        
        model.InvalidatePlot(true);
        view.Model = model;
        //view.Dock = DockStyle.Fill;
        return view;
    }

    public static PlotModel PlotHeatmap(this FrequencyShiftDistance fdd, IList<int>? indexes = null, int start = 0, int stop = 0, double max = 0, string titleXAxis = "Distance (m)", string titleYAxis = "Time(s)")
    {
        var model = new PlotModel { Title = "Heatmap" };
        if (fdd.Distance.Count == 0)
            return model;
        if (stop == 0)
            stop = fdd.Distance.Count;
		var data = new double[stop-start, fdd.Traces.Count];
        var minValue = double.MaxValue;
        var maxValue = double.MinValue;
        for (int i = 0, k = start; k < stop; i++, k++)
        {
            for (int j = 0; j < data.GetLength(1); j++)
            {
                var v = fdd.Traces[j][k];
                data[i, j] = v;                
                if (maxValue < v)
                    maxValue = v;
                if (minValue > v)
                    minValue = v;
            }
        }

        max = max == 0 ? Math.Max(Math.Abs(minValue), Math.Abs(maxValue)) : max;
        var xScale = fdd.Distance[^1] / fdd.Distance.Count;

        model.Axes.Add(new LinearColorAxis
        {
            Key = "linear",
            Title = "Ghz",
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
            TicklineColor = OxyColors.White,
            TextColor = OxyColors.White,
            Position = AxisPosition.Bottom
        });
        model.Axes.Add(new LinearAxis
        {
            Key = "y",
            AbsoluteMinimum = 0,
            AbsoluteMaximum = data.GetLength(1) - 1,
            Title = titleYAxis,
            //StartPosition = 1,
            //EndPosition = 0,
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
        model.Axes.Add(new LinearAxis
        {
            Key = "axis_bottom",
            Minimum = 0,
            Maximum = data.GetLength(0) * xScale,
            Position = AxisPosition.Bottom,
        });
        //model.Axes.Add(new DateTimeAxis
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
        if (indexes is not null)
        {
            for (int i = 1; i < indexes.Count; i++)
            {
                var a = new RectangleAnnotation
                {
                    Fill = OxyColor.FromAColor(50, i % 2 == 0 ? OxyColors.Magenta : OxyColors.Yellow),
                    MinimumY = 0,
                    MaximumY = data.GetLength(0) - 1,
                    MinimumX = indexes[i - 1],
                    MaximumX = indexes[i],
                    Layer = AnnotationLayer.AboveSeries
                };
                model.Annotations.Add(a);
            }
        }
        model.InvalidatePlot(true);
        return model;
    }
}

//static void Plot(params PlotView[] plots)
//{
//	var t = new TableLayoutPanel
//	{
//		Location = new System.Drawing.Point(0, 0),
//		Dock = DockStyle.Fill,
//		RowCount = plots.Length,
//	};
//	var rowSize = 100F / t.RowCount;
//	for (int i = 0; i < plots.Length; i++)
//	{
//		t.RowStyles.Add(new RowStyle(SizeType.Percent, rowSize));
//		t.Controls.Add(plots[i]);
//	}
//	var f = new Form() { Text = "Hello NativeAOT!", Width = 1024, Height = 1024 };
//	f.Controls.Add(t);
//	Application.Run(f);
//}