using MemoryPack;
using MemoryPack.Compression;
using System.IO.Compression;

namespace MMU.Ifosic;

[MemoryPackable]
public partial class FrequencyShiftDistance
{
    public Dictionary<string, string> Info { get; set; } = new();
    public List<double> Distance { get; set; } = new();
    public List<double[]> Traces { get; set; } = new();
    public List<double> CalibrationTemperature { get; set; } = new();
    public List<double> TemperatureStart { get; set; } = new();
    public List<double> TemperatureEnd { get; set; } = new();
    public List<DateTime?> MeasurementStart { get; set; } = new();
    public List<DateTime?> MeasurementEnd { get; set; } = new();

    public static FrequencyShiftDistance Load(string fileName)
    {
        var ext = Path.GetExtension(fileName);
        if (ext == ".bin")
            return LoadBin(fileName);
        var o = new FrequencyShiftDistance();
        if (ext != ".zip")
            return o;
        o.ExtractFromZip(fileName);
        o.Save(fileName.Replace(".zip", ".bin"));
        return o;
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
            CalibrationTemperature.Add(double.TryParse(i["Calibration Temp."], out var ct) ? ct : 0);
            MeasurementStart.Add(DateTime.TryParse(i["Measurement Start"], out var ms) ? ms : null);
            MeasurementEnd.Add(DateTime.TryParse(i["Measurement End"], out var me) ? me : null);
            TemperatureStart.Add(double.TryParse(i["Temperature Start"], out var ts) ? ts : 0);
            TemperatureEnd.Add(double.TryParse(i["Temperature End"], out var te) ? te : 0);
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

    bool Save(string file)
    {
        using var compressor = new BrotliCompressor();
        MemoryPackSerializer.Serialize(compressor, this);
        File.WriteAllBytes(file, compressor.ToArray());
        return true;
    }

    public static FrequencyShiftDistance? LoadBin(string file)
    {
        var bin = File.ReadAllBytes(file);
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