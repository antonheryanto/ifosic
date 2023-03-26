using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
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
    [BindProperty] public Project Item { get; set; } = new();

    public async Task OnGetAsync(int id = 0)
    {
        if (id < 0)
            return;
        Item = await _db.Projects.Include(i => i.Fibers)
            .FirstOrDefaultAsync(f => f.Id == id) ?? new();
        Item.NumberOfFiber = Item.Fibers?.Count() ?? 0;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrEmpty(Item.Name))
            ModelState.AddModelError(nameof(Item.Name), $"{nameof(Item.Name)} is required.");

        if (!ModelState.IsValid)
            return Page();

        if (Item.Id > 0)
        {
            var item = await _db.Projects.Include(i => i.Fibers)
                .FirstOrDefaultAsync(f => f.Id == Item.Id);
            if (item == null)
                return Page();
            item.Name = Item.Name;
            item.Description = Item.Description;
            if (Item.NumberOfFiber > item.Fibers?.Count) {
                item.Fibers ??= new();
                for (int i = 0; i < Item.NumberOfFiber - item.Fibers.Count; i++)
                    item.Fibers.Add(new() { Name = $"Fiber {i + item.Fibers.Count + 1}", ProjectId = item.Id });
            }
            _db.Projects.Update(item);
        } 
        else
        {
            Item.Fibers ??= new();
            for (int i = 0; i < Item.NumberOfFiber; i++)
                Item.Fibers.Add(new() { Name = $"Fiber {i+1}" });
            _db.Projects.Add(Item);
        }
        await _db.SaveChangesAsync();

        return Redirect("~/projects/@Item.Id");
    }
}
