using MMU.Ifosic.Models;
using Microsoft.AspNetCore.Mvc;

namespace MMU.Ifosic.Web.Pages.Shared.Components.Page;

public class PageViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(IPaginatedList values, string key, Dictionary<string,string>? parms = null, string aspPage = "./Index") 
        => View("Default", new PageViewModel(){List = values, Key = key, Parms = parms ?? new(), AspPage = aspPage});
}

public class PageViewModel
{
    public IPaginatedList? List { get; set; }
    public string Key { get; set; } = "";
    public string? AspPage { get; set; }
    public Dictionary<string, string> Parms { get; set; } = new();
}
