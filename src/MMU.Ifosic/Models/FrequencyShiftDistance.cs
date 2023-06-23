using MessagePack;
using MessagePack.Resolvers;
using NPOI.POIFS.FileSystem;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.IO.Compression;
using System.Text;

namespace MMU.Ifosic.Models;

public class Group
{
	public Group(int x = 0, int y = 0, int id = -1)
	{
		Start = x;
		Stop = y;
        Id = id;
	}

	public int Start { get; set; }
	public int Stop { get; set; }
	public int Id { get; set; } = -1;

	public override string ToString() => $"{Start},{Stop},{Id}";
}

public class Measurement
{
    public string Name { get; set; } = "Pressure";
    public string Unit { get; set; } = "MPa";
    public List<(DateTime Date, double Value)> Values { get; set; } = new();
}


public partial class FrequencyShiftDistance
{
    public string Name { get; set; } = "";
    public Dictionary<string, string> Info { get; set; } = new();
    public List<double> Distance { get; set; } = new();    
    public List<double> Boundaries { get; set; } = new();
    //public List<int> PredictedBoundaries { get; set; } = new();
    public List<int> BoundaryIndexes { get; set; } = new();	
	public Dictionary<string, List<(DateTime Date, double Value)>> References { get; set; } = new();
	public List<double[]> Traces { get; set; } = new();
    public List<DateTime?> MeasurementStart { get; set; } = new();
    public List<DateTime?> MeasurementEnd { get; set; } = new();
    public List<int> Categories { get; set; } = new();
	public List<int> TimeBoundaries { get; set; } = new();

	public void GetBoundary(string fileName)
    {
		using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        GetBoundary(stream);
	}

