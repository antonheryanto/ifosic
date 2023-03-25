using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MMU.Ifosic.Models;
using Org.BouncyCastle.Utilities;
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
    public double[] Lines { get; set; } = Array.Empty<double>();
	public double[] Boundaries { get; set; } = Array.Empty<double>();
	public List<double[]> FreqDistance { get; set; } = new();


	public async Task OnGetAsync(int id = 0)
    {
        if (id < 0)
            return;
        Item = await _db.Projects.Include(i => i.Fibers)
            .FirstOrDefaultAsync(f => f.Id == id) ?? new();
		Data = FrequencyShiftDistance.Load(Path.Combine(_path, $"{id}.bin"));        
        Lines = new double[Data.Traces.Count];
		Boundaries = new double[] { 33.57, 35.21, 38.45, 41.74, 45.12, 46.82 };
        var index = Data.ToBoundariesIndex(Boundaries);
		for (int i = 0; i < Data.Traces.Count; i++)
            Lines[i] = Data.Traces[i][800];
        
        for (var i = 0; i < Data.Traces.Count; i++)
        {
			FreqDistance.Add(new double[] { Data.Distance[i], Data.Traces[50][i] });
	    }
    }
}
