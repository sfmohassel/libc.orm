using System.Collections.Generic;
using libc.orm.DatabaseMigration.Abstractions;
using libc.orm.DatabaseMigration.Abstractions.Expressions.Base;

namespace libc.orm.DatabaseMigration.DdlMigration
{
    public class MigrationContext
    {
        public MigrationContext(IQuerySchema querySchema, string connectionString)
        {
            QuerySchema = querySchema;
            ConnectionString = connectionString;
            Expressions = new List<IMigrationExpression>();
        }

        /// <summary>
        ///     Gets or sets the collection of expressions
        /// </summary>
        public ICollection<IMigrationExpression> Expressions { get; set; }

        /// <summary>
        ///     Gets the <see cref="IQuerySchema" /> to access the database
        /// </summary>
        public IQuerySchema QuerySchema { get; }

        /// <summary>
        ///     Gets or sets the connection string
        /// </summary>
        public string ConnectionString { get; set; }
    }
}