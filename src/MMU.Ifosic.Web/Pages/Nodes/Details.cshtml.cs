using MMU.Ifosic.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MMU.Ifosic.Web.Pages.Nodes;

public class DetailsModel : PageModel
{
    private readonly Db _db;
    public DetailsModel(Db db)
    {
        _db = db;
    }

    public Node Item { get; set; } = new();

    public async Task OnGet(int id)
    {
        Item = await _db.Nodes.FindAsync(id) ?? new();
    }

}
