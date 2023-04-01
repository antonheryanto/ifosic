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

	// loop location within boundary of targeted fiber
	var unix = new DateTime(1970, 1, 1); // TODO adjust based correct ref time
	var times = new double[fdd.Traces.Count];
	for (int i = fdd.BoundaryIndexes[fiberId - 1]; i < fdd.BoundaryIndexes[fiberId]; i++)
	{
		// aggregate only good signal
		if (fdd.Categories[i] != 1)
			continue;

		// get each time freq at that location
		for (int j = 0; j < fdd.Traces.Count; j++)
		{
			times[j] = fdd.MeasurementStart[j]?.Subtract(unix).TotalMilliseconds ?? 0;
			candidates.Add(new double[] { times[j], fdd.Traces[j][i] });
		}
	}
	var groups = candidates.GroupBy(x => x[0]).ToList();
	var averages = new Dictionary<double, double>();
	foreach (var group in groups)
	{
		var sum = 0d;
		var n = 0;
		foreach (var v in group)
		{
			sum += v[1];
			n++;
		}
		averages[group.Key] = sum / n;
	}

	var Reference = "Pressure";
	var refValues = new Dictionary<double, int>();
    var refMax = double.MinValue;
    if (fdd.References.TryGetValue(Reference, out var references))
	{
		foreach (var (d, v) in references)
		{
            refMax = Math.Max(refMax, v);
            if (!refValues.ContainsKey(v))
				refValues.Add(v, 0);
			refValues[v]++;
		}
	}

	var Averages = averages.Select(d => new double[] { d.Key, d.Value }).ToList();
	var AveragePoints = Signal.GetAveragePoint(averages.Values.ToArray(), times);
	var refDate = references[0].Date;
    var averageIndex = 0;
    for (int i = 0; i < AveragePoints.Count; i++)
    {
        var second = AveragePoints[i][0];
        var aDate = unix.AddMilliseconds(second);
        if (refDate > aDate)
            continue;
        averageIndex = i;
        break;
    }

    var refArray = refValues.Keys.ToList();
    var refMaxId = refArray.IndexOf(refMax);
    var refStart = refMaxId < refArray.Count / 2 ? refMaxId : 0;
    var avgPoints = new List<double>();
    var refPoints = new List<double>();
    var ReferencePoints = new List<double[]>();
    for (int i = 0, j = refStart; j < refArray.Count; i++, j++)
    {
		var avgIdx = j + averageIndex;
		if (avgIdx > AveragePoints.Count - 1)
			continue;
        refPoints.Add(refArray[j]);
        avgPoints.Add(AveragePoints[avgIdx][1]);
        ReferencePoints.Add(new double[] { refPoints[i], avgPoints[i] });
    }

    //var p = MathNet.Numerics.Fit.Line(refArray, refPoints);
	if (refPoints.Count < 2)
	{
		refPoints.Insert(0, 0);
        avgPoints.Insert(0, 0);
    }
    var Regression = MathNet.Numerics.LinearRegression.SimpleRegression.Fit(refPoints.ToArray(), avgPoints.ToArray());
    var RegressionPoints = new List<double[]>();
    for (int i = 0; i < refPoints.Count; i++)
    {
        RegressionPoints.Add(new double[] { refPoints[i], refPoints[i] * Regression.B + Regression.A });
    }

	return Results.Ok(new { candidates, Averages, AveragePoints, ReferencePoints, Slope = Regression.B, RegressionPoints });
});

app.UseExceptional();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.Run();