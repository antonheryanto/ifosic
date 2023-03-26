using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    public List<double> Lines { get; set; } = new();
	public List<double[]> FreqDistance { get; set; } = new();

	public async Task<IActionResult> OnGetAsync(int id = 0, int location = 800, int time = 50)
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

        return Page();
    }
}
