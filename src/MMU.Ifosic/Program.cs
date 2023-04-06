using MathNet.Numerics;
using MMU.Ifosic;
using MMU.Ifosic.Models;
using OxyPlot;
using OxyPlot.WindowsForms;
using System.Runtime.Intrinsics.X86;
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
Signal.GetBoundary(fdd, Path.Combine(path, "model.onnx"));
//fdd.Save($@"C:\Projects\MMU\projects\{id}.bin");
fdd.ToMessagePack($@"C:\Projects\MMU\ifosic\src\Python\Set0{id}.msgpack");


var transpose = new double[fdd.Distance.Count][];
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

var distanceSmooth = Signal.Convolution(distanceDict, 4);

// get freq on certain distance for range of time
//var distanceIndex = new int[748]; // fdd.BoundaryIndexes[2] + 5;
var plotFT = new PlotView();
var projectY = new Dictionary<double, int>();  
for (int i = 0; i < distanceIndex.Count; i++)
{
    var freqByTime = new double[fdd.Traces.Count];
    for (int j = 0; j < fdd.Traces.Count; j++)
    {
        var v = fdd.Traces[j][distanceIndex[i]];
		freqByTime[j] = v;
        if (!projectY.TryAdd(v, 1))
            projectY[v]++;
	}
	//plotFT.PlotScatter(freqByTime, color: OxyColors.Purple);
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



var groups = candidates.GroupBy(x => x[0]).ToList();
var averages = new Dictionary<double, double>();
var projectX = new Dictionary<double, double>();
var avDistance = new List<double>();
int g = 0;
foreach (var group in groups)
{
    var sum = 0d;
    var n = 0;
    foreach (var v in group)
    {
        sum += v[1];
		n++;
	}
    projectX[group.Key] = sum;
	averages[group.Key] = sum / n;
    // get distance of neightboard point
    if (g > 0)
        avDistance.Add(MathNet.Numerics.Distance.Euclidean(
            new double[] { g - 1, averages[g - 1] }, new double[] { g, averages[g] }));
	g++;
}
avDistance.Add(0);


// get freq vs distance
// get group with 
//var times = new int[] { 26, 35, 45, 46, 65, 83 };
//var times = boundaryDistance.Keys.ToArray();
//var plotFD = new PlotView();
//var ftd = new double[times.Length];
//var bIndex = 1;
//for (int j = 0; j < ftd.Length; j++)
//{
//    var tx = times[j];
//    for (int i = fdd.BoundaryIndexes[bIndex]; i < fdd.BoundaryIndexes[bIndex+1]; i++)
//    {
//        ftd[j] += fdd.Traces[tx][i];
//    }
//    ftd[j] /= fdd.BoundaryIndexes[bIndex+1] - fdd.BoundaryIndexes[bIndex];
//    plotFD.PlotLine(fdd.Traces[tx]);
//}

var freqKeys = projectY.Keys.ToList().OrderBy(x => x);
var freqValues = new List<double>();
var freqTimes = new List<double>();
foreach (var key in freqKeys)
{
    freqTimes.Add(key);
    freqValues.Add(projectY[key]);
}

var plots = new PlotView[] {
    new PlotView()
         .PlotLine(distanceDict)
         .PlotLine(distanceSmooth)
         .PlotBoundary(fdd.BoundaryIndexes),
    new PlotView()
        .PlotLine(averages.Values.ToArray()),
    //    .PlotLine(avDistance)
  //      .PlotLine(conv)
  //      .PlotLine(convDist)
		//.PlotScatter(averagePoint),
    //new PlotView().PlotLine(freqValues, freqTimes, min: 0),
  //  new PlotView().PlotScatter(candidates)
  //      .PlotScatter(averages.Values.ToArray(), color: OxyColors.Blue)
		//.PlotLine(avDistance),
    //new PlotView().PlotLine(predictedBoundary, min: 0).PlotBoundary(fdd.BoundaryIndexes),
    //new PlotView().PlotLine(boundaryDistance.Values.ToArray(), min: 0).PlotBoundary(fdd.BoundaryIndexes),
    //new PlotView().PlotLine(distances, min: 0).PlotBoundary(fdd.BoundaryIndexes)
    //new PlotView().PlotHeatmap(fdd, max: 25),
    //new PlotView().PlotScatter(freqByTime)
        //.PlotScatter(ftd, times, color: OxyColors.Purple),
    //plotFT,
	//plotFD.PlotBoundary(fdd.Boundaries),
    //new PlotView().PlotScatter(fdd.Distance),
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
