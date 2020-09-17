using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using libc.orm.Internals;
using libc.orm.Models;
using SqlKata.Compilers;
namespace libc.orm.DatabaseConnection {
    public class CompilerHelper {
        private readonly Compiler c;
        public CompilerHelper(Compiler c) {
            this.c = c;
            Batch = new CompilerBatchHelper(c);
            Transaction = new CompilerTransactionHelper(c);
            Update = new CompilerUpdateHelper(c);
            Insert = new CompilerInsertHelper(c);
            Select = new CompilerSelectHelper(c);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="inQueryOrParameter">this can be @parameterName or a select * from ... query</param>
        /// <returns></returns>
        public string ToInQuery(string inQueryOrParameter) {
            if (c is PostgresCompiler) {
                return $" = ANY ({inQueryOrParameter})";
            }
            return $" in {inQueryOrParameter}";
        }
        public CompilerBatchHelper Batch { get; }
        public CompilerTransactionHelper Transaction { get; }
        public CompilerUpdateHelper Update { get; }
        public CompilerInsertHelper Insert { get; }
        public CompilerSelectHelper Select { get; }
        public class CompilerBatchHelper {
            private static readonly System.Text.RegularExpressions.Regex paramRegex =
                new System.Text.RegularExpressions.Regex(@"(?<!\w)@\w+");
            private readonly Compiler c;
            public CompilerBatchHelper(Compiler c) {
                this.c = c;
            }
            public int ParametersCount(string query) {
                return paramRegex.Matches(query).Count;
            }
            public int GetBatchSize(string query) {
                const decimal paramLimit = 500;
                var paramPerQuery = ParametersCount(query);
                var batchSize = (int) Math.Floor(paramLimit / paramPerQuery);
                return batchSize;
            }
            public IEnumerable<QueryBatch> Query<T>(string query, IEnumerable<T> parameters) {
                var res = new List<QueryBatch>();
                var batchSize = GetBatchSize(query);
                foreach (var batch in parameters.Page(batchSize)) {
                    var batchQuery = new StringBuilder();
                    var batchParameters = new Dictionary<string, object>();
                    for (var i = 0; i < batch.Count; i++) {
                        var q = paramRegex.Replace(query, x => $"{x}{i}");
                        batchQuery.AppendLine($"{q};");
                        var p = batch[i].ToDictionary().ToDictionary(a => $"{a.Key}{i}", a => a.Value);
                        foreach (var param in p) batchParameters.Add(param.Key, param.Value);
                    }
                    res.Add(new QueryBatch {
                        Query = batchQuery.ToString(),
                        Parameters = batchParameters
                    });
                }
                return res;
            }
        }
        public class CompilerTransactionHelper {
            private readonly Compiler c;
            public CompilerTransactionHelper(Compiler c) {
                this.c = c;
            }
            public string WrapInTransaction(string query) {
                if (c is SqlServerCompiler) return $"BEGIN TRAN;\n{query}\nCOMMIT;";
                if (c is MySqlCompiler) return $"START TRANSACTION;\n{query}\nCOMMIT;";
                if (c is SqliteCompiler) return $"BEGIN TRANSACTION;\n{query}\nCOMMIT;";
                throw new NotImplementedException("only sqlserver, mysql and sqlite are supported");
            }
        }
        public class CompilerUpdateHelper {
            private readonly Compiler c;
            public CompilerUpdateHelper(Compiler c) {
                this.c = c;
            }
            public string SetExclude<T>(IEnumerable<string> excludedProperties) {
                return SetExclude(typeof(T), excludedProperties);
            }
            public string SetExclude(Type type, IEnumerable<string> excludedProperties) {
                var props = new Reflector(type).GetMemberNames();
                if (excludedProperties != null) props = props.Except(excludedProperties);
                return Set(props);
            }
            public string Set(IEnumerable<string> properties) {
                return properties
                    .Select(a => $"{a.Quote(c)} = @{a}")
                    .ConcatString(", ", " ", " ");
            }
        }
        public class CompilerInsertHelper {
            private readonly Compiler c;
            public CompilerInsertHelper(Compiler c) {
                this.c = c;
            }
            public string InsertInto<T>(IEnumerable<string> excludedProperties) {
                return InsertInto(typeof(T), excludedProperties);
            }
            public string InsertInto(Type type, IEnumerable<string> excludedProperties) {
                var props = new Reflector(type).GetMemberNames();
                if (excludedProperties != null) props = props.Except(excludedProperties);
                return InsertInto(props.ToArray());
            }
            public string InsertInto(string[] properties) {
                var into = properties.Select(a => a.Quote(c)).ConcatString(", ", " (", ") ");
                var values = properties.Select(a => $"@{a}").ConcatString(", ", " (", ") ");
                return $"{into} values {values}";
            }
        }
        public class CompilerSelectHelper {
            private readonly Compiler c;
            public CompilerSelectHelper(Compiler c) {
                this.c = c;
            }
            public string[] NoAlias(Type type, bool quote, string tableNameOrAlias, params string[] excludedProperties) {
                return Col._NoAlias(c, quote, type, tableNameOrAlias, excludedProperties)
                    .Select(a => a.ToString())
                    .ToArray();
            }
            public string[] NoAlias<T>(bool quote, string tableNameOrAlias, params string[] excludedProperties) {
                return NoAlias(typeof(T), quote, tableNameOrAlias, excludedProperties);
            }
            public string[] AliasWithTableNameAsPrefix(Type type, bool quote, string tableNameOrAlias,
                params string[] excludedProperties) {
                return Col._AliasWithTableNameAsPrefix(c, quote, type, tableNameOrAlias, excludedProperties)
                    .Select(a => a.ToString())
                    .ToArray();
            }
            public string[] AliasWithTableNameAsPrefix<T>(bool quote, string tableNameOrAlias,
                params string[] excludedProperties) {
                return AliasWithTableNameAsPrefix(typeof(T), quote, tableNameOrAlias, excludedProperties);
            }
            public string[] AliasWithCustomPrefix(Type type, bool quote, string tableNameOrAlias, string aliasPrefix,
                params string[] excludedProperties) {
                return Col._AliasWithCustomPrefix(c, quote, type, tableNameOrAlias, aliasPrefix, excludedProperties)
                    .Select(a => a.ToString())
                    .ToArray();
            }
            public string[] AliasWithCustomPrefix<T>(bool quote, string tableNameOrAlias, string aliasPrefix,
                params string[] excludedProperties) {
                return AliasWithCustomPrefix(typeof(T), quote, tableNameOrAlias, aliasPrefix, excludedProperties);
            }
            private class Col {
                private readonly Compiler c;
                private readonly bool quote;
                private Col(string tableNameOrAlias, string column, string alias, Compiler c, bool quote) {
                    TableNameOrAlias = tableNameOrAlias;
                    Column = column;
                    Alias = alias;
                    this.c = c;
                    this.quote = quote;
                }
                private string TableNameOrAlias { get; }
                private string Column { get; }
                private string Alias { get; }
                public override string ToString() {
                    if (quote) {
                        var colSelection = string.IsNullOrWhiteSpace(TableNameOrAlias)
                            ? Column.Quote(c)
                            : $"{TableNameOrAlias}.{Column.Quote(c)}";
                        if (string.IsNullOrWhiteSpace(Alias)) return colSelection;
                        return $"{colSelection} as {Alias.Quote(c)}";
                    } else {
                        var colSelection = string.IsNullOrWhiteSpace(TableNameOrAlias)
                            ? Column
                            : $"{TableNameOrAlias}.{Column}";
                        if (string.IsNullOrWhiteSpace(Alias)) return colSelection;
                        return $"{colSelection} as {Alias}";
                    }
                }
                public static IEnumerable<Col> _NoAlias(Compiler c, bool quote, Type type, string tableNameOrAlias,
                    params string[] excludedProperties) {
                    return _create(type, excludedProperties, prop => new Col(tableNameOrAlias, prop, null, c, quote));
                }
                public static IEnumerable<Col> _AliasWithTableNameAsPrefix(Compiler c, bool quote, Type type,
                    string tableNameOrAlias, params string[] excludedProperties) {
                    return _create(type, excludedProperties,
                        prop => new Col(tableNameOrAlias, prop, $"{tableNameOrAlias}{prop}", c, quote));
                }
                public static IEnumerable<Col> _AliasWithCustomPrefix(Compiler c, bool quote, Type type, string tableNameOrAlias,
                    string aliasPrefix,
                    params string[] excludedProperties) {
                    return _create(type, excludedProperties,
                        prop => new Col(tableNameOrAlias, prop, $"{aliasPrefix}{prop}", c, quote));
                }
                private static IEnumerable<Col> _create(Type type, string[] excludedProperties, Func<string, Col> resolve) {
                    return new Reflector(type)
                        .GetMemberNames()
                        .Where(a => excludedProperties == null || !excludedProperties.Contains(a))
                        .Select(resolve)
                        .ToArray();
                }
            }
        }
    }
    public class QueryBatch {
        public string Query { get; set; }
        public IDictionary<string, object> Parameters { get; set; }
    }
}