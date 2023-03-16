using Microsoft.EntityFrameworkCore;
using System.Collections;

namespace MMU.Ifosic.Models;

public static class PagingExtension
{
    public static async Task<PaginatedList<T>> PaginateAsync<T>(this IQueryable<T> source, int pageIndex = 1, int pageSize = 10) //where T:class
    {
        var count = await source.CountAsync();
        var items = await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PaginatedList<T>(items, count, pageIndex, pageSize);
    }
}

public interface IPaginatedList : IList
{
    int Numbering { get; }
    int PageIndex { get; }
    int TotalPages { get; }
    int PageDisplayed { get; }
    int PageStart { get; }
    int PreviousPage { get; }
    int NextPage { get; }
    bool HasPreviousPage { get; }
    bool HasNextPage { get; }    
}

public class PaginatedList<T> : List<T>, IPaginatedList //where T:class
{
    public int Numbering { get; init; }
    public int PageIndex { get; init; }
    public int TotalPages { get; init; }
    public int PageDisplayed { get; set; } = 10;
    public int PageStart => Math.Max(1, PageIndex - PageDisplayed / 2);
    public int TotalItems { get; set; }

    public PaginatedList() { }
    public PaginatedList(List<T> items, int count, int pageIndex = 1, int pageSize = 10)
    {
        TotalItems = count;
        PageIndex = pageIndex;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        Numbering = pageSize * (pageIndex - 1);
        AddRange(items);
    }

    public int PreviousPage => PageIndex - 1;
    public int NextPage => PageIndex + 1;

    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;

}
