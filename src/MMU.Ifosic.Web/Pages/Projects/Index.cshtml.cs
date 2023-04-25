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
    public Characterisation Characterisation = new();
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
		Data = FrequencyShiftDistance.Load(Path.Combine(_path, $"{id}.bin")) ?? new();
		
        for (int i = 0; i < Data.Traces.Count; i++)
            Lines.Add(Data.Traces[i][LocationId]);
        
        for (var i = 0; i < Data.Distance.Count; i++)
			FreqDistance.Add(new double[] { Data.Distance[i], Data.Traces[Time][i] });

		Characterisation = new Characterisation(Data, fiberId, Reference);
        return Page();
    }
}
