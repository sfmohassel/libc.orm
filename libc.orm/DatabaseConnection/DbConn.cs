using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using SqlKata;
using SqlKata.Compilers;

namespace libc.orm.DatabaseConnection
{
    public class DbConn : IDisposable
    {
        private readonly Action<SqlResult> logger;
        private readonly bool wasClosed;

        public DbConn(IDbConnection db, Compiler compiler, Action<SqlResult> logger)
        {
            Compiler = compiler;
            this.logger = logger;
            Connection = db;
            wasClosed = db.State == ConnectionState.Closed;
            if (wasClosed) db.Open();
        }

        public IDbConnection Connection { get; }
        public Compiler Compiler { get; }
        public IDbTransaction Transaction { get; private set; }

        public void Dispose()
        {
            try
            {
                Connection.Dispose();
            }
            finally
            {
                try
                {
                    if (wasClosed && Connection.State == ConnectionState.Open) Connection.Close();
                }
                catch
                {
                    // ignored
                }
            }
        }

        public IDbTransaction BeginTransaction(IsolationLevel il)
        {
            Transaction = Connection.BeginTransaction(il);

            return Transaction;
        }

        public IDbTransaction BeginTransaction()
        {
            Transaction = Connection.BeginTransaction();

            return Transaction;
        }

        public int Execute(List<Query> sql, int? commandTimeout = null, CommandType? commandType = null)
        {
            var res = new int[sql.Count];
            for (var i = 0; i < sql.Count; i++) res[i] = Execute(sql[i], commandTimeout, commandType);

            return res.Sum();
        }

        private SqlResult compile(Query query)
        {
            var res = Compiler.Compile(query);
            log(res);

            return res;
        }

        private void log(SqlResult sql)
        {
            logger?.Invoke(sql);
        }

        private void log(string sql, object param)
        {
            var res = new SqlResult
            {
                Sql = sql,
                Bindings =
                {
                    param
                }
            };

            logger?.Invoke(res);
        }

        /// <summary>
        ///     Execute parameterized SQL.
        /// </summary>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>The number of rows affected.</returns>
        public int Execute(Query sql, int? commandTimeout = null, CommandType? commandType = null)
        {
            var s = compile(sql);

            return Connection.Execute(s.Sql, s.NamedBindings, Transaction, commandTimeout, commandType);
        }

        /// <summary>
        ///     Execute parameterized SQL that selects a single value.
        /// </summary>
        /// <param name="sql">The SQL to execute.</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>The first cell selected as <see cref="object" />.</returns>
        public object ExecuteScalar(Query sql, int? commandTimeout = null, CommandType? commandType = null)
        {
            var s = compile(sql);

            return Connection.ExecuteScalar(s.Sql, s.NamedBindings, Transaction, commandTimeout, commandType);
        }

        /// <summary>
        ///     Execute parameterized SQL that selects a single value.
        /// </summary>
        /// <typeparam name="T">The type to return.</typeparam>
        /// <param name="sql">The SQL to execute.</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>The first cell returned, as <typeparamref name="T" />.</returns>
        public T ExecuteScalar<T>(Query sql, int? commandTimeout = null, CommandType? commandType = null)
        {
            var s = compile(sql);

            return Connection.ExecuteScalar<T>(s.Sql, s.NamedBindings, Transaction, commandTimeout, commandType);
        }

        /// <summary>
        ///     Execute parameterized SQL and return an <see cref="IDataReader" />.
        /// </summary>
        /// <param name="sql">The SQL to execute.</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An <see cref="IDataReader" /> that can be used to iterate over the results of the SQL query.</returns>
        /// <remarks>
        ///     This is typically used when the results of a query are not processed by Dapper, for example, used to fill a
        ///     <see cref="DataTable" />
        ///     or <see cref="T:DataSet" />.
        /// </remarks>
        /// <example>
        ///     <code>
        /// <![CDATA[
        /// DataTable table = new DataTable("MyTable");
        /// using (var reader = ExecuteReader(cnn, sql, param))
        /// {
        ///     table.Load(reader);
        /// }
        /// ]]>
        /// </code>
        /// </example>
        public IDataReader ExecuteReader(Query sql, int? commandTimeout = null,
            CommandType? commandType = null)
        {
            var s = compile(sql);

            return Connection.ExecuteReader(s.Sql, s.NamedBindings, Transaction, commandTimeout, commandType);
        }

        /// <summary>
        ///     Return a sequence of dynamic objects with properties matching the columns.
        /// </summary>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <remarks>Note: each row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
        public IEnumerable<dynamic> Query(Query sql, bool buffered = true, int? commandTimeout = null,
            CommandType? commandType = null)
        {
            var s = compile(sql);

            return Connection.Query(s.Sql, s.NamedBindings, Transaction, buffered, commandTimeout, commandType);
        }

        /// <summary>
        ///     Return a sequence of dynamic objects with properties matching the columns.
        /// </summary>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <remarks>Note: each row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
        public IEnumerable<dynamic> Query(SqlResult sql, bool buffered = true, int? commandTimeout = null,
            CommandType? commandType = null)
        {
            log(sql);

            return Connection.Query(sql.Sql, sql.NamedBindings, Transaction, buffered, commandTimeout,
                commandType);
        }

        /// <summary>
        ///     Return a dynamic object with properties matching the columns.
        /// </summary>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <remarks>Note: the row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
        public dynamic QueryFirst(Query sql, int? commandTimeout = null, CommandType? commandType = null)
        {
            var s = compile(sql);

            return Connection.QueryFirst(s.Sql, s.NamedBindings, Transaction, commandTimeout, commandType);
        }

        /// <summary>
        ///     Return a dynamic object with properties matching the columns.
        /// </summary>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <remarks>Note: the row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
        public dynamic QueryFirst(SqlResult sql, int? commandTimeout = null, CommandType? commandType = null)
        {
            log(sql);

            return Connection.QueryFirst(sql.Sql, sql.NamedBindings, Transaction, commandTimeout, commandType);
        }

        /// <summary>
        ///     Return a dynamic object with properties matching the columns.
        /// </summary>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <remarks>Note: the row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
        public dynamic QueryFirstOrDefault(Query sql, int? commandTimeout = null,
            CommandType? commandType = null)
        {
            var s = compile(sql);

            return Connection.QueryFirstOrDefault(s.Sql, s.NamedBindings, Transaction, commandTimeout, commandType);
        }

