using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MMU.Ifosic.Models;
using System.Data;

namespace MMU.Ifosic.Web.Pages.Fibers;

[Authorize]
public class EditModel : PageModel
{
    private readonly Db _db;
    public EditModel(Db db)
    {
        _db = db;
    }
    public Fiber Item { get; set; } = new();

	public async Task OnGetAsync(int id = 0)
    {
        if (id < 0)
            return;

		Item = await _db.Fibers.FindAsync(id) ?? new();
	}
}
