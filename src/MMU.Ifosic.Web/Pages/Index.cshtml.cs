using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MMU.Ifosic.Models;

namespace MMU.Ifosic.Web.Pages;

[Authorize]
public class IndexModel : PageModel
{
    private readonly Db _db;
    public IndexModel(Db db)
    {
        _db = db;
    }

    public List<Project> Items { get; set; }

    public async Task OnGetAsync()
    {
        Items = await _db.Projects.Include(i => i.Fibers).ToListAsync();
    }
}