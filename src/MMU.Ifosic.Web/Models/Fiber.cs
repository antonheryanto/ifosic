using System.ComponentModel.DataAnnotations.Schema;

namespace MMU.Ifosic.Models;

public class Fiber
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public double Coefficient { get; set; }
    public List<Project>? Projects { get; set; }
	[NotMapped] public bool IsActive { get; set; }
}
