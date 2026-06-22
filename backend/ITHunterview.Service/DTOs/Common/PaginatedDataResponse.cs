using System.Collections.Generic;

namespace ITHunterview.Service.DTOs.Common
{
    public class PaginatedDataResponse<T>
    {
        public IEnumerable<T> Data { get; set; } = new List<T>();
        public PaginationMeta Meta { get; set; } = new PaginationMeta();
    }

    public class PaginationMeta
    {
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
    }
}
