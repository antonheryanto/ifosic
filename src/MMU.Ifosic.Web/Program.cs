using MMU.Ifosic.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using StackExchange.Exceptional;

var builder = WebApplication.CreateBuilder(args);
var env = builder.Environment;
var cfg = builder.Configuration;
var cn = cfg.GetConnectionString("db");

builder.Services.AddExceptional(cfg.GetSection("Exceptional"), o =>
{
    o.Store.ApplicationName = nameof(MMU.Ifosic);
    o.Store.Type = "Memory";
    //o.Store.ConnectionString = cn;
    o.UseExceptionalPageOnThrow = env.IsDevelopment();
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o => {
    o.LoginPath = "/users/auth";
    o.Cookie.Name = nameof(MMU.Ifosic);
});
builder.Services.AddDbContext<Db>(o => o.UseSqlite(cn));
builder.Services.AddEmail(cfg);
builder.Services.AddRazorPages();

var app = builder.Build();
// Error Page
app.MapGet("/error/{path?}/{subPath?}", ExceptionalMiddleware.HandleRequestAsync);
app.UseExceptional();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.Run();