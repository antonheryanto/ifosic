﻿using MathNet.Numerics;
using Microsoft.ML.OnnxRuntime.Tensors;
using MMU.Ifosic;
using MMU.Ifosic.Models;
using OxyPlot;
using OxyPlot.WindowsForms;
using static System.Net.WebRequestMethods;

var id = 4;
int fiberId = 1;
//var path = $@"C:\\Projects\\MMU\\Set0{id}";
var path = $@"C:\Projects\MMU\projects\";
//var fdd = FrequencyShiftDistance.Load($"{path}.zip");
//fdd.AddReference($@"{path}\\pressure.csv");
//fdd.Save($@"C:\Projects\MMU\projects\{id}.bin");
var fdd = FrequencyShiftDistance.Load($@"C:\Projects\MMU\projects\{id}.bin");
//fdd.AddReference(@"C:\Projects\MMU\Set01\Set01_Pressure vs Time.xlsx");
// get the distance
var distanceIndex = new List<int>();
Signal.GetBoundary(fdd, Path.Combine("C:\\Projects\\MMU\\Ifosic\\src\\Python", "model.onnx"));
//fdd.Save($@"C:\Projects\MMU\projects\{id}.bin");
//fdd.ToMessagePack($@"C:\Projects\MMU\ifosic\src\Python\Set0{id}.msgpack");

bool start = false;
for (int i = 1; i < fdd.Categories.Count; i++)
{
    var cat = fdd.Categories[i];
    if (cat < 0)
        continue;

    if (!start)
    {
        if (cat == 2)
            start = true;
        continue;
    }

    if (cat > 1)
        continue;

    start = false;
    //var distance = MathNet.Numerics.Distance.Euclidean(transpose[i - 1], transpose[i]);
    distanceIndex.Add(i);
}

var distanceDict = new double[fdd.Distance.Count];
var transpose = new double[fdd.Distance.Count][];
for (int i = 0; i < distanceIndex.Count - 1; i++)
{
    for (int j = distanceIndex[i], k = 0; j < distanceIndex[i + 1]; j++, k++)
    {
        if (transpose[j] is null || transpose[k].Length == 0)
        {
            transpose[j] = new double[fdd.Traces.Count];
            for (int l = 0; l < fdd.Traces.Count; l++)
                transpose[j][l] = fdd.Traces[l][j];
        }

        if (k == 0)
            continue;
        distanceDict[j - 1] = MathNet.Numerics.Distance.Euclidean(transpose[j - 1], transpose[j]);
    }
}


var candidates = new List<double[]>();
// loop location within boundary of targeted fiber
for (int i = fdd.BoundaryIndexes[fiberId - 1]; i < fdd.BoundaryIndexes[fiberId]; i++)
{
	// aggregate only good signal
	if (fdd.Categories[i] != 1)
		continue;

    // get each time freq at that location
    for (int j = 0; j < fdd.Traces.Count; j++)
    {
        candidates.Add(new double[] { j, fdd.Traces[j][i] });
    }
}


var plots = new PlotView[] {
    //new PlotView().PlotHeatmap(ng.GetItem(ix[600]), max: 1),
    //new PlotView().PlotHeatmap(fdd, max: 25),
    //new PlotView()
    //     .PlotLine(distanceDict)
    //     .PlotLine(distanceSmooth)
    //     .PlotBoundary(fdd.BoundaryIndexes),
    //new PlotView()
    //    .PlotLine(conv)
    //    .PlotLine(convDist)
    //    .PlotScatter(averagePoint),
};

var t = new TableLayoutPanel
{
    Location = new System.Drawing.Point(0, 0),
    Dock = DockStyle.Fill,
    RowCount = plots.Length,
};
var rowSize = 100F / t.RowCount;
for (int i = 0; i < plots.Length; i++)
{
    t.RowStyles.Add(new RowStyle(SizeType.Percent, rowSize));
    t.Controls.Add(plots[i]);
}
var f = new Form() { Text = "Hello NativeAOT!", Width = 1024, Height = 1024 };
f.Controls.Add(t);
Application.Run(f);
