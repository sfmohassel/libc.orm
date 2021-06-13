#region License

//
// Copyright (c) 2007-2018, Sean Chambers <schambers80@gmail.com>
// Copyright (c) 2010, Nathan Brown
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

#endregion

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using libc.orm.DatabaseMigration.Abstractions.Expressions;
using libc.orm.DatabaseMigration.DdlProcessing;
using libc.orm.DatabaseMigration.DdlProcessing.BatchParser.Sources;
using libc.orm.DatabaseMigration.DdlProcessing.BatchParser.SpecialTokenSearchers;
using libc.orm.sqlite.DdlGeneration;
using libc.orm.sqlite.DdlProcessing.BatchParser;
using Microsoft.Extensions.Logging;

namespace libc.orm.sqlite.DdlProcessing
{
    // ReSharper disable once InconsistentNaming
    public class SQLiteProcessor : GenericProcessorBase
    {
        private readonly SQLiteBatchParser batchParser;

        public SQLiteProcessor(
            // ReSharper disable once SuggestBaseTypeForParameter
            SQLiteGenerator generator,
            ILogger logger,
            ProcessorOptions options,
            SQLiteBatchParser batchParser)
            : base(() => new SQLiteFactory(), generator, logger, options)
        {
            this.batchParser = batchParser;
        }

        public override string DatabaseType => "SQLite";
        public override IList<string> DatabaseTypeAliases { get; } = new List<string>();

        public override bool SchemaExists(string schemaName)
        {
            return true;
        }

        public override bool TableExists(string schemaName, string tableName)
        {
            return Exists("select count(*) from sqlite_master where name=\"{0}\" and type='table'", tableName);
        }

        public override bool ColumnExists(string schemaName, string tableName, string columnName)
        {
            var dataSet = Read("PRAGMA table_info([{0}])", tableName);

            if (dataSet.Tables.Count == 0)
                return false;

            var table = dataSet.Tables[0];

            if (!table.Columns.Contains("Name"))
                return false;

            return table.Select(string.Format("Name='{0}'", columnName.Replace("'", "''"))).Length > 0;
        }

        public override bool ConstraintExists(string schemaName, string tableName, string constraintName)
        {
            return false;
        }

        public override bool IndexExists(string schemaName, string tableName, string indexName)
        {
            return Exists("select count(*) from sqlite_master where name='{0}' and tbl_name='{1}' and type='index'",
                indexName, tableName);
        }

        public override bool SequenceExists(string schemaName, string sequenceName)
        {
            return false;
        }

        public override void Execute(string template, params object[] args)
        {
            Process(string.Format(template, args));
        }

        public override bool Exists(string template, params object[] args)
        {
            EnsureConnectionIsOpen();

            using (var command = CreateCommand(string.Format(template, args)))
            {
                using (var reader = command.ExecuteReader())
                {
                    try
                    {
                        if (!reader.Read()) return false;
                        if (int.Parse(reader[0].ToString()) <= 0) return false;

                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }
            }
        }

        public override DataSet ReadTableData(string schemaName, string tableName)
        {
            return Read("select * from [{0}]", tableName);
        }

        public override bool DefaultValueExists(string schemaName, string tableName, string columnName,
            object defaultValue)
        {
            return false;
        }

        public override void Process(PerformDBOperationExpression expression)
        {
            Logger.LogSay("Performing DB Operation");

            if (Options.PreviewOnly)
                return;

            EnsureConnectionIsOpen();
            expression.Operation?.Invoke(Connection, Transaction);
        }

        protected override void Process(string sql)
        {
            if (string.IsNullOrEmpty(sql))
                return;

            if (Options.PreviewOnly)
            {
                ExecuteBatchNonQuery(
                    sql,
                    sqlBatch =>
                    {
                        Logger.LogSql(sqlBatch);
                    },
                    (sqlBatch, goCount) =>
                    {
                        Logger.LogSql(sqlBatch);
                        Logger.LogSql($"GO {goCount}");
                    });

                return;
            }

            Logger.LogSql(sql);
            EnsureConnectionIsOpen();

            if (ContainsGo(sql))
                ExecuteBatchNonQuery(
                    sql,
                    sqlBatch =>
                    {
                        using (var command = CreateCommand(sqlBatch))
                        {
                            command.ExecuteNonQuery();
                        }
                    },
                    (sqlBatch, goCount) =>
                    {
                        using (var command = CreateCommand(sqlBatch))
                        {
                            for (var i = 0; i != goCount; ++i) command.ExecuteNonQuery();
                        }
                    });
            else
                ExecuteNonQuery(sql);
        }

        private bool ContainsGo(string sql)
        {
            var containsGo = false;
            batchParser.SpecialToken += (sender, args) => containsGo = true;

            using (var source = new TextReaderSource(new StringReader(sql), true))
            {
                batchParser.Process(source);
            }

            return containsGo;
        }

        private void ExecuteNonQuery(string sql)
        {
            using (var command = CreateCommand(sql))
            {
                try
                {
                    command.ExecuteNonQuery();
                }
                catch (DbException ex)
                {
                    throw new Exception(ex.Message + "\r\nWhile Processing:\r\n\"" + command.CommandText + "\"", ex);
                }
            }
        }

        private void ExecuteBatchNonQuery(string sql, Action<string> executeBatch, Action<string, int> executeGo)
        {
            var sqlBatch = string.Empty;

            try
            {
                batchParser.SqlText += (sender, args) =>
                {
                    sqlBatch = args.SqlText.Trim();
                };

                batchParser.SpecialToken += (sender, args) =>
                {
                    if (string.IsNullOrEmpty(sqlBatch))
                        return;

                    if (args.Opaque is GoSearcher.GoSearcherParameters goParams) executeGo(sqlBatch, goParams.Count);
                    sqlBatch = null;
                };

                using (var source = new TextReaderSource(new StringReader(sql), true))
                {
                    batchParser.Process(source, Options.StripComments);
                }

                if (!string.IsNullOrEmpty(sqlBatch)) executeBatch(sqlBatch);
            }
            catch (DbException ex)
            {
                throw new Exception(ex.Message + "\r\nWhile Processing:\r\n\"" + sqlBatch + "\"", ex);
            }
        }

        public override DataSet Read(string template, params object[] args)
        {
            EnsureConnectionIsOpen();

            using (var command = CreateCommand(string.Format(template, args)))
            {
                using (var reader = command.ExecuteReader())
                {
                    return reader.ReadDataSet();
                }
            }
        }
    }
}