using MMU.Ifosic.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Globalization;

namespace MMU.Ifosic.Web.Pages.Users;

[Authorize]
public class EditModel : PageModel
{
    private readonly Db _db;
    private static readonly User _user = new() { Roles = new() { new() { Id = 2 } } };

    [BindProperty] public User Item { get; set; } = _user;
	[BindProperty] public string? Password { get; set; }
    [BindProperty] public Dictionary<int, Role> Roles { get; set; } = new();
    [BindProperty] public bool IsAgree { get; set; }
    [BindProperty(SupportsGet = true)] public int? TypeId { get; set; }


    public EditModel(Db db)
    {
        _db = db;
    }

    public async Task OnGetAsync(string? token, int id = 0)
    {
        id = !User.IsAdmin() ? User.GetId() : id;
        Roles = await _db.Roles.ToDictionaryAsync(k => k.Id, v => v);
        if (!User.IsLogged())
        {
            Roles[2].IsActive = true;
            if (string.IsNullOrEmpty(token))
                return;
            Item = await _db.Users.Include(i => i.Roles)
                .FirstOrDefaultAsync(w => w.Token == token)
                ?? _user;
        }

        if (id > 0)
            Item = (await _db.Users.Include(i => i.Roles).FirstOrDefaultAsync(x => x.Id == id)) ?? _user;

        if (User.IsAdmin() && id == 0 && Item is not null)
            Item.IsActive = true;

        if (Item?.Roles?.Count > 0)
            foreach (var m in Item.Roles)
                Roles[m.Id].IsActive = true;
    }

    static readonly TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
    const string EXIST = "is already registered";
    const string PASSWORD_INVALID = "Password is not match";
    // validate IC or Passport
    public async Task<IActionResult> OnPostAsync([FromServices] EmailService email)
    {
        // reformat phone
        Item.Phone = string.Join("", (Item?.Phone ?? "").Trim().Split('-'));

        if (!User.IsLogged() && !string.IsNullOrEmpty(Item.Token) && (Password.IsNullOrEmpty() || Item.Password != Password))
        {
            ModelState.AddModelError($"{nameof(Item)}.{nameof(Password)}", PASSWORD_INVALID);
            ModelState.AddModelError(nameof(Password), PASSWORD_INVALID);
        }
        // validate email
        if (await _db.Users.AnyAsync(a => a.Email == Item.Email && (Item.Id == 0 || (Item.Id > 0 && a.Id != Item.Id))))
            ModelState.AddModelError(nameof(Item.Email), $"Emel {EXIST}");
        // validate phone
        //if (await _db.Users.AnyAsync(a => a.Phone == Item.Phone && (Item.Id == 0 || (Item.Id > 0 && a.Id != Item.Id))))
        //    ModelState.AddModelError(nameof(Item.Phone), $"Telefon {EXIST}");
        
        if (!User.IsLogged() && Item.Token is not null && !IsAgree)
            ModelState.AddModelError(nameof(IsAgree), $"Please agree to terms and conditions.");
        // make sure all data valid
        if (!ModelState.IsValid)
            return Page();

        // FIXME this cause manual house keeping
        var item = Item.Id == 0 ? Item : await _db.Users.Include(i => i.Roles)
            .FirstOrDefaultAsync(f => f.Id == Item.Id) 
            ?? new();

        item.Roles ??= new();
        
        if (User.IsAdmin())
        {
            item.IsActive = Item.IsActive;
            foreach (var m in Roles.Values)
            {
                var exist = item.Roles.Any(w => w.Id == m.Id);
                if (m.IsActive && !exist)
                    item.Roles.Add(m);
                else if (!m.IsActive && exist)
                    item.Roles.RemoveAll(f => f.Id == m.Id);
            }
        }
        else if (!User.IsLogged())
        {
            if (item.Token is null)
            {
                foreach (var m in Roles.Values) //Authors
                    if (m.IsActive)
                        item.Roles.Add(m);
            }
            else if (Item.Token == item.Token)
            {
                item.IsActive = true;
            }
        }

        var returnUrl = "~/";

        // send activation
        if ((!User.IsLogged() && item.Token is null) || (User.IsAdmin() && Item.Id == 0 && !Item.IsActive))
        {
            item.Token = item.Email?.Encrypt().Password.Replace('+', '=');
            returnUrl = $"/users/edit?token={item.Token}&typeId=1";
        }

        if (Password is not null && Item.Password == Password)
            (item.Password, item.Salt) = Password.Encrypt();
        item.Name = textInfo.ToTitleCase(Item.Name.ToLower());
        item.Email = Item.Email?.ToLower();
        item.Phone = Item.Phone;
        item.UpdatedDate = DateTime.UtcNow;
        item.UpdatedById = User.GetId();
        if (item.Id == 0)
        {
            item.CreatedDate = DateTime.UtcNow;
            item.CreatedById = item.UpdatedById;
        }

        _db.Users.Update(item);

        await _db.SaveChangesAsync();

        return Redirect(User.IsAdmin() ? "~/users" : returnUrl);
    }
}