        /// <summary>
        ///     Return a dynamic object with properties matching the columns.
        /// </summary>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <remarks>Note: the row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
        public dynamic QueryFirstOrDefault(SqlResult sql, int? commandTimeout = null,
            CommandType? commandType = null)
        {
            log(sql);

            return Connection.QueryFirstOrDefault(sql.Sql, sql.NamedBindings, Transaction, commandTimeout, commandType);
        }

        /// <summary>
        ///     Return a dynamic object with properties matching the columns.
        /// </summary>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <remarks>Note: the row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
        public dynamic QuerySingle(Query sql, int? commandTimeout = null, CommandType? commandType = null)
        {
            var s = compile(sql);

            return Connection.QuerySingle(s.Sql, s.NamedBindings, Transaction, commandTimeout, commandType);
        }

        /// <summary>
        ///     Return a dynamic object with properties matching the columns.
        /// </summary>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <remarks>Note: the row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
        public dynamic QuerySingle(SqlResult sql, int? commandTimeout = null, CommandType? commandType = null)
        {
            log(sql);

            return Connection.QuerySingle(sql.Sql, sql.NamedBindings, Transaction, commandTimeout, commandType);
        }

        /// <summary>
        ///     Return a dynamic object with properties matching the columns.
        /// </summary>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <remarks>Note: the row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
        public dynamic QuerySingleOrDefault(Query sql, int? commandTimeout = null,
            CommandType? commandType = null)
        {
            var s = compile(sql);

            return Connection.QuerySingleOrDefault(s.Sql, s.NamedBindings, Transaction, commandTimeout, commandType);
        }

        /// <summary>
        ///     Return a dynamic object with properties matching the columns.
        /// </summary>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <remarks>Note: the row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
        public dynamic QuerySingleOrDefault(SqlResult sql, int? commandTimeout = null,
            CommandType? commandType = null)
        {
            log(sql);

            return Connection.QuerySingleOrDefault(sql.Sql, sql.NamedBindings, Transaction, commandTimeout,
                commandType);
        }

        /// <summary>
        ///     Executes a query, returning the data typed as <typeparamref name="T" />.
        /// </summary>
        /// <typeparam name="T">The type of results to return.</typeparam>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="buffered">Whether to buffer results in memory.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <returns>
        ///     A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first
        ///     column in assumed, otherwise an instance is
        ///     created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public IEnumerable<T> Query<T>(Query sql, bool buffered = true, int? commandTimeout = null,
            CommandType? commandType = null)
        {
            var s = compile(sql);

            return Connection.Query<T>(s.Sql, s.NamedBindings, Transaction, buffered, commandTimeout, commandType);
        }

