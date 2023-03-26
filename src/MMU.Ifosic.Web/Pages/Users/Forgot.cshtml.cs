using MMU.Ifosic.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace MMU.Ifosic.Web.Pages.Users;
public class ForgotModel : PageModel
{
    private readonly Db _db;
    public ForgotModel(Db db)
    {
        _db = db;
    }

    [BindProperty] public string? Email { get; set; }
    [BindProperty] public string? Password { get; set; }
    [BindProperty] public string? Confirm { get; set; }


    public string? Message { get; set; }

    public async Task OnGetAsync(string? token)
    {
        if (token is null)
            return;

        var item = await _db.Users.FirstOrDefaultAsync(w => w.Token == token);
        if (item?.Email is not null)
            Email = item.Email;
    }

    public async Task<IActionResult> OnPostAsync([FromServices] EmailService email, string? token)
    {
        if (string.IsNullOrEmpty(Email))
            ModelState.AddModelError(nameof(Email), "Please specifiy email");

        if (!string.IsNullOrEmpty(token) && (string.IsNullOrEmpty(Password) || Password != Confirm))
        {
            ModelState.AddModelError(nameof(Password), "Password is not match");
            ModelState.AddModelError(nameof(Confirm), "Password is not match");
        }

        if (!ModelState.IsValid)
            return Page();

        if (token is not null)
        {
            var user = await _db.Users.FirstOrDefaultAsync(w => w.Token == token);
            user.Password = Password?.Encrypt(user.Salt).Password;
            _db.Users.Update(user);
            await _db.SaveChangesAsync();

            return Redirect("~/users/auth");
        }

        var item = await _db.Users.FirstOrDefaultAsync(w => w.Email == Email);
        if (item == null)
        {
            ModelState.AddModelError(nameof(Email), "Email does not exist.");
            return Page();
        }

        item.Token ??= item?.Email?.Encrypt().Password.Replace("+", "=");
        await _db.SaveChangesAsync();
        const string APP_NAME = "Inteligent DFOS Characterisation System";
        Message = "A reset link has been sent to your registered email";
        var body = $@"
                    You're receiving this e-mail because you requested a password reset for your user account at {APP_NAME}.
                    <br><br>
                    Please go to the <a href='https://{Request.Host}/users/forgot?token={item?.Token}'>following page and choose a new password</a>
                    <br><br>
                    Your username, in case you've forgotten: {Email}
                    <br><br>
                    Thank you.";
        await email.SendAsync($"Reset password at {APP_NAME}", body, new MimeKit.MailboxAddress(item?.Name, item?.Email));
        return Page();
    }
}
