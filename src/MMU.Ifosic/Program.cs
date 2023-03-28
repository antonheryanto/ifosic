using MMU.Ifosic;
using MMU.Ifosic.Models;
using MMU.Ifosic.Web.Models;
using OxyPlot;
using OxyPlot.WindowsForms;
using static System.Net.WebRequestMethods;

var id = 1;
//var path = $@"C:\\Projects\\MMU\\Set0{id}";
//var fdd = FrequencyShiftDistance.Load($"{path}.zip");
//fdd.AddReference($@"{path}\\pressure.csv");
//fdd.Save($@"C:\Projects\MMU\projects\{id}.bin");
//fdd.ToMessagePack($@"C:\Projects\MMU\ifosic\src\Python\Set0{id}.msgpack");
var fdd = FrequencyShiftDistance.Load($@"C:\Projects\MMU\projects\{id}.bin");

// get the distance
var transpose = new double[fdd.Distance.Count][];
var distances = new double[fdd.Distance.Count - 1];
for (int i = 0; i  < fdd.Distance.Count; i++)
{
    transpose[i] = new double[fdd.Traces.Count];
    for (int j = 0; j < fdd.Traces.Count; j++)
    {
        transpose[i][j] = fdd.Traces[j][i];
	}
    if (i == 0)
        continue;
     distances[i-1] = MathNet.Numerics.Distance.Euclidean(transpose[i-1], transpose[i]);
}

var predictedBoundary = Signal.Inference(transpose, @"C:\\Projects\\MMU\\Ifosic\\src\\Python\model.onnx");
var boundaryDistance = new Dictionary<int, double>();
for (int i = 0; i < predictedBoundary.Length - 1; i++)
{
    // f2
    if (i > fdd.BoundaryIndexes[1] && i < fdd.BoundaryIndexes[2] && predictedBoundary[i] < 2)
    {
        var distance = MathNet.Numerics.Distance.Euclidean(transpose[i - 1], transpose[i]);
        if (distance > 11)
            continue;
        boundaryDistance.Add(i, distance);
	}
}


// get freq on certain distance for range of time
//var distanceIndex = new int[748]; // fdd.BoundaryIndexes[2] + 5;
var distanceIndex = boundaryDistance.Keys.ToArray();
var plotFT = new PlotView();
for (int i = 0; i < distanceIndex.Length; i++)
{
    var freqByTime = new double[fdd.Traces.Count];
    for (int j = 0; j < fdd.Traces.Count; j++)
        freqByTime[j] = fdd.Traces[j][distanceIndex[i]];
	plotFT.PlotScatter(freqByTime, color: OxyColors.Purple);
}

// get freq vs distance
// get group with 
//var times = new int[] { 26, 35, 45, 46, 65, 83 };
var times = boundaryDistance.Keys.ToArray();
var plotFD = new PlotView();
var ftd = new double[times.Length];
var bIndex = 1;
for (int j = 0; j < ftd.Length; j++)
{
    var tx = times[j];
    for (int i = fdd.BoundaryIndexes[bIndex]; i < fdd.BoundaryIndexes[bIndex+1]; i++)
    {
        ftd[j] += fdd.Traces[tx][i];
    }
    ftd[j] /= fdd.BoundaryIndexes[bIndex+1] - fdd.BoundaryIndexes[bIndex];
    plotFD.PlotLine(fdd.Traces[tx]);
}


var plots = new PlotView[] {
    //new PlotView().PlotLine(predictedBoundary, min: 0).PlotBoundary(fdd.BoundaryIndexes),
    //new PlotView().PlotLine(boundaryDistance.Values.ToArray(), min: 0).PlotBoundary(fdd.BoundaryIndexes),
    //new PlotView().PlotLine(distances, min: 0).PlotBoundary(fdd.BoundaryIndexes)
    //new PlotView().PlotHeatmap(fdd, max: 25),
    //new PlotView().PlotScatter(freqByTime)
        //.PlotScatter(ftd, times, color: OxyColors.Purple),
    plotFT,
	//plotFD.PlotBoundary(fdd.Boundaries),
    //new PlotView().PlotScatter(fdd.TemperatureEnd.ToArray()),
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
