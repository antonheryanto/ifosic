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
builder.Services.AddRazorPages().AddJsonOptions(o => o.JsonSerializerOptions.IncludeFields = true);

var app = builder.Build();
// Error Page
app.MapGet("/error/{path?}/{subPath?}", ExceptionalMiddleware.HandleRequestAsync);
app.MapGet("/api/project/{id}", (int id) => {
    var path = cfg.GetValue<string>("ProjectPath") ?? Path.Combine(Environment.CurrentDirectory, "projects");
    var fdd = FrequencyShiftDistance.Load(Path.Combine(path, $"{id}.bin"));
    var boundaries = new double[] { fdd.Boundaries[0] - 3, fdd.Boundaries[^1] + 5 };
    var indexes = fdd.ToBoundariesIndex(boundaries);
    var data = new List<double[]>();
    var start = indexes[0];
    var stop = indexes[1];
	for (int i = 0; i < fdd.Traces.Count; i++)
		for (int j = start; j < stop; j++)
			data.Add(new double[] { fdd.Distance[j], i, fdd.Traces[i][j] });
    return Results.Ok(new { start, stop, data });
});

app.UseExceptional();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.Run();