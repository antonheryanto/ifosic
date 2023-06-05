using MMU.Ifosic;
using MMU.Ifosic.Models;
using OxyPlot.WindowsForms;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

IPAddress ipAddress = new(new byte[] { 127, 0, 0, 1 });
IPEndPoint ipEndPoint = new(ipAddress, 3082);
using Socket client = new(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
await client.ConnectAsync(ipEndPoint);

async Task<string> SendMessage(Socket client, string message)
{
	var authMessage = Encoding.UTF8.GetBytes(message);
	_ = await client.SendAsync(authMessage, SocketFlags.None);
	// Receive ack.
	var buffer = new byte[1_024];
	var received = await client.ReceiveAsync(buffer, SocketFlags.None);
	var response = Encoding.UTF8.GetString(buffer, 0, received);
	if (!response.Contains("COMPLD"))
		return "FAIL";
	var ress = response.Trim('\r', '\n', ';', ' ').Split("\r\n");
	return ress.Length < 3 ? "" : ress[2].Trim(' ', '"');
}

var auth = await SendMessage(client, "ACT-USER::root:1::root;");
var list = await SendMessage(client, "RTRV-PATCH:::123:;");
var sw = new Stopwatch();
sw.Start();
var add = await SendMessage(client, "ENT-PATCH::1,9:123:;");
sw.Stop();
Debug.WriteLine($"Elapsed={sw.Elapsed}");
var list2 = await SendMessage(client, "RTRV-PATCH:::123:;");
client.Shutdown(SocketShutdown.Both);

return;

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