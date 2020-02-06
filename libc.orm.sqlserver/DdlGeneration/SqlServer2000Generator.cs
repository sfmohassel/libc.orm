#region License

//
// Copyright (c) 2007-2018, Sean Chambers <schambers80@gmail.com>
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
using System.Linq;
using System.Text;
using libc.orm.DatabaseMigration.Abstractions.Expressions;
using libc.orm.DatabaseMigration.Abstractions.Extensions;
using libc.orm.DatabaseMigration.DdlGeneration;
using libc.orm.sqlserver.DdlProcessing.Extensions;
using libc.orm.sqlserver.DdlProcessing.Extensions.SqlServer;
namespace libc.orm.sqlserver.DdlGeneration {
    public class SqlServer2000Generator : GenericGenerator {
        private readonly IEnumerable<string> _supportedAdditionalFeatures = new List<string> {
            SqlServerExtensions.IdentityInsert,
            SqlServerExtensions.IdentitySeed,
            SqlServerExtensions.IdentityIncrement,
            SqlServerExtensions.ConstraintType
        };
        public SqlServer2000Generator(SqlServer2000Quoter quoter, GeneratorOptions options)
            : base(new SqlServer2000Column(new SqlServer2000TypeMap(), quoter),
                quoter, new EmptyDescriptionGenerator(), options) {
        }
        public override string RenameTable => "sp_rename {0}, {1}";
        public override string RenameColumn => "sp_rename {0}, {1}";
        public override string DropIndex => "DROP INDEX {1}.{0}";
        public override string AddColumn => "ALTER TABLE {0} ADD {1}";
        public virtual string IdentityInsert => "SET IDENTITY_INSERT {0} {1}";
        public override string CreateConstraint => "ALTER TABLE {0} ADD CONSTRAINT {1} {2}{3} ({4})";

