using MMU.Ifosic.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using StackExchange.Exceptional;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Microsoft.AspNetCore.Routing.Matching;

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
var path = cfg.GetValue<string>("ProjectPath") ?? Path.Combine(Environment.CurrentDirectory, "projects");
// Error Page
app.MapGet("/error/{path?}/{subPath?}", ExceptionalMiddleware.HandleRequestAsync);
app.MapGet("/api/project/{id}", (int id) => {
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
app.MapGet("/api/project/{id}/time/{location}", (int id, int location) =>
{
	var fdd = FrequencyShiftDistance.Load(Path.Combine(path, $"{id}.bin"));
	var freq = new double[fdd.Traces.Count];
	for (int i = 0; i < fdd.Traces.Count; i++)
		freq[i] = fdd.Traces[i][location];
	return Results.Ok(freq);
});

app.MapGet("/api/project/{id}/location/{time}", (int id, int time) =>
{
	var fdd = FrequencyShiftDistance.Load(Path.Combine(path, $"{id}.bin"));
	var freq = new double[fdd.Distance.Count][];
	for (var i = 0; i < freq.Length; i++)
		freq[i] = new double[] { fdd.Distance[i], fdd.Traces[time][i] };
	return Results.Ok(freq);
});

app.MapGet("/api/project/{id}/fiber/{fiberId}", (int id, int fiberId) =>
{
	var fdd = FrequencyShiftDistance.Load(Path.Combine(path, $"{id}.bin"));
	var candidates = new List<double[]>();
	var unix = new DateTime(1970, 1, 1);
	// loop location within boundary of targeted fiber
	for (int i = fdd.BoundaryIndexes[fiberId - 1]; i < fdd.BoundaryIndexes[fiberId]; i++)
	{
		// aggregate only good signal
		if (fdd.Categories[i] != 1)
			continue;

		// get each time freq at that location
		for (int j = 0; j < fdd.Traces.Count; j++)
			candidates.Add(new double[] { fdd.MeasurementStart[j]?.Subtract(unix).TotalMilliseconds ?? 0, fdd.Traces[j][i] });
	}
	return Results.Ok(candidates);
});

app.UseExceptional();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.Run();