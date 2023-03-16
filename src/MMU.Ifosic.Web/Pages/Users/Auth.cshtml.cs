using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MMU.Ifosic.Models;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace MMU.Ifosic.Pages.Users;

public class AuthModel : PageModel
{
    private readonly Db _db;

    [BindProperty] public string? Username { get; set; }
    [BindProperty] public string? Password { get; set; }

    public AuthModel(Db db)
    {
        _db = db;
    }

    public async Task<IActionResult> OnGet(string token, string returnUrl = $"~/", int id = 0)
    {
        _db.Database.SqlQuery<int>($"select count(*) from users");
        if (!User.IsAdmin() && string.IsNullOrEmpty(token))
            return Page();

        var user = await _db.Users.Include(i => i.Roles)
            .FirstOrDefaultAsync(f => (!string.IsNullOrEmpty(token) && f.Token == token) || f.Id == id);
        if (user == null || (!User.IsAdmin() && user.IsAdmin)) // security protection, cannot log as admin using token
            return Page();

        await LoginAsync(user, impersonate: User.IsAdmin());
        return Redirect(returnUrl);
    }

    public async Task<IActionResult> OnGetDelete()
    {
        var impersonate = User.Claims.FirstOrDefault
            (s => s.Type == "Impersonate")?.Value;
        if (int.TryParse(impersonate, out int id))
        {
            var user = await _db.Users.Include(i => i.Roles)
                .FirstOrDefaultAsync(f => f.Id == id);
            if (user != null)
            {
                await LoginAsync(user);
                return Redirect("~/");
            }
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        var item = await _db.Users.FindAsync(User.GetId());
        if (item != null)
        {
            item.SignOutDate = DateTime.UtcNow;
            _db.Update(item);
            await _db.SaveChangesAsync();
        }
        return Redirect("~/users/auth");
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl)
    {
        var error = "Email or password is incorrect";
        if (Username.IsNullOrEmpty() || Password.IsNullOrEmpty())
        {
            ModelState.AddModelError(nameof(Username), error);
            ModelState.AddModelError(nameof(Password), error);
            return Page();
        }

        var username = Username?.Trim() ?? "";
        var id = string.Join("", username.Split('-'));

        var user = await _db.Users.Include(i => i.Roles)
            .Where(w => w.Email == username || w.Phone == id)
            .FirstOrDefaultAsync();
        if (user == null)
        {
            ModelState.AddModelError(nameof(Username), error);
            ModelState.AddModelError(nameof(Password), error);
            return Page();
        }

        if (user.Password != Password?.Trim()?.Encrypt(user.Salt).Password)
        {
            ModelState.AddModelError(nameof(Username), error);
            ModelState.AddModelError(nameof(Password), error);
        }
        if (!user.IsActive)
            ModelState.AddModelError("", "Please activate your account through your email");

        if (!ModelState.IsValid)
            return Page();

        await LoginAsync(user);
        return Redirect(returnUrl ?? "~/");
    }

    public async Task LoginAsync(User user, bool impersonate = false)
    {
        List<Claim> claims = new() {
            new (ClaimTypes.Name, user.Id.ToString()),
            new (ClaimTypes.GivenName, user.Name)
        };
        if (user.Email is not null)
            claims.Add(new(ClaimTypes.Email, user.Email));
        if (user.Phone is not null)
            claims.Add(new(ClaimTypes.MobilePhone, user.Phone));        
        if (user.Roles?.Count > 0)
            foreach (var role in user.Roles)
                claims.Add(new Claim(ClaimTypes.Role, role.Name ?? role.Id.ToString()));
        if (impersonate)
            claims.Add(new Claim("Impersonate", User.GetId().ToString()));

        ClaimsIdentity u = new(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new(u), new() { ExpiresUtc = DateTime.UtcNow.AddDays(1) });
        user.SignInDate = DateTime.UtcNow;
        _db.Users.Attach(user).Property(p => p.SignInDate).IsModified = true;
        await _db.SaveChangesAsync();
    }
}
