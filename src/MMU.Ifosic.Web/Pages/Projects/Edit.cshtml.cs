using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MMU.Ifosic.Models;
using System.Data;

namespace MMU.Ifosic.Web.Pages.Projects;

[Authorize]
public class EditModel : PageModel
{
    private readonly Db _db;
    public EditModel(Db db)
    {
        _db = db;
    }
    public Project Item { get; set; } = new();
	[BindProperty] public Dictionary<int, Fiber> Fibers { get; set; } = new();

	public async Task OnGetAsync(int id = 0)
    {
        if (id < 0)
            return;
		Fibers = await _db.Fibers.ToDictionaryAsync(k => k.Id, v => v);

		Item = await _db.Projects.Include(i => i.Fibers)
            .FirstOrDefaultAsync(f => f.Id == id) ?? new();
		if (Item.Fibers?.Count > 0)
			foreach (var m in Item.Fibers)
				Fibers[m.Id].IsActive = true;
	}
}
