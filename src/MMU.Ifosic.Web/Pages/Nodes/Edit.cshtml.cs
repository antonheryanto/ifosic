using MMU.Ifosic.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MMU.Ifosic.Pages.Nodes;

[Authorize]
public class EditModel : PageModel
{
    private readonly Db _db;

    [BindProperty] public Node Item { get; set; } = new();

    public EditModel(Db db)
    {
        _db = db;
    }

    public async Task OnGetAsync(int id = 0)
    {
        if (!User.IsAdmin() || id == 0)
            return;
        Item = await _db.Nodes.FindAsync(id) ?? new();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // TODO validate email, phone or ic unique
        if (!ModelState.IsValid)
            return Page();
        Item.UserId = User.GetId();
        //if (Item.Id == 0)
            Item.CreatedDate = DateTime.UtcNow;
        Item.UpdatedDate = DateTime.UtcNow;
        _db.Nodes.Update(Item);
        await _db.SaveChangesAsync();

        return Redirect("~/nodes");
    }
}
