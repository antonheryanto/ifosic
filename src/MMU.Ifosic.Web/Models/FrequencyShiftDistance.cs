using MemoryPack;
using MemoryPack.Compression;
using System.Collections.Generic;
using System.IO.Compression;

namespace MMU.Ifosic.Models;

[MemoryPackable]
public partial class FrequencyShiftDistance
{
    public Dictionary<string, string> Info { get; set; } = new();
    public List<double> Distance { get; set; } = new();    
    public List<double> Boundaries { get; set; } = new();
	public Dictionary<string, List<(DateTime Date, double Value)>> References { get; set; } = new();
	public List<double[]> Traces { get; set; } = new();
    public List<DateTime?> MeasurementStart { get; set; } = new();
    public List<DateTime?> MeasurementEnd { get; set; } = new();

    public void AddReference(string fileName)
    {
        using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        string name = "Refence";
        var rows = new List<(DateTime Date, double Value)>();
		bool headerParse = false;
        int i = 0;
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (string.IsNullOrEmpty(line))
                continue;
			var cols = line.Split(',');
            if (cols.Length < 2)
                break;

			if (!headerParse)
            {
                name = cols[1].Trim();
                headerParse = true;
                continue;
            }

            var d = DateTime.Now;
            rows.Add((DateTime.TryParse(cols[0], out var date) ? date
                : new DateTime(d.Year, d.Month, d.Day).AddHours(i++),
                double.TryParse(cols[1], out var value) ? value : 0));
		}
        References[name] = rows;
    }

    public static FrequencyShiftDistance Load(string fileName)
    {
        var ext = Path.GetExtension(fileName);
        if (ext == ".bin")
            return LoadBin(fileName);
        var o = new FrequencyShiftDistance();
        if (ext != ".zip")
            return o;
        o.ExtractFromZip(fileName);
        //o.Save(fileName.Replace(".zip", ".bin"));
        return o;
    }

	public int[] ToBoundariesIndex(double[] boundaries)
	{
		var indexes = new int[boundaries.Length];
		for (int i = 0, j = 0; i < Distance.Count; i++)
		{
			if (j == indexes.Length)
				break;
			double v = boundaries[j];
			if (Distance[i] > v)
			{
				indexes[j] = i;
				j++;
			}
		}
		return indexes;
	}

	void ExtractFromZip(string fileName)
    {
        using var stream = File.OpenRead(fileName);
        var zip = new ZipArchive(stream);
        foreach (var entry in zip.Entries)
        {
            if (!entry.Name.EndsWith(".fdd"))
                continue;
            var (i, d, f) = Extract(entry.Open());
            MeasurementStart.Add(DateTime.TryParse(i["Measurement Start"], out var ms) ? ms : null);
            MeasurementEnd.Add(DateTime.TryParse(i["Measurement End"], out var me) ? me : null);
            if (Distance.Count == 0)
                Distance.AddRange(d);
            if (Info.Count == 0)
            {
                foreach (var (k, v) in i)
                    Info.Add(k, v);
            }
                
            Traces.Add(f.ToArray());
        }
    }

    public bool Save(string fileName)
    {
        using var compressor = new BrotliCompressor();
        MemoryPackSerializer.Serialize(compressor, this);
        File.WriteAllBytes(fileName, compressor.ToArray());
        return true;
    }

    public static FrequencyShiftDistance? LoadBin(string fileName)
    {
        var bin = File.ReadAllBytes(fileName);
        using var decompressor = new BrotliDecompressor();
        var decompressedBuffer = decompressor.Decompress(bin);
        return MemoryPackSerializer.Deserialize<FrequencyShiftDistance>(decompressedBuffer);
    }

    (Dictionary<string, string> info, List<double> distances, List<double> frequencies) Extract(Stream stream)
    {
        using var reader = new StreamReader(stream);
        var distances = new List<double>();
        var frequencies = new List<double>();
        var info = new Dictionary<string, string>();
        int i = -1;
        while (!reader.EndOfStream)
        {
            i++;
            var line = reader.ReadLine();
            if (string.IsNullOrEmpty(line))
                continue;
            if (i < 103)
            {
                var l = line.Substring(0, 24).Trim();
                var t = line.Substring(24).Trim();
                info.Add(l, t);
                continue;
            }

            var v = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (!double.TryParse(v[1], out var distance))
                continue;

            distances.Add(distance);
            frequencies.Add(double.Parse(v[2]));
        }
        return (info, distances, frequencies);
    }
}