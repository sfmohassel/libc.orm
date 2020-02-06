using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dapper;
using libc.models.Extensions;
using libc.models.Reflection;
using libc.orm.Models.Interfaces;
using libc.orm.QueryFilters;
using NodaTime;
using SqlKata;
using SqlKata.Compilers;
namespace libc.orm.Models {
    public static class DataModelExtensions {
        private const string COLUMN = nameof(IData.UpdateUtc);
        /// <summary>
        ///     Remember the 2100, 64000, 32000, 16000 parameters limitation in sqlserver, oracle, mysql, postgres
        ///     So here we bypass that issue by creating sql dynamically
        /// </summary>
        /// <param name="query"></param>
        /// <param name="items"></param>
        /// <param name="fn"></param>
        /// <returns></returns>
        public static List<Query> BatchItems<T>(this Query query, IEnumerable<T> items,
            Func<(Query query, IEnumerable<T> batch), Query> fn) {
            // sql server parameter limit per query is 2100. Don't change this. 900 is reserved for query
            // parameters
            const int limit = 1200;
            var enumerable = items?.ToList() ?? new List<T>();
            var res = new List<Query>();
            var i = 0;
            while (i < enumerable.Count) {
                var batch = enumerable.Skip(i).Take(limit);
                var q = fn((query.Clone(), batch));
                res.Add(q);
                i += limit;
            }
            return res;
        }
        /// <summary>
        ///     Remember the 2100, 64000, 32000, 16000 parameters limitation in sqlserver, oracle, mysql, postgres
        ///     So here we bypass that issue by creating sql dynamically
        /// </summary>
        /// <param name="query"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public static List<Query> AsInsertManyExtended(this Query query, IEnumerable<object> items) {
            //sql server parameter limit per query is 2100. Don't change this
            const double limit = 2000;
            var enumerable = items?.ToList() ?? new List<object>();
            if (enumerable.Count == 0) return new List<Query>();
            var colsCount = enumerable[0].ToDictionary().Keys.Count;
            var rowsCount = (int) Math.Floor(limit / colsCount);
            if (rowsCount == 0) rowsCount = enumerable.Count;
            var res = new List<Query>();
            var i = 0;
            while (i < enumerable.Count) {
                var batch = enumerable.Skip(i).Take(rowsCount);
                var q = query.Clone().asInsertManyExtended(batch);
                res.Add(q);
                i += rowsCount;
            }
            return res;
        }
        private static Query asInsertManyExtended(this Query query, IEnumerable<object> items) {
            var list = items.AsList();
            var cols = list[0].ToDictionary().Keys;
            var rows = list.Select(a => a.ToDictionary().Values);
            var res = query.AsInsert(cols, rows);
            return res;
        }
        public static Query AsUpdateExtended(this Query query, ICollection<string> cols,
            ICollection<object> values, IData x = null) {
            if (cols.Contains(COLUMN)) return query.AsUpdate(cols, values);
            var updateUtc = SystemClock.Instance.GetCurrentInstant().ToUnixTimeTicks();
            if (x != null) x.UpdateUtc = updateUtc;
            cols.Add(COLUMN);
            values.Add(updateUtc);
            return query.AsUpdate(cols, values);
        }
        public static Query AsUpdateExtended(this Query query, IDictionary<string, object> data,
            IData x = null) {
            if (data.ContainsKey(COLUMN)) return query.AsUpdate(data);
            var updateUtc = SystemClock.Instance.GetCurrentInstant().ToUnixTimeTicks();
            if (x != null) x.UpdateUtc = updateUtc;
            data[COLUMN] = updateUtc;
            return query.AsUpdate(new ReadOnlyDictionary<string, object>(data));
        }
        public static Query AsUpdateExtended(this Query query, object data, IData x = null) {
            var k = data.ToDictionary();
            if (k.ContainsKey(COLUMN)) return query.AsUpdate(data);
            var updateUtc = SystemClock.Instance.GetCurrentInstant().ToUnixTimeTicks();
            if (x != null) x.UpdateUtc = updateUtc;
            k[COLUMN] = updateUtc;
            return query.AsUpdate(new ReadOnlyDictionary<string, object>(k));
        }
        public static string Quote(this string tableOrColumn, Compiler c) {
            return c.Wrap(tableOrColumn);
            /*if (c is MySqlCompiler) {
                return $"`{tableOrColumn}`";
            }
            if (c is SqlServerCompiler) {
                return $"[{tableOrColumn}]";
            }
            return tableOrColumn;*/
        }
        public static Query Paginate(this Query query, Compiler compiler, PageRequest request) {
            return query.Paginate(compiler, request.Page, request.GetRowsPerPage());
        }
        public static Query Paginate(this Query query, Compiler compiler, int page, int perPage) {
            if (compiler is SqlServerCompiler) {
                var skip = (page - 1) * perPage;
                var take = perPage;
                return query.CombineRaw($"offset {skip} rows fetch next {take} rows only");
            }
            return query.ForPage(page, perPage);
        }
        public static T ParsePrefixed<T>(this IDictionary<string, object> row, string prefix) where T : new() {
            var res = new T();
            //parse
            var items = row.Where(a => a.Key.StartsWith(prefix));
            var reflector = new Reflector(typeof(T), res);
            foreach (var item in items) {
                var prop = item.Key.Substring(prefix.Length);
                reflector.Set(prop, item.Value, false);
            }
            return res;
        }
    }
}