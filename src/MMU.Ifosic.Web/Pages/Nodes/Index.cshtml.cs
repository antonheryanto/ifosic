using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MMU.Ifosic.Models;

namespace MMU.Ifosic.Pages.Nodes;

[Authorize]
public class IndexModel : PageModel
{
    private readonly Db _db;

    public IndexModel(Db db)
    {
        _db = db;
    }

    public List<Node> Items { get; set; } = new();

    public async Task OnGetAsync()
    {
        Items = await _db.Nodes.ToListAsync();
    }
}