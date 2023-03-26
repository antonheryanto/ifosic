using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MMU.Ifosic.Models;

namespace MMU.Ifosic.Web.Pages.Fibers;

[Authorize]
public class IndexModel : PageModel
{
    private readonly Db _db;
    public IndexModel(Db db)
    {
        _db = db;
    }

    public List<Fiber> Items { get; set; } = new();
	public Fiber Item { get; set; } = new();

    public async Task OnGetAsync(int id = 0)
    {
        if (id > 0)
        {
            Item = await _db.Fibers.FindAsync(id) ?? new();
            return;
        }
        Items = await _db.Fibers.Include(i => i.Project).ToListAsync();
    }
}