using System;
using System.Linq;
using libc.orm.DatabaseConnection;
using libc.orm.Models;
using SqlKata;
namespace libc.orm.QueryFilters {
    public static class Pager {
        public static PageResponse<TResult> Run<TResult>(DbConn db, PageRequest request, Query query,
            Func<Query, PageRequest, Query> search, string defaultSortColumn = "CreateUtc") {
            var res = new PageResponse<TResult>();

            //count
            var countQuery = createSearchQuery(request, query, search).AsCount();
            res.TotalRows = db.ExecuteScalar<int>(countQuery);
            var pageQuery = createSearchQuery(request, query, search);

            //sort
            if (request.Sortable())
                foreach (var sort in request.Sorts)
                    pageQuery = sort.Ascending ? pageQuery.OrderBy(sort.Column) : pageQuery.OrderByDesc(sort.Column);
            else //there must be at least one column sorted for pagination
                pageQuery = pageQuery.OrderBy(defaultSortColumn);

            //page
            pageQuery = pageQuery.Paginate(db.Compiler, request);
            res.Rows = db.Query<TResult>(pageQuery).ToList();
            return res;
        }
        private static Query createSearchQuery(PageRequest request, Query query,
            Func<Query, PageRequest, Query> search) {
            var res = query.Clone();
            if (request.HasSearch() && search != null) res = search(res, request);
            return res;
        }
    }
}