using System.IO.Compression;

namespace MMU.Ifosic;

public class FrequencyShiftDistance
{
    public FrequencyShiftDistance(string fileName)
    {
        if (Path.GetExtension(fileName) == ".zip")
            ExtractFromZip(fileName);        
    }

    void ExtractFromZip(string fileName)
    {
        using var stream = File.OpenRead(fileName);
        var zip = new ZipArchive(stream);
        foreach (var entry in zip.Entries)
        {
            if (!entry.Name.EndsWith(".fdd"))
                continue;
            Extract(entry.Open());
        }
    }

    (Dictionary<string, string> info, List<double> distances, List<double> frequencies) Extract(Stream stream)
    {
        using var reader = new StreamReader(stream);
        var distances = new List<double>();
        var frequencies = new List<double>();
        var info = new Dictionary<string, string>();
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (string.IsNullOrEmpty(line))
                continue;
            var v = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (v.Length < 2)
                continue;
            if (v.Length == 2) {
                info.Add(v[0], v[1]);
                continue;
            }
            if (!double.TryParse(v[1], out var distance))
                continue;

            distances.Add(distance);
            frequencies.Add(double.Parse(v[2]));
        }
        return (info, distances, frequencies);
    }
}