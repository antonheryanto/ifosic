using MMU.Ifosic;
using MMU.Ifosic.Models;
using OxyPlot.WindowsForms;


var root = $@"C:\Projects\MMU\";
var project = Path.Combine(root, "projects");
var srcPath = Path.Combine(root, "Ifosic\\src\\Python");
var id = 7;
var fiberId = 1;
//var name = Path.Combine(root, $"Set0{id}.zip");
var name = Path.Combine(root, $"Set01_Time_Parallel.zip");
//var name = Path.Combine(project, $"{id}.bin");
var fdd = FrequencyShiftDistance.Load(name);

//var img = Signal.Resize(fdd.Traces);
//var ng = new NumberToGrid(-20, 30, 64);
//var ngC = ng.GetClass(27);

//FrequencyShiftDistance.ToMessagePack(img, Path.Combine(srcPath, $"Set0{id}.msgpack"));
//fdd.AddReference(@"C:\Projects\MMU\Set01\Set01_Pressure vs Time.xlsx");
// get the distance
fdd.Save($@"C:\Projects\MMU\projects\{id}.bin");
//fdd.ToMessagePack($@"C:\Projects\MMU\ifosic\src\Python\Set0{id}.msgpack");

// parallel fiber layout
// time based switch, one iteration, multiple iteration
// time to fiber switch info
// boundary is distance to fiber switch

//Augment(fdd);
return;

var (cxt, dxt) = Signal.GetBoundary(fdd, Path.Combine(srcPath, "model_512_64.onnx"));
var catD = fdd.Categories
		//.Where((s, i) => i > 600 && i < 1250)
		.Select(s => (double)s).ToList();

var plots = new PlotView[] {
    //new PlotView().PlotHeatmap(ng.GetItem(ix[600]), max: 1),
    //new PlotView().PlotHeatmap(fdd, max: 25),
    new PlotView()
		 .PlotLine(dxt)
		 .PlotBoundary(cxt)
		 .PlotLine(catD),
         //.PlotBoundary(fdd.BoundaryIndexes),
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

static void Augment(FrequencyShiftDistance? fdd)
{
	if (fdd is null)
		return;
	var serialTimeIndex = new List<int>();
	var parallelTimeIndex = new List<int>();
	var mid = fdd.Traces.Count / 2;
	for (int i = 0; i < fdd.Traces.Count; i++)
	{
		serialTimeIndex.Add(i < mid ? 1 : 2);
		parallelTimeIndex.Add(i % 2 != 0 ? 2 : 1);
	}
	var fiberIndex = 2;
	var boundaries = new List<double>();
	for (int i = fiberIndex; i < fiberIndex + 3; i++)
	{
		boundaries.Add(fdd.Boundaries[i]);
	}
	// generate parallel set using serial set 1, fiber 3, 4, 5
	// uses boundary as anchor, put fiber one to the selected fiber, append the reminder 

	var serialTraces = new List<double[]>();
	var parallelTraces = new List<double[]>();
	for (int j = 0; j < serialTimeIndex.Count; j++)
	{
		serialTraces.Add(GenerateData(fdd, j, fiberIndex: fiberIndex + serialTimeIndex[j], startIndex: 1, endIndex: 5));
		parallelTraces.Add(GenerateData(fdd, j, fiberIndex: fiberIndex + parallelTimeIndex[j], startIndex: 1, endIndex: 5));
	}
	fdd.ToZip("C:\\Projects\\MMU\\Set01_Time_Serial.zip", serialTraces, serialTimeIndex, boundaries);
	fdd.ToZip("C:\\Projects\\MMU\\Set01_Time_Parallel.zip", parallelTraces, parallelTimeIndex, boundaries);
}

static double[] GenerateData(FrequencyShiftDistance fdd, int timeIndex, int fiberIndex, int startIndex, int endIndex)
{
	var trace = new List<double>();
	for (int i = 0; i < (fdd.BoundaryIndexes[fiberIndex] - fdd.BoundaryIndexes[startIndex]); i++)
		trace.Add(fdd.Traces[timeIndex][i]);
	for (int i = 0; i < fdd.BoundaryIndexes[startIndex]; i++)
		trace.Add(fdd.Traces[timeIndex][i]);
	for (int i = fdd.BoundaryIndexes[fiberIndex]; i < fdd.BoundaryIndexes[fiberIndex + 1]; i++)
		trace.Add(fdd.Traces[timeIndex][i]);
	for (int i = fdd.BoundaryIndexes[endIndex]; i < fdd.Traces[timeIndex].Length; i++)
		trace.Add(fdd.Traces[timeIndex][i]);
	for (int i = (fdd.Traces.Count - (fdd.BoundaryIndexes[endIndex] - fdd.BoundaryIndexes[fiberIndex + 1])); i < fdd.Traces.Count; i++)
		trace.Add(fdd.Traces[timeIndex][i]);
	return trace.ToArray();
}

static double[] BoundaryTest(FrequencyShiftDistance? fdd)
{
	if (fdd is null)
		return Array.Empty<double>();

	var distanceDict = new double[fdd.Distance.Count];
	var distanceIndex = new List<int>();
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

	return distanceDict;
}