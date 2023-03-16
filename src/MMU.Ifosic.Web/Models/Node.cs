using System;
using System.ComponentModel.DataAnnotations;

namespace MMU.Ifosic.Models;

public class Node
{
    public int Id { get; set; }
    public string Title { get; set; } = "Untitled";
    [MaxLength(8192)]
    public string Body { get; set; } = "";
    public bool IsPublished { get; set; }
    public int UserId { get; set; }
    public DateTime? CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; } = DateTime.UtcNow;
}
