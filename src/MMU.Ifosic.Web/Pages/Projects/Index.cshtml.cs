using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MMU.Ifosic.Models;

namespace MMU.Ifosic.Web.Pages.Projects;

[Authorize]
public class IndexModel : PageModel
{
    private readonly Db _db;
    public IndexModel(Db db)
    {
        _db = db;
    }
    public Project Item { get; set; } = new();

    public async Task OnGetAsync(int id = 0)
    {
        if (id < 0)
            return;
        Item = await _db.Projects.FindAsync(id) ?? new();
    }
}
