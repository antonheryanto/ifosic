using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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

		var boundaryDistance = new Dictionary<int, double>();
		var unix = new DateTime(1970, 1, 1);
		// loop location within boundary of targeted fiber
		for (int i = Data.BoundaryIndexes[fiberId - 1]; i < Data.BoundaryIndexes[fiberId]; i++)
		{
			// aggregate only good signal
			if (Data.Categories[i] != 1)
				continue;

			// get each time freq at that location
			for (int j = 0; j < Data.Traces.Count; j++)
				Candidates.Add(new double[] { Data.MeasurementStart[j]?.Subtract(unix).TotalMilliseconds ?? 0, Data.Traces[j][i] });
		}

		if (Data.References.TryGetValue("Pressure", out var value))
		{
			var timeDiff = (Data.MeasurementStart[0] - value[0].Date) ?? new TimeSpan();
			foreach (var (d,v) in Data.References["Pressure"])
			{
				References.Add(new double[] { d.Add(timeDiff).Subtract(unix).TotalMilliseconds, v });
			}
		}

		return Page();
    }
}
