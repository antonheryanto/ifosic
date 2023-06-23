using MMU.Ifosic.Neubrex;
using System.ComponentModel.DataAnnotations.Schema;
using MessagePack;
using MessagePack.Resolvers;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MMU.Ifosic.Models;

public partial class Project : ObservableObject
{
    public int Id { get; set; }
    [ObservableProperty] private string _name = "Untitled";
    [ObservableProperty] private string _description = "";
    [ObservableProperty] private int _layoutId;
    [NotMapped]
    [ObservableProperty] private int _numberOfFiber = 1;
    [ObservableProperty] private string _measurement = "Pressure";
    public List<Fiber>? Fibers { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    public int CreatedById { get; set; }
    public int UpdatedById { get; set; }
    [NotMapped] public OpticalSwitch Switch { get; set; } = new();
    [NotMapped] public SessionRunner Runner { get; set; } = new();
    [NotMapped] public List<FrequencyShiftDistance> Items { get; set; } = new();

    public string Layout => Layouts.TryGetValue(LayoutId, out var layout) ? layout : Layouts[1];
    [NotMapped]
    public static readonly Dictionary<int, string> Layouts = new()
    {
        {1, "Serial" },
        {2, "Parallel" },
        {3, "Parallel 2"},
	};

    [NotMapped]
    public static readonly Dictionary<int, string> Measurements = new()
    {
        {1, "Pressure" },
        {2, "Temperature" },
        {3, "Strain"},
    };

    public static Project? Load(string fileName) => FromMessagePack<Project>(fileName);

    public bool Save(string fileName) => ToMessagePack(this, fileName);

    private static readonly MessagePackSerializerOptions _options = ContractlessStandardResolver.Options
        .WithCompression(MessagePackCompression.Lz4BlockArray);

    public static T? FromMessagePack<T>(string fileName, MessagePackSerializerOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return default;
        options ??= _options;
        var bin = File.ReadAllBytes(fileName);
        return MessagePackSerializer.Deserialize<T>(bin, options);
    }

    
    public static bool ToMessagePack<T>(T value, string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;
        var bytes = MessagePackSerializer.Serialize(value, _options);
        File.WriteAllBytes(fileName, bytes);
        return true;
    }
}
