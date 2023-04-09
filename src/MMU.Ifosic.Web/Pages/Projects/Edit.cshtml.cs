using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MMU.Ifosic.Models;

namespace MMU.Ifosic.Web.Pages.Projects;

[Authorize]
public class EditModel : PageModel
{
    private static readonly string[] _mime = new string[]
    {
        "application/octet-stream",
        "multipart/x-zip",
        "application/zip",
        "application/zip-compressed",
        "application/x-zip-compressed",
    };
    
    private readonly Db _db;
    private const string FOLDER_NAME = "projects";
    private readonly string _path = Path.Combine(Environment.CurrentDirectory, FOLDER_NAME);

    public EditModel(Db db, IConfiguration cfg)
    {
        _db = db;
        if (cfg.GetValue<string>("ProjectPath") is var name && name is not null)
            _path = name;
    }

    [BindProperty] public Project Item { get; set; } = new();
    [BindProperty] public IFormFile? Upload { get; set; }


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


        if (Item.Id > 0 && Upload is not null && Upload.Length > 0 && _mime.Contains(Upload.ContentType))
        {
            //var path = Path.Combine(_path, Item.Id.ToString());
            var file = Path.Combine(_path, Upload.FileName);
            using (var fileStream = new FileStream(file, FileMode.Create))
            {
                await Upload.CopyToAsync(fileStream);
            }
            var fdd = FrequencyShiftDistance.Load(file);
            Signal.GetBoundary(fdd, Path.Combine(_path, "model.onnx"), Item.NumberOfFiber);
            fdd.Save(Path.Combine(_path, $"{Item.Id}.bin"));
        }

        return Redirect("~/projects/@Item.Id");
    }
}
