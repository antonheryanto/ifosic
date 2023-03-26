using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.VisualBasic;
using MMU.Ifosic.Models;
using Newtonsoft.Json.Linq;
using System.Diagnostics.Metrics;

namespace MMU.Ifosic.Web.Pages.Fibers;

[Authorize]
public class EditModel : PageModel
{
    private readonly Db _db;
    public EditModel(Db db)
    {
        _db = db;
    }
    [BindProperty] public Fiber Item { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id = 0)
    {
        if (id < 0)
            return Redirect("~/fibers"); //disable create 

        Item = await _db.Fibers.FindAsync(id) ?? new();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Item.Id == 0)
            return Page();
        
        _db.Update(Item);
        await _db.SaveChangesAsync();
        return Redirect("~/fibers");
    }
}