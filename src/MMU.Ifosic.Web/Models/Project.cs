
namespace MMU.Ifosic.Models;

public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = "Untitled";
    public string Description { get; set; } = "";
    public List<Fiber>? Fibers { get; set; }
    public int LayoutId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    public int CreatedById { get; set; }
    public int UpdatedById { get; set; }
}
