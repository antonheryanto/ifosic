using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.EntityFrameworkCore;
using MMU.Ifosic.Models;
using Org.BouncyCastle.Utilities;
using StackExchange.Exceptional.Internal;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MMU.Ifosic.Web.Pages.Projects;

[Authorize]
public class IndexModel : PageModel
{
    private readonly Db _db;
    private readonly string _path;
    public IndexModel(Db db, IConfiguration cfg)
    {
        _db = db;
		_path = cfg.GetValue<string>("ProjectPath") ?? Path.Combine(Environment.CurrentDirectory, "projects");
    }
    public Project Item { get; set; } = new();
    public FrequencyShiftDistance Data { get; set; } = new();
    public List<double> Lines { get; set; } = new();
	public List<double[]> FreqDistance { get; set; } = new();
	public List<double[]> Candidates { get; set; } = new();
	public List<double[]> References { get; set; } = new();
	public List<double[]> Averages { get; set; } = new();
	public List<double[]> AveragePoints { get; set; } = new();
	public List<double[]> ReferencePoints { get; set; } = new();
	public List<double[]> RegressionPoints { get; set; } = new();
	public (double A, double B) Regression { get; set; } = new();
	public string Reference { get; set; } = "Pressure";
	public string Unit { get; set; } = "MPa";
	[BindProperty(SupportsGet = true)] public int LocationId { get; set; } = 700;
	[BindProperty(SupportsGet = true)] public int Time { get; set; } = 100;

    public async Task<IActionResult> OnGetAsync(int id = 0, int fiberId=1)
    {
        if (id < 1)
            return Redirect("~/");
        Item = await _db.Projects.Include(i => i.Fibers)
            .FirstOrDefaultAsync(f => f.Id == id) ?? new();
        Item.NumberOfFiber = Item.Fibers?.Count ?? Item.NumberOfFiber;
        // load data
		Data = FrequencyShiftDistance.Load(Path.Combine(_path, $"{id}.bin"));
		for (int i = 0; i < Data.Traces.Count; i++)
            Lines.Add(Data.Traces[i][LocationId]);
        
        for (var i = 0; i < Data.Distance.Count; i++)
			FreqDistance.Add(new double[] { Data.Distance[i], Data.Traces[Time][i] });

		var unix = new DateTime(1970, 1, 1);
		// loop location within boundary of targeted fiber
		var times = new double[Data.Traces.Count];
		for (int i = Data.BoundaryIndexes[fiberId - 1]; i < Data.BoundaryIndexes[fiberId]; i++)
		{
			// aggregate only good signal
			if (Data.Categories[i] != 1)
				continue;

			// get each time freq at that location
			for (int j = 0; j < Data.Traces.Count; j++)
			{
				times[j] = Data.MeasurementStart[j]?.Subtract(unix).TotalMilliseconds ?? 0;
				Candidates.Add(new double[] { times[j], Data.Traces[j][i] });
			}
		}

		var groups = Candidates.GroupBy(x => x[0]).ToList();
		var averages = new Dictionary<double, double>();
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
			averages[group.Key] = sum / n;

			g++;
		}

		Averages = averages.Select(d => new double[] { d.Key, d.Value }).ToList();
		var refValues = new Dictionary<double, int>();
		if (!Data.References.TryGetValue(Reference, out var references))
			return Page();

		var refMax = double.MinValue;
        var refDate = references[0].Date;
        var timeDiff = refDate >= Data.MeasurementStart[0] ? new TimeSpan()
                : (Data.MeasurementStart[0] - refDate) ?? new TimeSpan();
        foreach (var (d, v) in references)
        {
			refMax = Math.Max(refMax, v);
            References.Add(new double[] { d.Add(timeDiff).Subtract(unix).TotalMilliseconds, v });
            if (!refValues.ContainsKey(v))
                refValues.Add(v, 0);
            refValues[v]++;
        }

        if (refValues.Count == 0)
            return Page();

        AveragePoints = Signal.GetAveragePoint(averages.Values.ToArray(), times);
        var averageIndex = 0;
        for (int i = 0; i < AveragePoints.Count; i++)
		{
			var second = AveragePoints[i][0];
            var aDate = unix.AddMilliseconds(second);
			if (refDate > aDate)
				continue;
			averageIndex = i;
			break;
		}
        var refArray = refValues.Keys.ToList();
		var refMaxId = refArray.IndexOf(refMax);
		var refStart = refMaxId < refArray.Count / 2 ? refMaxId : 0; 
        var avgPoints = new List<double>();
        var refPoints = new List<double>();
		
		for (int i = 0, j = refStart; j < refArray.Count; i++, j++) {
            var avgIdx = j + averageIndex;
            if (avgIdx > AveragePoints.Count - 1)
                continue;
            refPoints.Add(refArray[j]);
            avgPoints.Add(AveragePoints[avgIdx][1]);
            ReferencePoints.Add(new double[] { refPoints[i], avgPoints[i] });
        }		
        if (refPoints.Count < 2)
        {
			for (int i = refPoints.Count; i < 3; i++)
			{
				refPoints.Insert(0, 0);
				avgPoints.Insert(0, 0);
			}
        }
        //var p = MathNet.Numerics.Fit.Line(refArray, refPoints);
        Regression = MathNet.Numerics.LinearRegression.SimpleRegression.Fit(refPoints.ToArray(), avgPoints.ToArray());
		for (int i = 0; i < refPoints.Count; i++)
		{
			RegressionPoints.Add(new double[] { refPoints[i], refPoints[i] * Regression.B + Regression.A });
		}

		return Page();
    }
}