	public void GetBoundary(Stream stream)    
    {
		using var reader = new StreamReader(stream);
        Boundaries = new();
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (string.IsNullOrEmpty(line))
                continue;
            var cols = line.Split(':');
            if (cols[0].StartsWith("Bou"))
            {
                var values = cols[1].Split(",");
                for (int i = 0; i < values.Length; i++)
                {
                    var raw = values[i].Split('/');
                    Boundaries.Add(double.TryParse(raw[0], out var value) ? value : 0);
                }
                break;
            }
        }
        BoundaryIndexes = new(ToBoundariesIndex(Boundaries));
	}

    public void AddReference(string fileName)
    {
        var ext = Path.GetExtension(fileName);
        if (ext == ".csv")
            GetReferenceCsv(fileName);
        else if (ext.StartsWith(".xls")) {
            using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            GetReferenceExcel(stream);
        }
    }

    private void GetReferenceExcel(Stream stream)
    {
        using XSSFWorkbook xssWorkbook = new(stream);
        var sheet = xssWorkbook.GetSheetAt(0);
        IRow headerRow = sheet.GetRow(sheet.FirstRowNum); // start from row 1
        // get the name and unit of reference data
        var fullname = (headerRow.GetCell(headerRow.FirstCellNum + 1)?.StringCellValue ?? "").Split("(");
        var name = fullname[0]?.Trim() ?? "Pressure";
        var unit = fullname.Length > 0 ? fullname[1].Trim(')') : "";
        var rows = new List<(DateTime Date, double Value)>();
        for (int i = (sheet.FirstRowNum + 1); i <= sheet.LastRowNum; i++)
        {
            if (!(sheet.GetRow(i) is var row) || row is null || row.Cells.All(d => d.CellType == CellType.Blank)) 
                continue;

            var date = row.GetCell(row.FirstCellNum)?.DateCellValue ?? DateTime.Now;
            var time = row.GetCell(row.FirstCellNum + 1)?.DateCellValue ?? DateTime.Now;
            var value = row.GetCell(row.FirstCellNum + 3)?.NumericCellValue ??
                (double.TryParse(row.GetCell(row.FirstCellNum + 3)?.ToString(), out var v) ? v : 0);
            var datetime = new DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second);
            rows.Add((datetime, value));
        }
        References[name] = rows;
    }

    private void GetReferenceCsv(string fileName, string name = "Reference")
    {
        using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        var rows = new List<(DateTime Date, double Value)>();
		bool headerParse = false;
        int i = 0;
        var now = DateTime.Now;
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

            rows.Add((DateTime.TryParse(cols[0], out var date) ? date
                : new DateTime(now.Year, now.Month, now.Day).AddHours(i++),
                double.TryParse(cols[1], out var value) ? value : 0));
		}
        References[name] = rows;
    }

	public static FrequencyShiftDistance? LoadBin(string fileName) => FromMessagePack(fileName);

	public static FrequencyShiftDistance? Load(string fileName)
    {
        var ext = Path.GetExtension(fileName);
        if (ext == ".bin")
            return LoadBin(fileName);
        var o = new FrequencyShiftDistance {  Name = Path.GetFileName(fileName) };
        if (ext != ".zip")
            return o;
        o.ExtractFromZip(fileName);
        //o.Save(fileName.Replace(".zip", ".bin"));
        return o;
    }

    public static FrequencyShiftDistance? LoadFolder(string path)
    {
        var di = new DirectoryInfo(path);
        if (!di.Exists)
            return null;
        var o = new FrequencyShiftDistance { Name = di.Name };
        foreach (var file in di.GetFiles("*.fdd", SearchOption.AllDirectories))
        {
            if (file.Name.StartsWith("Baseline", StringComparison.OrdinalIgnoreCase))
                continue;
            using var stream = new FileStream(file.FullName,
                FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var (i, d, f) = o.Extract(stream);
            if (o.Distance.Count == 0)
                o.Distance.AddRange(d);
            if (o.Info.Count == 0)
            {
                foreach (var (k, v) in i)
                    o.Info.Add(k, v);
            }
            o.Traces.Add(f.ToArray());
        }

        return o;
    }

    public int ToBoundariesIndex(double boundary)
	{
		for (int i = 0; i < Distance.Count; i++)
		{
			if (Distance[i] > boundary)
			{
                return i;
			}
		}
		return 0;
	}

	public int[] ToBoundariesIndex(IList<double> boundaries)
	{
		var indexes = new int[boundaries.Count];
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
            if (entry.Name.EndsWith(".fdd"))
            {
                var (i, d, f) = Extract(entry.Open());
                MeasurementStart.Add(DateTime.TryParse(i["Measurement Start"], out var ms) ? ms : null);
                MeasurementEnd.Add(DateTime.TryParse(i["Measurement End"], out var me) ? me : null);
                TimeBoundaries.Add(int.TryParse(i["Optical SW Port Number"], out var port) ? port : 1);
                if (Distance.Count == 0)
                    Distance.AddRange(d);
                if (Info.Count == 0)
                {
                    foreach (var (k, v) in i)
                        Info.Add(k, v);
                }
                Traces.Add(f.ToArray());
            }
            else if (entry.Name.EndsWith("Results.txt"))
            {
                GetBoundary(entry.Open());
            }
            else if (entry.Name.EndsWith("vs Time.xlsx"))
            {
                GetReferenceExcel(entry.Open());
            }
        }
    }

    private const string DATE_FORMAT = "yyyy/MM/dd HH:mm:ss.ffffff";
    private string ToText(double[] trace, int port = 1, DateTime? ms = null, DateTime? me = null)
    {
        var b = new StringBuilder();
		foreach (var (k, v) in Info)
		{
            var vn = v;
            if (k == "Optical SW Port Number")
                vn = port.ToString();
            if (ms is not null && k == "Measurement Start")
                vn = ms?.ToString(DATE_FORMAT);
			if (me is not null && k == "Measurement End")
				vn = me?.ToString(DATE_FORMAT);
			b.AppendLine($"{k.PadRight(24)} {vn}");
		}
        for (int j = Info.Count; j < 103; j++)
        {
			b.AppendLine();
		}
		b.Append("No.".PadRight(16));
        b.Append("Distance(m)".PadRight(26));
        b.AppendLine("Frequency Diff, GHz ");
		for (int j = 0; j < trace.Length; j++)
        {
            b.Append(j.ToString().PadRight(16));
            b.Append(Distance[j].ToString("0.000").PadRight(26));
            b.AppendLine($"{trace[j]:0.000000}");
        }
        return b.ToString();
    }

    public bool ToZip(string fileName, string? refFile = null, IList<double[]>? traces = null, IList<int>? timeIndex = null,
        IList<double>? boundaries = null)
	{
		traces ??= Traces;
		using var zip = ZipFile.Open(fileName, ZipArchiveMode.Create);
		var dir = "RawData_FreqShift-Distance_fdd/";
		zip.CreateEntry(dir, CompressionLevel.Optimal);
		for (int i = 0; i < traces.Count; i++)
		{
			var name = $"{dir}FD_{MeasurementStart[i]:yyyyMMdd-HHmmss.ffffff}.fdd";
			var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
			using var entryStream = entry.Open();
			using var writer = new StreamWriter(entryStream);
			var info = ToText(traces[i], timeIndex is null ? 1 : timeIndex[i], MeasurementStart[i], MeasurementEnd[i]);
			writer.WriteLine(info);
		}

        boundaries ??= Boundaries;
        if (boundaries is not null)
        {
            var result = zip.CreateEntry("Results.txt", CompressionLevel.Optimal);
            using var resultStream = result.Open();
            using var resultWriter = new StreamWriter(resultStream);
            resultWriter.WriteLine($"Boundaries: {string.Join(',', boundaries)}");
        }

        if (refFile is not null)
        {
			var refEntry = zip.CreateEntry(Path.GetFileName(refFile), CompressionLevel.Optimal);
            using var refEntryStream = refEntry.Open();
			using var refStream = new FileStream(refFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			refStream.CopyTo(refEntryStream);
		}
		return true;
	}

	public bool Save(string fileName) => ToMessagePack(this, fileName);

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

	public static FrequencyShiftDistance? FromMessagePack(string fileName)
	{
		if (string.IsNullOrWhiteSpace(fileName))
			return null;
		var bin = File.ReadAllBytes(fileName);
		return MessagePackSerializer.Deserialize<FrequencyShiftDistance>(bin, _options);
	}

	static readonly MessagePackSerializerOptions _options = ContractlessStandardResolver.Options
	    .WithCompression(MessagePackCompression.Lz4BlockArray);

    public bool ToMessagePack(string fileName) => ToMessagePack(this, fileName);

    public static bool ToMessagePack<T>(T value, string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;
        var bytes = MessagePackSerializer.Serialize(value, _options);
        File.WriteAllBytes(fileName, bytes);
        return true;
    }
}