        /// <summary>
        ///     Executes a query, returning the data typed as <typeparamref name="T" />.
        /// </summary>
        /// <typeparam name="T">The type of results to return.</typeparam>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="buffered">Whether to buffer results in memory.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <returns>
        ///     A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first
        ///     column in assumed, otherwise an instance is
        ///     created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public IEnumerable<T> Query<T>(SqlResult sql, bool buffered = true, int? commandTimeout = null,
            CommandType? commandType = null)
        {
            log(sql);

            return Connection.Query<T>(sql.Sql, sql.NamedBindings, Transaction, buffered, commandTimeout, commandType);
        }

        /// <summary>
        ///     Executes a single-row query, returning the data typed as <typeparamref name="T" />.
        /// </summary>
        /// <typeparam name="T">The type of result to return.</typeparam>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <returns>
        ///     A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first
        ///     column in assumed, otherwise an instance is
        ///     created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public T QueryFirst<T>(Query sql, int? commandTimeout = null, CommandType? commandType = null)
        {
            var s = compile(sql);

            return Connection.QueryFirst<T>(s.Sql, s.NamedBindings, Transaction, commandTimeout, commandType);
        }

        /// <summary>
        ///     Executes a single-row query, returning the data typed as <typeparamref name="T" />.
        /// </summary>
        /// <typeparam name="T">The type of result to return.</typeparam>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <returns>
        ///     A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first
        ///     column in assumed, otherwise an instance is
        ///     created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public T QueryFirst<T>(SqlResult sql, int? commandTimeout = null, CommandType? commandType = null)
        {
            log(sql);

            return Connection.QueryFirst<T>(sql.Sql, sql.NamedBindings, Transaction, commandTimeout, commandType);
        }

        /// <summary>
        ///     Executes a single-row query, returning the data typed as <typeparamref name="T" />.
        /// </summary>
        /// <typeparam name="T">The type of result to return.</typeparam>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <returns>
        ///     A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first
        ///     column in assumed, otherwise an instance is
        ///     created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public T QueryFirstOrDefault<T>(Query sql, int? commandTimeout = null, CommandType? commandType = null)
        {
            var s = compile(sql);

            return Connection.QueryFirstOrDefault<T>(s.Sql, s.NamedBindings, Transaction, commandTimeout, commandType);
        }

        /// <summary>
        ///     Executes a single-row query, returning the data typed as <typeparamref name="T" />.
        /// </summary>
        /// <typeparam name="T">The type of result to return.</typeparam>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <returns>
        ///     A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first
        ///     column in assumed, otherwise an instance is
        ///     created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public T QueryFirstOrDefault<T>(SqlResult sql, int? commandTimeout = null, CommandType? commandType = null)
        {
            log(sql);

            return Connection.QueryFirstOrDefault<T>(sql.Sql, sql.NamedBindings, Transaction, commandTimeout,
                commandType);
        }

        /// <summary>
        ///     Executes a single-row query, returning the data typed as <typeparamref name="T" />.
        /// </summary>
        /// <typeparam name="T">The type of result to return.</typeparam>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <returns>
        ///     A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first
        ///     column in assumed, otherwise an instance is
        ///     created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public T QuerySingle<T>(Query sql, int? commandTimeout = null, CommandType? commandType = null)
        {
            var s = compile(sql);

            return Connection.QuerySingle<T>(s.Sql, s.NamedBindings, Transaction, commandTimeout, commandType);
        }

        /// <summary>
        ///     Executes a single-row query, returning the data typed as <typeparamref name="T" />.
        /// </summary>
        /// <typeparam name="T">The type of result to return.</typeparam>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <returns>
        ///     A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first
        ///     column in assumed, otherwise an instance is
        ///     created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public T QuerySingle<T>(SqlResult sql, int? commandTimeout = null, CommandType? commandType = null)
        {
            log(sql);

            return Connection.QuerySingle<T>(sql.Sql, sql.NamedBindings, Transaction, commandTimeout, commandType);
        }

        /// <summary>
        ///     Executes a single-row query, returning the data typed as <typeparamref name="T" />.
        /// </summary>
        /// <typeparam name="T">The type of result to return.</typeparam>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <returns>
        ///     A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first
        ///     column in assumed, otherwise an instance is
        ///     created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public T QuerySingleOrDefault<T>(Query sql, int? commandTimeout = null, CommandType? commandType = null)
        {
            var s = compile(sql);

            return Connection.QuerySingleOrDefault<T>(s.Sql, s.NamedBindings, Transaction, commandTimeout, commandType);
        }

        /// <summary>
        ///     Executes a single-row query, returning the data typed as <typeparamref name="T" />.
        /// </summary>
        /// <typeparam name="T">The type of result to return.</typeparam>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <returns>
        ///     A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first
        ///     column in assumed, otherwise an instance is
        ///     created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public T QuerySingleOrDefault<T>(SqlResult sql, int? commandTimeout = null, CommandType? commandType = null)
        {
            log(sql);

            return Connection.QuerySingleOrDefault<T>(sql.Sql, sql.NamedBindings, Transaction, commandTimeout,
                commandType);
        }

        /// <summary>
        ///     Executes a single-row query, returning the data typed as <paramref name="type" />.
        /// </summary>
        /// <param name="type">The type to return.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="buffered">Whether to buffer results in memory.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <exception cref="ArgumentNullException"><paramref name="type" /> is <c>null</c>.</exception>
        /// <returns>
        ///     A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first
        ///     column in assumed, otherwise an instance is
        ///     created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public IEnumerable<object> Query(Type type, Query sql, bool buffered = true, int? commandTimeout = null,
            CommandType? commandType = null)
        {
            var s = compile(sql);

            return Connection.Query(type, s.Sql, s.NamedBindings, Transaction, buffered, commandTimeout,
                commandType);
        }

        /// <summary>
        ///     Executes a single-row query, returning the data typed as <paramref name="type" />.
        /// </summary>
        /// <param name="type">The type to return.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="buffered">Whether to buffer results in memory.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <exception cref="ArgumentNullException"><paramref name="type" /> is <c>null</c>.</exception>
        /// <returns>
        ///     A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first
        ///     column in assumed, otherwise an instance is
        ///     created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public IEnumerable<object> Query(Type type, SqlResult sql, bool buffered = true, int? commandTimeout = null,
            CommandType? commandType = null)
        {
            log(sql);

            return Connection.Query(type, sql.Sql, sql.NamedBindings, Transaction, buffered, commandTimeout,
                commandType);
        }

        /// <summary>
        ///     Executes a single-row query, returning the data typed as <paramref name="type" />.
        /// </summary>
        /// <param name="type">The type to return.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <exception cref="ArgumentNullException"><paramref name="type" /> is <c>null</c>.</exception>
        /// <returns>
        ///     A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first
        ///     column in assumed, otherwise an instance is
        ///     created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public object QueryFirst(Type type, Query sql, int? commandTimeout = null, CommandType? commandType = null)
        {
            var s = compile(sql);

            return Connection.QueryFirst(type, s.Sql, s.NamedBindings, Transaction, commandTimeout, commandType);
        }

        /// <summary>
        ///     Executes a single-row query, returning the data typed as <paramref name="type" />.
        /// </summary>
        /// <param name="type">The type to return.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <exception cref="ArgumentNullException"><paramref name="type" /> is <c>null</c>.</exception>
        /// <returns>
        ///     A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first
        ///     column in assumed, otherwise an instance is
        ///     created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public object QueryFirst(Type type, SqlResult sql, int? commandTimeout = null,
            CommandType? commandType = null)
        {
            log(sql);

            return Connection.QueryFirst(type, sql.Sql, sql.NamedBindings, Transaction, commandTimeout, commandType);
        }

        /// <summary>
        ///     Executes a single-row query, returning the data typed as <paramref name="type" />.
        /// </summary>
        /// <param name="type">The type to return.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <exception cref="ArgumentNullException"><paramref name="type" /> is <c>null</c>.</exception>
        /// <returns>
        ///     A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first
        ///     column in assumed, otherwise an instance is
        ///     created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public object QueryFirstOrDefault(Type type, Query sql, int? commandTimeout = null,
            CommandType? commandType = null)
        {
            var s = compile(sql);

            return Connection.QueryFirstOrDefault(type, s.Sql, s.NamedBindings, Transaction, commandTimeout,
                commandType);
        }

        /// <summary>
        ///     Executes a single-row query, returning the data typed as <paramref name="type" />.
        /// </summary>
        /// <param name="type">The type to return.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <exception cref="ArgumentNullException"><paramref name="type" /> is <c>null</c>.</exception>
        /// <returns>
        ///     A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first
        ///     column in assumed, otherwise an instance is
        ///     created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public object QueryFirstOrDefault(Type type, SqlResult sql, int? commandTimeout = null,
            CommandType? commandType = null)
        {
            log(sql);

            return Connection.QueryFirstOrDefault(type, sql.Sql, sql.NamedBindings, Transaction, commandTimeout,
                commandType);
        }

        /// <summary>
        ///     Executes a single-row query, returning the data typed as <paramref name="type" />.
        /// </summary>
        /// <param name="type">The type to return.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <exception cref="ArgumentNullException"><paramref name="type" /> is <c>null</c>.</exception>
        /// <returns>
        ///     A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first
        ///     column in assumed, otherwise an instance is
        ///     created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public object QuerySingle(Type type, Query sql, int? commandTimeout = null, CommandType? commandType = null)
        {
            var s = compile(sql);

            return Connection.QuerySingle(type, s.Sql, s.NamedBindings, Transaction, commandTimeout, commandType);
        }

        /// <summary>
        ///     Executes a single-row query, returning the data typed as <paramref name="type" />.
        /// </summary>
        /// <param name="type">The type to return.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <exception cref="ArgumentNullException"><paramref name="type" /> is <c>null</c>.</exception>
        /// <returns>
        ///     A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first
        ///     column in assumed, otherwise an instance is
        ///     created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public object QuerySingle(Type type, SqlResult sql, int? commandTimeout = null,
            CommandType? commandType = null)
        {
            log(sql);

            return Connection.QuerySingle(type, sql.Sql, sql.NamedBindings, Transaction, commandTimeout, commandType);
        }

        /// <summary>
        ///     Executes a single-row query, returning the data typed as <paramref name="type" />.
        /// </summary>
        /// <param name="type">The type to return.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <exception cref="ArgumentNullException"><paramref name="type" /> is <c>null</c>.</exception>
        /// <returns>
        ///     A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first
        ///     column in assumed, otherwise an instance is
        ///     created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public object QuerySingleOrDefault(Type type, Query sql, int? commandTimeout = null,
            CommandType? commandType = null)
        {
            var s = compile(sql);

            return Connection.QuerySingleOrDefault(type, s.Sql, s.NamedBindings, Transaction, commandTimeout,
                commandType);
        }

        /// <summary>
        ///     Executes a single-row query, returning the data typed as <paramref name="type" />.
        /// </summary>
        /// <param name="type">The type to return.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <exception cref="ArgumentNullException"><paramref name="type" /> is <c>null</c>.</exception>
        /// <returns>
        ///     A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first
        ///     column in assumed, otherwise an instance is
        ///     created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public object QuerySingleOrDefault(Type type, SqlResult sql, int? commandTimeout = null,
            CommandType? commandType = null)
        {
            log(sql);

            return Connection.QuerySingleOrDefault(type, sql.Sql, sql.NamedBindings, Transaction, commandTimeout,
                commandType);
        }

        /// <summary>
        ///     Execute a command that returns multiple result sets, and access each in turn.
        /// </summary>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        public SqlMapper.GridReader QueryMultiple(Query sql, int? commandTimeout = null,
            CommandType? commandType = null)
        {
            var s = compile(sql);

            return Connection.QueryMultiple(s.Sql, s.NamedBindings, Transaction, commandTimeout, commandType);
        }

        /// <summary>
        ///     Execute a command that returns multiple result sets, and access each in turn.
        /// </summary>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        public SqlMapper.GridReader QueryMultiple(SqlResult sql, int? commandTimeout = null,
            CommandType? commandType = null)
        {
            log(sql);

            return Connection.QueryMultiple(sql.Sql, sql.NamedBindings, Transaction, commandTimeout, commandType);
        }

        /// <summary>
        ///     Perform a multi-mapping query with 2 input types.
        ///     This returns a single type, combined
        /// </summary>
        /// <typeparam name="TFirst">The first type in the recordset.</typeparam>
        /// <typeparam name="TSecond">The second type in the recordset.</typeparam>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        public IEnumerable<(TFirst, TSecond)> Query<TFirst, TSecond>(Query sql, bool buffered = true,
            string splitOn = "Id",
            int? commandTimeout = null, CommandType? commandType = null)
        {
            var s = compile(sql);

            return Connection.Query<TFirst, TSecond, (TFirst, TSecond)>(s.Sql, (first, second) => (first, second),
                s.NamedBindings, Transaction, buffered, splitOn, commandTimeout, commandType);
        }

        /// <summary>
        ///     Perform a multi-mapping query with 2 input types.
        ///     This returns a single type, combined
        /// </summary>
        /// <typeparam name="TFirst">The first type in the recordset.</typeparam>
        /// <typeparam name="TSecond">The second type in the recordset.</typeparam>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        public IEnumerable<(TFirst, TSecond)> Query<TFirst, TSecond>(SqlResult sql, bool buffered = true,
            string splitOn = "Id",
            int? commandTimeout = null, CommandType? commandType = null)
        {
            log(sql);

            return Connection.Query<TFirst, TSecond, (TFirst, TSecond)>(sql.Sql, (first, second) => (first, second),
                sql.NamedBindings, Transaction, buffered, splitOn, commandTimeout, commandType);
        }

        /// <summary>
        ///     Perform a multi-mapping query with 3 input types.
        ///     This returns a single type, combined
        /// </summary>
        /// <typeparam name="TFirst">The first type in the recordset.</typeparam>
        /// <typeparam name="TSecond">The second type in the recordset.</typeparam>
        /// <typeparam name="TThird">The third type in the recordset.</typeparam>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        public IEnumerable<(TFirst, TSecond, TThird)> Query<TFirst, TSecond, TThird>(Query sql,
            bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null)
        {
            var s = compile(sql);

            return Connection.Query<TFirst, TSecond, TThird, (TFirst, TSecond, TThird)>(s.Sql,
                (first, second, third) => (first, second, third), s.NamedBindings, Transaction, buffered, splitOn,
                commandTimeout, commandType);
        }

        /// <summary>
        ///     Perform a multi-mapping query with 3 input types.
        ///     This returns a single type, combined
        /// </summary>
        /// <typeparam name="TFirst">The first type in the recordset.</typeparam>
        /// <typeparam name="TSecond">The second type in the recordset.</typeparam>
        /// <typeparam name="TThird">The third type in the recordset.</typeparam>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        public IEnumerable<(TFirst, TSecond, TThird)> Query<TFirst, TSecond, TThird>(SqlResult sql,
            bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null)
        {
            log(sql);

            return Connection.Query<TFirst, TSecond, TThird, (TFirst, TSecond, TThird)>(sql.Sql,
                (first, second, third) => (first, second, third), sql.NamedBindings, Transaction, buffered, splitOn,
                commandTimeout, commandType);
        }

        /// <summary>
        ///     Perform a multi-mapping query with 4 input types.
        ///     This returns a single type, combined
        /// </summary>
        /// <typeparam name="TFirst">The first type in the recordset.</typeparam>
        /// <typeparam name="TSecond">The second type in the recordset.</typeparam>
        /// <typeparam name="TThird">The third type in the recordset.</typeparam>
        /// <typeparam name="TFourth">The fourth type in the recordset.</typeparam>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        public IEnumerable<(TFirst, TSecond, TThird, TFourth)> Query<TFirst, TSecond, TThird, TFourth>(Query sql,
            bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null)
        {
            var s = compile(sql);

            return Connection.Query<TFirst, TSecond, TThird, TFourth, (TFirst, TSecond, TThird, TFourth)>(s.Sql,
                (first, second, third, fourth) => (first, second, third, fourth), s.NamedBindings, Transaction,
                buffered, splitOn, commandTimeout, commandType);
        }

        /// <summary>
        ///     Perform a multi-mapping query with 4 input types.
        ///     This returns a single type, combined
        /// </summary>
        /// <typeparam name="TFirst">The first type in the recordset.</typeparam>
        /// <typeparam name="TSecond">The second type in the recordset.</typeparam>
        /// <typeparam name="TThird">The third type in the recordset.</typeparam>
        /// <typeparam name="TFourth">The fourth type in the recordset.</typeparam>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        public IEnumerable<(TFirst, TSecond, TThird, TFourth)> Query<TFirst, TSecond, TThird, TFourth>(SqlResult sql,
            bool buffered = true, string splitOn = "Id", int? commandTimeout = null,
            CommandType? commandType = null)
        {
            log(sql);

            return Connection.Query<TFirst, TSecond, TThird, TFourth, (TFirst, TSecond, TThird, TFourth)>(sql.Sql,
                (first, second, third, fourth) => (first, second, third, fourth), sql.NamedBindings, Transaction,
                buffered, splitOn, commandTimeout, commandType);
        }

        /// <summary>
        ///     Perform a multi-mapping query with 5 input types.
        ///     This returns a single type, combined
        /// </summary>
        /// <typeparam name="TFirst">The first type in the recordset.</typeparam>
        /// <typeparam name="TSecond">The second type in the recordset.</typeparam>
        /// <typeparam name="TThird">The third type in the recordset.</typeparam>
        /// <typeparam name="TFourth">The fourth type in the recordset.</typeparam>
        /// <typeparam name="TFifth">The fifth type in the recordset.</typeparam>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        public IEnumerable<(TFirst, TSecond, TThird, TFourth, TFifth)> Query<TFirst, TSecond, TThird, TFourth,
            TFifth>(Query sql, bool buffered = true, string splitOn = "Id", int? commandTimeout = null,
            CommandType? commandType = null)
        {
            var s = compile(sql);

            return Connection
                .Query<TFirst, TSecond, TThird, TFourth, TFifth, (TFirst, TSecond, TThird, TFourth, TFifth)>(
                    s.Sql, (first, second, third, fourth, fifth) => (first, second, third, fourth, fifth),
                    s.NamedBindings, Transaction, buffered, splitOn, commandTimeout, commandType);
        }

        /// <summary>
        ///     Perform a multi-mapping query with 5 input types.
        ///     This returns a single type, combined
        /// </summary>
        /// <typeparam name="TFirst">The first type in the recordset.</typeparam>
        /// <typeparam name="TSecond">The second type in the recordset.</typeparam>
        /// <typeparam name="TThird">The third type in the recordset.</typeparam>
        /// <typeparam name="TFourth">The fourth type in the recordset.</typeparam>
        /// <typeparam name="TFifth">The fifth type in the recordset.</typeparam>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        public IEnumerable<(TFirst, TSecond, TThird, TFourth, TFifth)> Query<TFirst, TSecond, TThird, TFourth,
            TFifth>(SqlResult sql, bool buffered = true, string splitOn = "Id", int? commandTimeout = null,
            CommandType? commandType = null)
        {
            log(sql);

            return Connection
                .Query<TFirst, TSecond, TThird, TFourth, TFifth, (TFirst, TSecond, TThird, TFourth, TFifth)>(
                    sql.Sql, (first, second, third, fourth, fifth) => (first, second, third, fourth, fifth),
                    sql.NamedBindings, Transaction, buffered, splitOn, commandTimeout, commandType);
        }

        /// <summary>
        ///     Perform a multi-mapping query with 6 input types.
        ///     This returns a single type, combined
        /// </summary>
        /// <typeparam name="TFirst">The first type in the recordset.</typeparam>
        /// <typeparam name="TSecond">The second type in the recordset.</typeparam>
        /// <typeparam name="TThird">The third type in the recordset.</typeparam>
        /// <typeparam name="TFourth">The fourth type in the recordset.</typeparam>
        /// <typeparam name="TFifth">The fifth type in the recordset.</typeparam>
        /// <typeparam name="TSixth">The sixth type in the recordset.</typeparam>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        public IEnumerable<(TFirst, TSecond, TThird, TFourth, TFifth, TSixth)> Query<TFirst, TSecond, TThird,
            TFourth, TFifth, TSixth>(Query sql, bool buffered = true, string splitOn = "Id", int? commandTimeout = null,
            CommandType? commandType = null)
        {
            var s = compile(sql);

            return Connection
                .Query<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, (TFirst, TSecond, TThird, TFourth, TFifth,
                    TSixth)>(s.Sql,
                    (first, second, third, fourth, fifth, sixth) => (first, second, third, fourth, fifth, sixth),
                    s.NamedBindings, Transaction, buffered, splitOn, commandTimeout, commandType);
        }

        /// <summary>
        ///     Perform a multi-mapping query with 6 input types.
        ///     This returns a single type, combined
        /// </summary>
        /// <typeparam name="TFirst">The first type in the recordset.</typeparam>
        /// <typeparam name="TSecond">The second type in the recordset.</typeparam>
        /// <typeparam name="TThird">The third type in the recordset.</typeparam>
        /// <typeparam name="TFourth">The fourth type in the recordset.</typeparam>
        /// <typeparam name="TFifth">The fifth type in the recordset.</typeparam>
        /// <typeparam name="TSixth">The sixth type in the recordset.</typeparam>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        public IEnumerable<(TFirst, TSecond, TThird, TFourth, TFifth, TSixth)> Query<TFirst, TSecond, TThird,
            TFourth, TFifth, TSixth>(SqlResult sql, bool buffered = true, string splitOn = "Id",
            int? commandTimeout = null,
            CommandType? commandType = null)
        {
            log(sql);

            return Connection
                .Query<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, (TFirst, TSecond, TThird, TFourth, TFifth,
                    TSixth)>(sql.Sql,
                    (first, second, third, fourth, fifth, sixth) => (first, second, third, fourth, fifth, sixth),
                    sql.NamedBindings, Transaction, buffered, splitOn, commandTimeout, commandType);
        }

        /// <summary>
        ///     Perform a multi-mapping query with 7 input types.
        ///     This returns a single type, combined
        /// </summary>
        /// <typeparam name="TFirst">The first type in the recordset.</typeparam>
        /// <typeparam name="TSecond">The second type in the recordset.</typeparam>
        /// <typeparam name="TThird">The third type in the recordset.</typeparam>
        /// <typeparam name="TFourth">The fourth type in the recordset.</typeparam>
        /// <typeparam name="TFifth">The fifth type in the recordset.</typeparam>
        /// <typeparam name="TSixth">The sixth type in the recordset.</typeparam>
        /// <typeparam name="TSeventh">The seventh type in the recordset.</typeparam>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        public IEnumerable<(TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh)> Query<TFirst, TSecond,
            TThird, TFourth, TFifth, TSixth, TSeventh>(Query sql, bool buffered = true, string splitOn = "Id",
            int? commandTimeout = null, CommandType? commandType = null)
        {
            var s = compile(sql);

            return Connection
                .Query<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, (TFirst, TSecond, TThird, TFourth
                    , TFifth, TSixth, TSeventh)>(s.Sql, (first, second, third, fourth, fifth, sixth, seventh) =>
                        (first, second, third, fourth, fifth, sixth, seventh), s.NamedBindings, Transaction,
                    buffered, splitOn, commandTimeout, commandType);
        }

        /// <summary>
        ///     Perform a multi-mapping query with 7 input types.
        ///     This returns a single type, combined
        /// </summary>
        /// <typeparam name="TFirst">The first type in the recordset.</typeparam>
        /// <typeparam name="TSecond">The second type in the recordset.</typeparam>
        /// <typeparam name="TThird">The third type in the recordset.</typeparam>
        /// <typeparam name="TFourth">The fourth type in the recordset.</typeparam>
        /// <typeparam name="TFifth">The fifth type in the recordset.</typeparam>
        /// <typeparam name="TSixth">The sixth type in the recordset.</typeparam>
        /// <typeparam name="TSeventh">The seventh type in the recordset.</typeparam>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        public IEnumerable<(TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh)> Query<TFirst, TSecond,
            TThird, TFourth, TFifth, TSixth, TSeventh>(SqlResult sql, bool buffered = true, string splitOn = "Id",
            int? commandTimeout = null,
            CommandType? commandType = null)
        {
            log(sql);

            return Connection
                .Query<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, (TFirst, TSecond, TThird, TFourth
                    , TFifth, TSixth, TSeventh)>(sql.Sql,
                    (first, second, third, fourth, fifth, sixth, seventh) =>
                        (first, second, third, fourth, fifth, sixth, seventh), sql.NamedBindings, Transaction,
                    buffered, splitOn, commandTimeout, commandType);
        }

        /// <summary>
        ///     Perform a multi-mapping query with an arbitrary number of input types.
        ///     This returns a single type, combined
        /// </summary>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="types">Array of types in the recordset.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn" />.</returns>
        public IEnumerable<TReturn> Query<TReturn>(Query sql, Type[] types, Func<object[], TReturn> map,
            bool buffered = true, string splitOn = "Id", int? commandTimeout = null,
            CommandType? commandType = null)
        {
            var s = compile(sql);

            return Connection.Query(s.Sql, types, map, s.NamedBindings, Transaction, buffered, splitOn,
                commandTimeout, commandType);
        }

        /// <summary>
        ///     Perform a multi-mapping query with an arbitrary number of input types.
        ///     This returns a single type, combined
        /// </summary>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="types">Array of types in the recordset.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn" />.</returns>
        public IEnumerable<TReturn> Query<TReturn>(SqlResult sql, Type[] types, Func<object[], TReturn> map,
            bool buffered = true, string splitOn = "Id", int? commandTimeout = null,
            CommandType? commandType = null)
        {
            log(sql);

            return Connection.Query(sql.Sql, types, map, sql.NamedBindings, Transaction, buffered, splitOn,
                commandTimeout, commandType);
        }

        #region Regular Dapper methods

        /// <summary>
        ///     Execute parameterized SQL.
        /// </summary>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>The number of rows affected.</returns>
        public int Execute(string sql, object param = null, int? commandTimeout = null,
            CommandType? commandType = null)
        {
            log(sql, param);

            return Connection.Execute(sql, param, Transaction, commandTimeout, commandType);
        }

        /// <summary>
        ///     Execute parameterized SQL that selects a single value.
        /// </summary>
        /// <param name="sql">The SQL to execute.</param>
        /// <param name="param">The parameters to use for this command.</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>The first cell selected as <see cref="object" />.</returns>
        public object ExecuteScalar(string sql, object param = null, int? commandTimeout = null,
            CommandType? commandType = null)
        {
            log(sql, param);

            return Connection.ExecuteScalar(sql, param, Transaction, commandTimeout, commandType);
        }

        /// <summary>
        ///     Execute parameterized SQL that selects a single value.
        /// </summary>
        /// <typeparam name="T">The type to return.</typeparam>
        /// <param name="sql">The SQL to execute.</param>
        /// <param name="param">The parameters to use for this command.</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>The first cell returned, as <typeparamref name="T" />.</returns>
        public T ExecuteScalar<T>(string sql, object param = null, int? commandTimeout = null,
            CommandType? commandType = null)
        {
            log(sql, param);

            return Connection.ExecuteScalar<T>(sql, param, Transaction, commandTimeout, commandType);
        }

        /// <summary>
        ///     Execute parameterized SQL and return an <see cref="IDataReader" />.
        /// </summary>
        /// <param name="sql">The SQL to execute.</param>
        /// <param name="param">The parameters to use for this command.</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An <see cref="IDataReader" /> that can be used to iterate over the results of the SQL query.</returns>
        /// <remarks>
        ///     This is typically used when the results of a query are not processed by Dapper, for example, used to fill a
        ///     <see cref="DataTable" />
        ///     or <see cref="T:DataSet" />.
        /// </remarks>
        /// <example>
        ///     <code>
        /// <![CDATA[
        /// DataTable table = new DataTable("MyTable");
        /// using (var reader = ExecuteReader(cnn, sql, param))
        /// {
        ///     table.Load(reader);
        /// }
        /// ]]>
        /// </code>
        /// </example>
        public IDataReader ExecuteReader(string sql, object param = null, int? commandTimeout = null,
            CommandType? commandType = null)
        {
            log(sql, param);

            return Connection.ExecuteReader(sql, param, Transaction, commandTimeout, commandType);
        }

        /// <summary>
        ///     Return a sequence of dynamic objects with properties matching the columns.
        /// </summary>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <remarks>Note: each row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
        public IEnumerable<dynamic> Query(string sql, object param = null, bool buffered = true,
            int? commandTimeout = null,
            CommandType? commandType = null)
        {
            log(sql, param);

            return Connection.Query(sql, param, Transaction, buffered, commandTimeout, commandType);
        }

        /// <summary>
        ///     Return a dynamic object with properties matching the columns.
        /// </summary>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <remarks>Note: the row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
        public dynamic QueryFirst(string sql, object param = null, int? commandTimeout = null,
            CommandType? commandType = null)
        {
            log(sql, param);

            return Connection.QueryFirst(sql, param, Transaction, commandTimeout, commandType);
        }

        /// <summary>
        ///     Return a dynamic object with properties matching the columns.
        /// </summary>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <remarks>Note: the row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
        public dynamic QueryFirstOrDefault(string sql, object param = null, int? commandTimeout = null,
            CommandType? commandType = null)
        {
            log(sql, param);

            return Connection.QueryFirstOrDefault(sql, param, Transaction, commandTimeout, commandType);
        }

        /// <summary>
        ///     Return a dynamic object with properties matching the columns.
        /// </summary>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <remarks>Note: the row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
        public dynamic QuerySingle(string sql, object param = null, int? commandTimeout = null,
            CommandType? commandType = null)
        {
            log(sql, param);

            return Connection.QuerySingle(sql, param, Transaction, commandTimeout, commandType);
        }

        /// <summary>
        ///     Return a dynamic object with properties matching the columns.
        /// </summary>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <remarks>Note: the row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
        public dynamic QuerySingleOrDefault(string sql, object param = null, int? commandTimeout = null,
            CommandType? commandType = null)
        {
            log(sql, param);

            return Connection.QuerySingleOrDefault(sql, param, Transaction, commandTimeout, commandType);
        }

        /// <summary>
        ///     Executes a query, returning the data typed as <typeparamref name="T" />.
        /// </summary>
        /// <typeparam name="T">The type of results to return.</typeparam>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="buffered">Whether to buffer results in memory.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <returns>
        ///     A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first
        ///     column in assumed, otherwise an instance is
        ///     created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public IEnumerable<T> Query<T>(string sql, object param = null, bool buffered = true,
            int? commandTimeout = null,
            CommandType? commandType = null)
        {
            log(sql, param);

            return Connection.Query<T>(sql, param, Transaction, buffered, commandTimeout, commandType);
        }

        /// <summary>
        ///     Executes a single-row query, returning the data typed as <typeparamref name="T" />.
        /// </summary>
        /// <typeparam name="T">The type of result to return.</typeparam>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <returns>
        ///     A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first
        ///     column in assumed, otherwise an instance is
        ///     created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public T QueryFirst<T>(string sql, object param = null, int? commandTimeout = null,
            CommandType? commandType = null)
        {
            log(sql, param);

            return Connection.QueryFirst<T>(sql, param, Transaction, commandTimeout, commandType);
        }

        /// <summary>
        ///     Executes a single-row query, returning the data typed as <typeparamref name="T" />.
        /// </summary>
        /// <typeparam name="T">The type of result to return.</typeparam>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <returns>
        ///     A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first
        ///     column in assumed, otherwise an instance is
        ///     created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public T QueryFirstOrDefault<T>(string sql, object param = null, int? commandTimeout = null,
            CommandType? commandType = null)
        {
            log(sql, param);

            return Connection.QueryFirstOrDefault<T>(sql, param, Transaction, commandTimeout, commandType);
        }

        /// <summary>
        ///     Executes a single-row query, returning the data typed as <typeparamref name="T" />.
        /// </summary>
        /// <typeparam name="T">The type of result to return.</typeparam>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <returns>
        ///     A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first
        ///     column in assumed, otherwise an instance is
        ///     created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public T QuerySingle<T>(string sql, object param = null, int? commandTimeout = null,
            CommandType? commandType = null)
        {
            log(sql, param);

            return Connection.QuerySingle<T>(sql, param, Transaction, commandTimeout, commandType);
        }

        /// <summary>
        ///     Executes a single-row query, returning the data typed as <typeparamref name="T" />.
        /// </summary>
        /// <typeparam name="T">The type of result to return.</typeparam>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <returns>
        ///     A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first
        ///     column in assumed, otherwise an instance is
        ///     created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public T QuerySingleOrDefault<T>(string sql, object param = null, int? commandTimeout = null,
            CommandType? commandType = null)
        {
            log(sql, param);

            return Connection.QuerySingleOrDefault<T>(sql, param, Transaction, commandTimeout, commandType);
        }

        /// <summary>
        ///     Executes a single-row query, returning the data typed as <paramref name="type" />.
        /// </summary>
        /// <param name="type">The type to return.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="buffered">Whether to buffer results in memory.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <exception cref="ArgumentNullException"><paramref name="type" /> is <c>null</c>.</exception>
        /// <returns>
        ///     A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first
        ///     column in assumed, otherwise an instance is
        ///     created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public IEnumerable<object> Query(Type type, string sql, object param = null, bool buffered = true,
            int? commandTimeout = null, CommandType? commandType = null)
        {
            log(sql, param);

            return Connection.Query(type, sql, param, Transaction, buffered, commandTimeout, commandType);
        }

        /// <summary>
        ///     Executes a single-row query, returning the data typed as <paramref name="type" />.
        /// </summary>
        /// <param name="type">The type to return.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <exception cref="ArgumentNullException"><paramref name="type" /> is <c>null</c>.</exception>
        /// <returns>
        ///     A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first
        ///     column in assumed, otherwise an instance is
        ///     created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public object QueryFirst(Type type, string sql, object param = null, int? commandTimeout = null,
            CommandType? commandType = null)
        {
            log(sql, param);

            return Connection.QueryFirst(type, sql, param, Transaction, commandTimeout, commandType);
        }

        /// <summary>
        ///     Executes a single-row query, returning the data typed as <paramref name="type" />.
        /// </summary>
        /// <param name="type">The type to return.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <exception cref="ArgumentNullException"><paramref name="type" /> is <c>null</c>.</exception>
        /// <returns>
        ///     A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first
        ///     column in assumed, otherwise an instance is
        ///     created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public object QueryFirstOrDefault(Type type, string sql, object param = null, int? commandTimeout = null,
            CommandType? commandType = null)
        {
            log(sql, param);

            return Connection.QueryFirstOrDefault(type, sql, param, Transaction, commandTimeout, commandType);
        }

        /// <summary>
        ///     Executes a single-row query, returning the data typed as <paramref name="type" />.
        /// </summary>
        /// <param name="type">The type to return.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <exception cref="ArgumentNullException"><paramref name="type" /> is <c>null</c>.</exception>
        /// <returns>
        ///     A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first
        ///     column in assumed, otherwise an instance is
        ///     created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public object QuerySingle(Type type, string sql, object param = null, int? commandTimeout = null,
            CommandType? commandType = null)
        {
            log(sql, param);

            return Connection.QuerySingle(type, sql, param, Transaction, commandTimeout, commandType);
        }

        /// <summary>
        ///     Executes a single-row query, returning the data typed as <paramref name="type" />.
        /// </summary>
        /// <param name="type">The type to return.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <exception cref="ArgumentNullException"><paramref name="type" /> is <c>null</c>.</exception>
        /// <returns>
        ///     A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first
        ///     column in assumed, otherwise an instance is
        ///     created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public object QuerySingleOrDefault(Type type, string sql, object param = null, int? commandTimeout = null,
            CommandType? commandType = null)
        {
            log(sql, param);

            return Connection.QuerySingleOrDefault(type, sql, param, Transaction, commandTimeout, commandType);
        }

        /// <summary>
        ///     Execute a command that returns multiple result sets, and access each in turn.
        /// </summary>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        public SqlMapper.GridReader QueryMultiple(string sql, object param = null, int? commandTimeout = null,
            CommandType? commandType = null)
        {
            log(sql, param);

            return Connection.QueryMultiple(sql, param, Transaction, commandTimeout, commandType);
        }

        /// <summary>
        ///     Perform a multi-mapping query with 2 input types.
        ///     This returns a single type, combined from the raw types via <paramref name="map" />.
        /// </summary>
        /// <typeparam name="TFirst">The first type in the recordset.</typeparam>
        /// <typeparam name="TSecond">The second type in the recordset.</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn" />.</returns>
        public IEnumerable<TReturn> Query<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map,
            object param = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null,
            CommandType? commandType = null)
        {
            log(sql, param);

            return Connection.Query(sql, map, param, Transaction, buffered, splitOn, commandTimeout, commandType);
        }

        /// <summary>
        ///     Perform a multi-mapping query with 3 input types.
        ///     This returns a single type, combined from the raw types via <paramref name="map" />.
        /// </summary>
        /// <typeparam name="TFirst">The first type in the recordset.</typeparam>
        /// <typeparam name="TSecond">The second type in the recordset.</typeparam>
        /// <typeparam name="TThird">The third type in the recordset.</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn" />.</returns>
        public IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TReturn>(string sql,
            Func<TFirst, TSecond, TThird, TReturn> map, object param = null, bool buffered = true,
            string splitOn = "Id",
            int? commandTimeout = null, CommandType? commandType = null)
        {
            log(sql, param);

            return Connection.Query(sql, map, param, Transaction, buffered, splitOn, commandTimeout, commandType);
        }

        /// <summary>
        ///     Perform a multi-mapping query with 4 input types.
        ///     This returns a single type, combined from the raw types via <paramref name="map" />.
        /// </summary>
        /// <typeparam name="TFirst">The first type in the recordset.</typeparam>
        /// <typeparam name="TSecond">The second type in the recordset.</typeparam>
        /// <typeparam name="TThird">The third type in the recordset.</typeparam>
        /// <typeparam name="TFourth">The fourth type in the recordset.</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn" />.</returns>
        public IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TReturn>(string sql,
            Func<TFirst, TSecond, TThird, TFourth, TReturn> map, object param = null, bool buffered = true,
            string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null)
        {
            log(sql, param);

            return Connection.Query(sql, map, param, Transaction, buffered, splitOn, commandTimeout, commandType);
        }

        /// <summary>
        ///     Perform a multi-mapping query with 5 input types.
        ///     This returns a single type, combined from the raw types via <paramref name="map" />.
        /// </summary>
        /// <typeparam name="TFirst">The first type in the recordset.</typeparam>
        /// <typeparam name="TSecond">The second type in the recordset.</typeparam>
        /// <typeparam name="TThird">The third type in the recordset.</typeparam>
        /// <typeparam name="TFourth">The fourth type in the recordset.</typeparam>
        /// <typeparam name="TFifth">The fifth type in the recordset.</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn" />.</returns>
        public IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(string sql,
            Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, object param = null,
            bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null)
        {
            log(sql, param);

            return Connection.Query(sql, map, param, Transaction, buffered, splitOn, commandTimeout, commandType);
        }

        /// <summary>
        ///     Perform a multi-mapping query with 6 input types.
        ///     This returns a single type, combined from the raw types via <paramref name="map" />.
        /// </summary>
        /// <typeparam name="TFirst">The first type in the recordset.</typeparam>
        /// <typeparam name="TSecond">The second type in the recordset.</typeparam>
        /// <typeparam name="TThird">The third type in the recordset.</typeparam>
        /// <typeparam name="TFourth">The fourth type in the recordset.</typeparam>
        /// <typeparam name="TFifth">The fifth type in the recordset.</typeparam>
        /// <typeparam name="TSixth">The sixth type in the recordset.</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn" />.</returns>
        public IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>(string sql,
            Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn> map, object param = null,
            bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null)
        {
            log(sql, param);

            return Connection.Query(sql, map, param, Transaction, buffered, splitOn, commandTimeout, commandType);
        }

        /// <summary>
        ///     Perform a multi-mapping query with 7 input types.
        ///     This returns a single type, combined from the raw types via <paramref name="map" />.
        /// </summary>
        /// <typeparam name="TFirst">The first type in the recordset.</typeparam>
        /// <typeparam name="TSecond">The second type in the recordset.</typeparam>
        /// <typeparam name="TThird">The third type in the recordset.</typeparam>
        /// <typeparam name="TFourth">The fourth type in the recordset.</typeparam>
        /// <typeparam name="TFifth">The fifth type in the recordset.</typeparam>
        /// <typeparam name="TSixth">The sixth type in the recordset.</typeparam>
        /// <typeparam name="TSeventh">The seventh type in the recordset.</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn" />.</returns>
        public IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh,
            TReturn>(string sql, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn> map,
            object param = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null,
            CommandType? commandType = null)
        {
            log(sql, param);

            return Connection.Query(sql, map, param, Transaction, buffered, splitOn, commandTimeout, commandType);
        }

        /// <summary>
        ///     Perform a multi-mapping query with an arbitrary number of input types.
        ///     This returns a single type, combined from the raw types via <paramref name="map" />.
        /// </summary>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="types">Array of types in the recordset.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        /// <returns>An enumerable of <typeparamref name="TReturn" />.</returns>
        public IEnumerable<TReturn> Query<TReturn>(string sql, Type[] types, Func<object[], TReturn> map,
            object param = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null,
            CommandType? commandType = null)
        {
            log(sql, param);

            return Connection.Query(sql, types, map, param, Transaction, buffered, splitOn, commandTimeout,
                commandType);
        }

        #endregion
    }
}