using System;

namespace SMS.Web.Pages.Shared.Pagination
{
    public class PaginationInfo
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public string ItemName { get; set; } = "items";

        public int TotalPages => PageSize > 0
            ? (int)Math.Ceiling(TotalCount / (double)PageSize)
            : 0;

        public int FirstItemNumber => TotalCount == 0
            ? 0
            : ((PageNumber - 1) * PageSize) + 1;

        public int LastItemNumber => Math.Min(PageNumber * PageSize, TotalCount);

        public bool HasPrevious => PageNumber > 1;
        public bool HasNext => PageNumber < TotalPages;
    }
}