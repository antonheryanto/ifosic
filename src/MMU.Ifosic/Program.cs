using MMU.Ifosic;
using MMU.Ifosic.Models;
using NPOI.SS.Formula.Functions;
using OxyPlot.WindowsForms;


var root = $@"C:\Projects\MMU\";
var project = Path.Combine(root, "projects");
var srcPath = Path.Combine(root, "Ifosic\\src\\Python");
var id = 2;
var fiberId = 1;
//var name = Path.Combine(root, $"Set0{id}.zip");
//var name = Path.Combine(root, $"Set01_Time_Serial.zip");
//var name = Path.Combine(root, $"Set01_Time_Parallel.zip");
//var name = Path.Combine(project, $"{id}.bin");
//var fdd = FrequencyShiftDistance.Load(name);
//FrequencyShiftDistance.ToMessagePack(img, Path.Combine(srcPath, $"Set0{id}.msgpack"));
//fdd.AddReference(@"C:\Projects\MMU\Set01\Set01_Pressure vs Time.xlsx");
//fdd.ToMessagePack($@"C:\Projects\MMU\ifosic\src\Python\Set0{id}.msgpack");
//fdd.Save($@"C:\Projects\MMU\projects\{id}.bin");

Augmentation.Create(FrequencyShiftDistance.Load(Path.Combine(project, $"{id}.bin")), Path.Combine(root, $"Set{id}_"),
	@$"C:\Projects\MMU\Set0{id}\Set0{id}_Pressure vs Time.xlsx");

//var img = Signal.Resize(fdd.Traces);
//var ng = new NumberToGrid(-20, 30, 64);
//var ngC = ng.GetClass(27);
//var (cxt, dxt) = Signal.GetBoundary(fdd, Path.Combine(srcPath, "model_512_64.onnx"));
//var catD = fdd.Categories
//		//.Where((s, i) => i > 600 && i < 1250)
//		.Select(s => (double)s).ToList();
Console.WriteLine("Completed");

static void Plot(params PlotView[] plots)
{
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