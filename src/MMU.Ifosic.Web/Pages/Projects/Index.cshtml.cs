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

	public async Task<IActionResult> OnGetAsync(int id = 0, int location = 800, int time = 50, int fiberId=1)
    {
        if (id < 1)
            return Redirect("~/");
        Item = await _db.Projects.Include(i => i.Fibers)
            .FirstOrDefaultAsync(f => f.Id == id) ?? new();
        Item.NumberOfFiber = Item.Fibers?.Count ?? Item.NumberOfFiber;
        // load data
		Data = FrequencyShiftDistance.Load(Path.Combine(_path, $"{id}.bin"));
		for (int i = 0; i < Data.Traces.Count; i++)
            Lines.Add(Data.Traces[i][location]);
        
        for (var i = 0; i < Data.Distance.Count; i++)
			FreqDistance.Add(new double[] { Data.Distance[i], Data.Traces[time][i] });

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
		if (Data.References.TryGetValue(Reference, out var value))
		{
			var timeDiff = (Data.MeasurementStart[0] - value[0].Date) ?? new TimeSpan();
			foreach (var (d,v) in Data.References[Reference])
			{
				References.Add(new double[] { d.Add(timeDiff).Subtract(unix).TotalMilliseconds, v });
				if (!refValues.ContainsKey(v))
					refValues.Add(v, 0);
				refValues[v]++;
			}
		}

		if (refValues.Count == 0)
			return Page();
		AveragePoints = Signal.GetAveragePoint(averages.Values.ToArray(), times);
		var refArray = refValues.Keys.ToArray();
		var refPoints = new double[refArray.Length];
		for (int i = 0; i < refArray.Length; i++) {
			refPoints[i] = AveragePoints[i][1];
			ReferencePoints.Add(new double[] { refArray[i], AveragePoints[i][1] });
		}

		//var p = MathNet.Numerics.Fit.Line(refArray, refPoints);
		Regression = MathNet.Numerics.LinearRegression.SimpleRegression.Fit(refArray, refPoints);
		for (int i = 0; i < refArray.Length; i++)
		{
			RegressionPoints.Add(new double[] { refArray[i], refArray[i] * Regression.B + Regression.A });
		}

		return Page();
    }
}