        //Not need for the nonclusted keyword as it is the default mode
        public override string GetClusterTypeString(CreateIndexExpression column) {
            return column.Index.IsClustered ? "CLUSTERED " : string.Empty;
        }
        protected virtual string GetConstraintClusteringString(CreateConstraintExpression constraint) {
            object indexType;
            if (!constraint.Constraint.AdditionalFeatures.TryGetValue(
                SqlServerExtensions.ConstraintType, out indexType)) return string.Empty;
            return indexType.Equals(SqlServerConstraintType.Clustered) ? " CLUSTERED" : " NONCLUSTERED";
        }
        public override string Generate(CreateConstraintExpression expression) {
            var constraintType = expression.Constraint.IsPrimaryKeyConstraint ? "PRIMARY KEY" : "UNIQUE";
            var constraintClustering = GetConstraintClusteringString(expression);
            var columns = string.Join(", ", expression.Constraint.Columns.Select(x => Quoter.QuoteColumnName(x)).ToArray());
            return string.Format(CreateConstraint,
                Quoter.QuoteTableName(expression.Constraint.TableName, expression.Constraint.SchemaName),
                Quoter.Quote(expression.Constraint.ConstraintName),
                constraintType,
                constraintClustering,
                columns);
        }
        public override string Generate(RenameTableExpression expression) {
            var sourceParam = Quoter.QuoteValue(Quoter.QuoteTableName(expression.OldName, expression.SchemaName));
            var destinationParam = Quoter.QuoteValue(expression.NewName);
            return string.Format(RenameTable, sourceParam, destinationParam);
        }
        public override string Generate(RenameColumnExpression expression) {
            var tableName = Quoter.QuoteTableName(expression.TableName, expression.SchemaName);
            var columnName = Quoter.QuoteColumnName(expression.OldName);
            var sourceParam = Quoter.QuoteValue($"{tableName}.{columnName}");
            var destinationParam = Quoter.QuoteValue(expression.NewName);
            return string.Format(RenameColumn, sourceParam, destinationParam);
        }
        public override string Generate(DeleteColumnExpression expression) {
            // before we drop a column, we have to drop any default value constraints in SQL Server
            var builder = new StringBuilder();
            foreach (var column in expression.ColumnNames) {
                if (expression.ColumnNames.First() != column) builder.AppendLine("GO");
                BuildDelete(expression, column, builder);
            }
            return builder.ToString();
        }
        protected virtual void BuildDelete(DeleteColumnExpression expression, string columnName, StringBuilder builder) {
            builder.AppendLine(
                Generate(
                    new DeleteDefaultConstraintExpression {
                        ColumnName = columnName,
                        SchemaName = expression.SchemaName,
                        TableName = expression.TableName
                    }));
            builder.AppendLine();
            builder.AppendLine(string.Format(
                "-- now we can finally drop column" + Environment.NewLine + "ALTER TABLE {0} DROP COLUMN {1};",
                Quoter.QuoteTableName(expression.TableName, expression.SchemaName),
                Quoter.QuoteColumnName(columnName)));
        }
        public override string Generate(AlterDefaultConstraintExpression expression) {
            // before we alter a default constraint on a column, we have to drop any default value constraints in SQL Server
            var builder = new StringBuilder();
            builder.AppendLine(Generate(new DeleteDefaultConstraintExpression {
                ColumnName = expression.ColumnName,
                SchemaName = expression.SchemaName,
                TableName = expression.TableName
            }));
            builder.AppendLine();
            builder.AppendFormat(
                "-- create alter table command to create new default constraint as string and run it" +
                Environment.NewLine + "ALTER TABLE {0} WITH NOCHECK ADD CONSTRAINT {3} DEFAULT({2}) FOR {1};",
                Quoter.QuoteTableName(expression.TableName, expression.SchemaName),
                Quoter.QuoteColumnName(expression.ColumnName),
                SqlServer2000Column.FormatDefaultValue(expression.DefaultValue, Quoter),
                Quoter.QuoteConstraintName(
                    SqlServer2000Column.GetDefaultConstraintName(expression.TableName, expression.ColumnName)));
            return builder.ToString();
        }
        public override string Generate(InsertDataExpression expression) {
            if (IsUsingIdentityInsert(expression))
                return string.Format("{0}; {1}; {2}",
                    string.Format(IdentityInsert, Quoter.QuoteTableName(expression.TableName, expression.SchemaName), "ON"),
                    base.Generate(expression),
                    string.Format(IdentityInsert, Quoter.QuoteTableName(expression.TableName, expression.SchemaName),
                        "OFF"));
            return base.Generate(expression);
        }
        protected static bool IsUsingIdentityInsert(InsertDataExpression expression) {
            if (expression.AdditionalFeatures.ContainsKey(SqlServerExtensions.IdentityInsert))
                return (bool) expression.AdditionalFeatures[SqlServerExtensions.IdentityInsert];
            return false;
        }
        public override string Generate(CreateSequenceExpression expression) {
            return CompatibilityMode.HandleCompatibilty("Sequences are not supported in SqlServer2000");
        }
        public override string Generate(DeleteSequenceExpression expression) {
            return CompatibilityMode.HandleCompatibilty("Sequences are not supported in SqlServer2000");
        }
        public override string Generate(DeleteDefaultConstraintExpression expression) {
            var sql =
                "DECLARE @default sysname, @sql nvarchar(4000);" + Environment.NewLine + Environment.NewLine +
                "-- get name of default constraint" + Environment.NewLine +
                "SELECT @default = name" + Environment.NewLine +
                "FROM sys.default_constraints" + Environment.NewLine +
                "WHERE parent_object_id = object_id('{0}')" + Environment.NewLine +
                "AND type = 'D'" + Environment.NewLine +
                "AND parent_column_id = (" + Environment.NewLine +
                "SELECT column_id" + Environment.NewLine +
                "FROM sys.columns" + Environment.NewLine +
                "WHERE object_id = object_id('{0}')" + Environment.NewLine +
                "AND name = '{1}'" + Environment.NewLine +
                ");" + Environment.NewLine + Environment.NewLine +
                "-- create alter table command to drop constraint as string and run it" + Environment.NewLine +
                "SET @sql = N'ALTER TABLE {0} DROP CONSTRAINT ' + QUOTENAME(@default);" + Environment.NewLine +
                "EXEC sp_executesql @sql;";
            return string.Format(sql, Quoter.QuoteTableName(expression.TableName, expression.SchemaName),
                expression.ColumnName);
        }
        public override bool IsAdditionalFeatureSupported(string feature) {
            return _supportedAdditionalFeatures.Any(x => x == feature);
        }
    }
}