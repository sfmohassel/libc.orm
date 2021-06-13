#region License

//
// Copyright (c) 2018, Fluent Migrator Project
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
using System.Linq;
using libc.orm.DatabaseMigration.Abstractions;
using libc.orm.DatabaseMigration.Abstractions.Model;
using libc.orm.DatabaseMigration.DdlGeneration;

namespace libc.orm.postgres.DdlGeneration.Postgres
{
    internal class PostgresColumn : ColumnBase
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PostgresColumn" /> class.
        /// </summary>
        /// <param name="quoter">The Postgres quoter.</param>
        /// <param name="typeMap">The Postgres type map.</param>
        public PostgresColumn(PostgresQuoter quoter, ITypeMap typeMap)
            : base(typeMap, quoter)
        {
            AlterClauseOrder = new List<Func<ColumnDefinition, string>>
            {
                FormatAlterType,
                FormatAlterNullable
            };
        }

        protected IList<Func<ColumnDefinition, string>> AlterClauseOrder { get; set; }

        public string FormatAlterDefaultValue(string column, object defaultValue)
        {
            var formatDefaultValue = FormatDefaultValue(new ColumnDefinition
            {
                Name = column,
                DefaultValue = defaultValue
            });

            return string.Format("SET {0}", formatDefaultValue);
        }

        private string FormatAlterNullable(ColumnDefinition column)
        {
            if (!column.IsNullable.HasValue)
                return "";

            if (column.IsNullable.Value)
                return "DROP NOT NULL";

            return "SET NOT NULL";
        }

        private string FormatAlterType(ColumnDefinition column)
        {
            return string.Format("TYPE {0}", GetColumnType(column));
        }

        public string GenerateAlterClauses(ColumnDefinition column)
        {
            var clauses = new List<string>();

            foreach (var action in AlterClauseOrder)
            {
                var columnClause = action(column);

                if (!string.IsNullOrEmpty(columnClause))
                    clauses.Add(string.Format("ALTER {0} {1}", Quoter.QuoteColumnName(column.Name), columnClause));
            }

            return string.Join(", ", clauses.ToArray());
        }

        /// <inheritdoc />
        protected override string FormatNullable(ColumnDefinition column)
        {
            if (column.IsNullable == true && column.Type == null && !string.IsNullOrEmpty(column.CustomType))
                return "NULL";

            return base.FormatNullable(column);
        }

        /// <inheritdoc />
        protected override string FormatIdentity(ColumnDefinition column)
        {
            return string.Empty;
        }

        /// <inheritdoc />
        public override string AddPrimaryKeyConstraint(string tableName,
            IEnumerable<ColumnDefinition> primaryKeyColumns)
        {
            var columnDefinitions = primaryKeyColumns.ToList();
            var pkName = GetPrimaryKeyConstraintName(columnDefinitions, tableName);
            var cols = string.Empty;
            var first = true;

            foreach (var col in columnDefinitions)
            {
                if (first)
                    first = false;
                else
                    cols += ",";

                cols += Quoter.QuoteColumnName(col.Name);
            }

            if (string.IsNullOrEmpty(pkName))
                return string.Format(", PRIMARY KEY ({0})", cols);

            return string.Format(", {0}PRIMARY KEY ({1})", pkName, cols);
        }

        /// <inheritdoc />
        protected override string FormatType(ColumnDefinition column)
        {
            if (column.IsIdentity)
            {
                if (column.Type == DbType.Int64)
                    return "bigserial";

                return "serial";
            }

            return base.FormatType(column);
        }

        public string GetColumnType(ColumnDefinition column)
        {
            return FormatType(column);
        }
    }
}