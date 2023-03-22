using MMU.Ifosic.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StackExchange.Exceptional;

namespace MMU.Ifosic.Web.Pages.Users;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly Db _db;

    public PaginatedList<User> Items { get; set; } = new();
    public Dictionary<int, string> Roles { get; set; } = new();
    [BindProperty(SupportsGet = true)] public int SearchBy { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public string Search { get; set; } = "";
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    public Dictionary<string, string> Parms { get; set; } = new();

    public IndexModel(Db db)
    {
        _db = db;
    }

    public async Task OnGetAsync()
    {
        if (PageNumber > 0)
            Parms[nameof(PageNumber)] = PageNumber.ToString();
        if (SearchBy > 0)
            Parms[nameof(SearchBy)] = SearchBy.ToString();
        if (!string.IsNullOrEmpty(Search))
            Parms[nameof(Search)] = Search;
        var items = _db.Users.AsQueryable();
        if (!string.IsNullOrEmpty(Search))
            items = SearchBy switch
            {
                1 => items.Where(w => w.Name.ToLower().Contains(Search.ToLower())),
                2 => items.Where(w => (w.Email ?? "").ToLower().Contains(Search.ToLower())),
                3 => items.Where(w => w.IsActive == (Search == "1")),
                4 => items.Where(w => (w.Roles ?? new()).Any(r => r.Id.ToString() == Search)),
                _ => items
            };
        Items = await items.Include(x => x.Roles).PaginateAsync(PageNumber);
        Roles = await _db.Roles.ToDictionaryAsync(k => k.Id, v => v.Name ?? "") ?? new();
    }

    public async Task<IActionResult> OnGetDelete(int id = 0)
    {
        if (id < 0)
            return Page();
        var user = await _db.Users.FindAsync(id);
        if (user is null)
            return Page();

        try
        {
            _db.Remove(user);
        }
        catch (Exception e)
        {
            await e.LogAsync(HttpContext);
            user.IsActive = false;
            _db.Users.Update(user);
        }

        await _db.SaveChangesAsync();
        return Redirect("~/users");
    }
}