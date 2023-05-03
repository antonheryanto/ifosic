using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.EntityFrameworkCore;
using MMU.Ifosic.Models;
using NPOI.POIFS.Crypt.Dsig;
using Org.BouncyCastle.Utilities;
using StackExchange.Exceptional;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
builder.Services.AddRazorPages().AddJsonOptions(o => {
	o.JsonSerializerOptions.IncludeFields = true;
	o.JsonSerializerOptions.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals;
});

var app = builder.Build();
var path = cfg.GetValue<string>("ProjectPath") ?? Path.Combine(Environment.CurrentDirectory, "projects");
// Error Page
app.MapGet("/error/{path?}/{subPath?}", ExceptionalMiddleware.HandleRequestAsync);
app.MapGet("/api/project/{id}", (int id) => {
    var fdd = FrequencyShiftDistance.Load(Path.Combine(path, $"{id}.bin"));
	if (fdd is null)
		return Results.Ok();
	var start = 0;
	var stop = fdd.Distance.Count;
	if (fdd.Boundaries.Count > 0)
	{
		var boundaries = new double[] { fdd.Boundaries[0] - 3, fdd.Boundaries[^1] + 5 };
		var indexes = fdd.ToBoundariesIndex(boundaries);
		start = indexes[0];
		stop = indexes[1];
	}
	var data = new List<double[]>();
	for (int i = 0; i < fdd.Traces.Count; i++)
		for (int j = start; j < stop; j++)
			data.Add(new double[] { fdd.Distance[j], i, fdd.Traces[i][j] });
    return Results.Ok(new { start, stop, data });
});
app.MapGet("/api/project/{id}/time/{location}", (int id, int location, int? fiberId) =>
{
	var fiberIndex = fiberId ?? 1;
	var fdd = FrequencyShiftDistance.Load(Path.Combine(path, $"{id}.bin"));
	var freq = new List<double[]>();
	if (fdd is null)
		return Results.Ok(Array.Empty<double>);
	for (int i = 0; i < fdd.Traces.Count; i++)
	{
		if (fdd.TimeBoundaries?.Count > 0 && fdd.TimeBoundaries[i] != fiberIndex)
			continue;
		freq.Add(new[] { fdd.MeasurementStart[i]?.Subtract(Characterisation.UnixTime).TotalMilliseconds ?? 0,
			fdd.Traces[i][location] });
	}
	return Results.Ok(freq);
});

app.MapGet("/api/project/{id}/location/{time}", (int id, int time) =>
{
	var fdd = FrequencyShiftDistance.Load(Path.Combine(path, $"{id}.bin"));
	if (fdd is null)
		return Results.Ok(Array.Empty<double>);
	var freq = new double[fdd.Distance.Count][];
	for (var i = 0; i < freq.Length; i++)
		freq[i] = new double[] { fdd.Distance[i], fdd.Traces[time][i] };
	return Results.Ok(freq);
});

app.MapGet("/api/project/{id}/fiber/{fiberId}", (int id, int fiberId) =>
{
	var fdd = FrequencyShiftDistance.Load(Path.Combine(path, $"{id}.bin"));
	var m = new Characterisation(fdd, fiberId);
	return Results.Ok(m);
});

app.UseExceptional();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.Run();