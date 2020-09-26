using System;
namespace libc.orm.QueryFilters {
    public class PageRequest {
        public const int MaxRowsPerPage = 1000;
        /// <summary>
        ///     search term
        /// </summary>
        public string Search { get; set; }
        /// <summary>
        ///     sort array
        /// </summary>
        public PageRequestSort[] Sorts { get; set; } = new PageRequestSort[0];
        /// <summary>
        ///     page number. 1 based
        /// </summary>
        public int Page { get; set; }
        /// <summary>
        ///     number of rows per page
        /// </summary>
        public int RowsPerPage { get; set; }
        /// <summary>
        ///     valid number of rows per page
        /// </summary>
        /// <returns></returns>
        public int GetRowsPerPage() {
            return RowsPerPage <= 0 ? MaxRowsPerPage : Math.Min(MaxRowsPerPage, RowsPerPage);
        }
        /// <summary>
        ///     has sorting enabled
        /// </summary>
        /// <returns></returns>
        public bool Sortable() {
            return Sorts.Length > 0;
        }
        /// <summary>
        ///     has search term
        /// </summary>
        /// <returns></returns>
        public bool HasSearch() {
            return !string.IsNullOrWhiteSpace(Search);
        }
    }
    public class PageRequestSort {
        public string Column { get; set; }
        public bool Ascending { get; set; }
    }
}