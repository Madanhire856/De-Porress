using SMS.Web.Pages.Shared.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SMS.Web.Pages.Shared.Pagination
{
    public class PaginationResult<T>
    {
        public List<T> Items { get; set; } = new();
        public PaginationInfo Pagination { get; set; } = new();
    }

    public static class PaginationExtensions
    {
        public static PaginationResult<T> Paginate<T>(
            this IQueryable<T> query,
            int pageNumber,
            int pageSize,
            string itemName = "items")
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var totalCount = query.Count();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages > 0 && pageNumber > totalPages) pageNumber = totalPages;

            var items = query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PaginationResult<T>
            {
                Items = items,
                Pagination = new PaginationInfo
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    ItemName = itemName
                }
            };
        }

        public static async Task<PaginationResult<T>> PaginateAsync<T>(
            this IQueryable<T> query,
            int pageNumber,
            int pageSize,
            string itemName = "items")
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var totalCount = await Task.FromResult(query.Count());
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages > 0 && pageNumber > totalPages) pageNumber = totalPages;

            var items = await Task.FromResult(query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList());

            return new PaginationResult<T>
            {
                Items = items,
                Pagination = new PaginationInfo
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    ItemName = itemName
                }
            };
        }
    }
}