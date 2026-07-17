namespace CleanArchitecture.Application.Common.Models;

public record PagedRequest
{
    private const int MaxPageSize = 100;
    private readonly int _page = 1;
    private readonly int _pageSize = 20;

    public int Page
    {
        get => _page;
        init => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value < 1 ? 20 : Math.Min(value, MaxPageSize);
    }

    public string? Search { get; init; }
}
