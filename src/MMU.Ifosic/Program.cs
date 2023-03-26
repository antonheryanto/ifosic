using MMU.Ifosic;
using MMU.Ifosic.Models;
using OxyPlot;
using OxyPlot.WindowsForms;

var fdd = FrequencyShiftDistance.Load(@"C:\Projects\MMU\Set03.zip");
//fdd.GetBoundary(@"C:\Projects\MMU\Set02\Set02_Results.txt");
//fdd.AddReference(@"C:\Projects\MMU\Set01\pressure.csv");
//fdd.Save(@"C:\Projects\MMU\projects\3.bin");
//fdd.ToMessagePack(@"C:\Projects\MMU\ifosic\src\Python\Set01.msgpack");

// get freq on certain distance for range of time
var index = fdd.BoundaryIndexes[2] + 5;
//var index = 100;
var freqByTime = new double[fdd.Traces.Count];
for (int j = 0; j < fdd.Traces.Count; j++)
    freqByTime[j] = fdd.Traces[j][index];

// get freq vs distance
// get group with 
//var times = new int[] { 26, 35, 45, 46, 65, 83 };
var times = new int[] { 500 };
var plotFD = new PlotView();
var ftd = new double[times.Length];
for (int j = 0; j < ftd.Length; j++)
{
    var tx = times[j];
    for (int i = fdd.BoundaryIndexes[2]; i < fdd.BoundaryIndexes[3]; i++)
    {
        ftd[j] += fdd.Traces[tx][i];
    }
    ftd[j] /= fdd.BoundaryIndexes[3] - fdd.BoundaryIndexes[2];
    OxyPlotExtensions.PlotLine(plotFD, fdd.Traces[tx], fdd.Distance);
}


var plots = new PlotView[] {
    new PlotView().PlotHeatmap(fdd, max: 25),
    new PlotView().PlotScatter(freqByTime).PlotScatter(ftd, times, color: OxyColors.Purple),
    plotFD.PlotBoundary(fdd.Boundaries),
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
