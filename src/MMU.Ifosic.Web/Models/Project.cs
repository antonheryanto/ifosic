
using System.ComponentModel.DataAnnotations.Schema;

namespace MMU.Ifosic.Models;

public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = "Untitled";
    public string Description { get; set; } = "";
    [NotMapped]
    public int NumberOfFiber { get; set; } = 1;
    public string Measurement { get; set; } = "Pressure";
    public List<Fiber>? Fibers { get; set; }
    
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    public int CreatedById { get; set; }
    public int UpdatedById { get; set; }
    
    public int LayoutId { get; set; }

    public string Layout => Layouts.TryGetValue(LayoutId, out var layout) ? layout : Layouts[1];
    [NotMapped]
    public static readonly Dictionary<int, string> Layouts = new()
    {
        {1, "Serial" },
        {2, "Parallel Connect to Optical Switch" },
        {3, "Parallel Connect to Interrogator Unit (IO)"},
	};

    [NotMapped]
    public static readonly Dictionary<int, string> Measurements = new()
    {
        {1, "Pressure" },
        {2, "Temperature" },
        {3, "Strain"},
    };
}
