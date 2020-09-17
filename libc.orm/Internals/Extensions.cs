using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
namespace libc.orm.Internals {
    internal static class Extensions {
        public static string ConcatString<T>(this IEnumerable<T> list,
            string delimiter, string startString = "", string endString = "") {
            var res = new StringBuilder("");
            var k = list?.ToList() ?? new List<T>();
            if (k.Any()) {
                foreach (var o in k) res.AppendFormat("{0}{1}", o, delimiter);
                res.Remove(res.Length - delimiter.Length, delimiter.Length);
            }
            res.Insert(0, startString);
            res.Append(endString);
            return res.ToString();
        }
        public static IEnumerable<ReadOnlyCollection<T>> Page<T>(this IEnumerable<T> source, int pageSize) {
            using (var enumerator = source.GetEnumerator()) {
                while (enumerator.MoveNext()) {
                    var currentPage = new List<T>(pageSize) {
                        enumerator.Current
                    };
                    while (currentPage.Count < pageSize && enumerator.MoveNext()) currentPage.Add(enumerator.Current);
                    yield return new ReadOnlyCollection<T>(currentPage);
                }
            }
        }
        public static IDictionary<string, object> ToDictionary(this object source) {
            if (source == null)
                throw new NullReferenceException(
                    "Unable to convert anonymous object to a dictionary. The source anonymous object is null.");
            var props = source.GetType().GetRuntimeProperties();
            var res = props.ToDictionary(item => item.Name, item => item.GetValue(source));
            return res;
        }
        public static void AssertNotNull(this object item, string msg = "شناسه اشتباه است") {
            if (item == null) throw new Exception(msg);
        }
        public static dynamic RemoveProperties(this object source, params string[] properties) {
            if (properties == null || properties.Length == 0) return source;
            if (source == null) return null;
            var dic = source.ToDictionary();
            foreach (var property in properties)
                if (dic.ContainsKey(property))
                    dic.Remove(property);
            dynamic res = new ExpandoObject();
            var k = (ICollection<KeyValuePair<string, object>>) res;
            foreach (var kv in dic) k.Add(kv);
            return res;
        }
    }
}