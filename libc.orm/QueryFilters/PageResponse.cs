using System.Collections.Generic;

namespace libc.orm.QueryFilters
{
    public class PageResponse<TRow>
    {
        /// <summary>
        ///     total number of rows
        /// </summary>
        public int TotalRows { get; set; }

        /// <summary>
        ///     rows of page
        /// </summary>
        public List<TRow> Rows { get; set; } = new List<TRow>();
    }
}