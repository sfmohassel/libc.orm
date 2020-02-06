using System;
using libc.orm.DatabaseMigration.Abstractions.Builders.Alter;
using libc.orm.DatabaseMigration.Abstractions.Builders.Create;
using libc.orm.DatabaseMigration.Abstractions.Builders.Delete;
using libc.orm.DatabaseMigration.Abstractions.Builders.Execute;
using libc.orm.DatabaseMigration.Abstractions.Builders.IfDatabase;
using libc.orm.DatabaseMigration.Abstractions.Builders.Insert;
using libc.orm.DatabaseMigration.Abstractions.Builders.Rename;
using libc.orm.DatabaseMigration.Abstractions.Builders.Schema;
using libc.orm.DatabaseMigration.Abstractions.Builders.Update;
using libc.orm.DatabaseMigration.DdlExpressionBuilders.Alter;
using libc.orm.DatabaseMigration.DdlExpressionBuilders.Create;
using libc.orm.DatabaseMigration.DdlExpressionBuilders.Delete;
using libc.orm.DatabaseMigration.DdlExpressionBuilders.Execute;
using libc.orm.DatabaseMigration.DdlExpressionBuilders.IfDatabase;
using libc.orm.DatabaseMigration.DdlExpressionBuilders.Insert;
using libc.orm.DatabaseMigration.DdlExpressionBuilders.Rename;
using libc.orm.DatabaseMigration.DdlExpressionBuilders.Schema;
using libc.orm.DatabaseMigration.DdlExpressionBuilders.Update;
namespace libc.orm.DatabaseMigration.DdlMigration {
    public abstract class Migration : IMigration {
        private readonly object _mutex = new object();
        protected MigrationContext Context { get; private set; }
        /// <summary>
        ///     Gets the starting point for alterations
        /// </summary>
        public IAlterExpressionRoot Alter => new AlterExpressionRoot(Context);
        /// <summary>
        ///     Gets the starting point for creating database objects
        /// </summary>
        public ICreateExpressionRoot Create => new CreateExpressionRoot(Context);
        /// <summary>
        ///     Gets the starting point for renaming database objects
        /// </summary>
        public IRenameExpressionRoot Rename => new RenameExpressionRoot(Context);
        /// <summary>
        ///     Gets the starting point for data insertion
        /// </summary>
        public IInsertExpressionRoot Insert => new InsertExpressionRoot(Context);
        /// <summary>
        ///     Gets the starting point for data deletions
        /// </summary>
        public IDeleteExpressionRoot Delete => new DeleteExpressionRoot(Context);
        /// <summary>
        ///     Gets the starting point for SQL execution
        /// </summary>
        public IExecuteExpressionRoot Execute => new ExecuteExpressionRoot(Context);
        /// <summary>
        ///     Gets the starting point for data updates
        /// </summary>
        public IUpdateExpressionRoot Update => new UpdateExpressionRoot(Context);
        /// <summary>
        ///     Gets the starting point for schema-rooted expressions
        /// </summary>
        public ISchemaExpressionRoot Schema => new SchemaExpressionRoot(Context);
        public abstract int Version { get; }
        public abstract string Name { get; }
        public string ConnectionString { get; private set; }
        public void GetUpExpressions(MigrationContext context) {
            lock (_mutex) {
                Context = context;
                ConnectionString = Context.ConnectionString;
                Up();
                Context = null;
                ConnectionString = string.Empty;
            }
        }
        public void GetDownExpressions(MigrationContext context) {
            lock (_mutex) {
                Context = context;
                ConnectionString = Context.ConnectionString;
                Down();
                Context = null;
                ConnectionString = string.Empty;
            }
        }
        /// <summary>
        ///     Collect the UP migration expressions
        /// </summary>
        public abstract void Up();
        /// <summary>
        ///     Collects the DOWN migration expressions
        /// </summary>
        public abstract void Down();
        /// <summary>
        ///     Gets the starting point for database specific expressions
        /// </summary>
        /// <param name="databaseType">The supported database types</param>
        /// <returns>The database specific expression</returns>
        public IIfDatabaseExpressionRoot IfDatabase(params string[] databaseType) {
            return new IfDatabaseExpressionRoot(Context, databaseType);
        }
        /// <summary>
        ///     Gets the starting point for database specific expressions
        /// </summary>
        /// <param name="databaseTypeFunc">The lambda that tests if the expression can be applied to the current database</param>
        /// <returns>The database specific expression</returns>
        public IIfDatabaseExpressionRoot IfDatabase(Predicate<string> databaseTypeFunc) {
            return new IfDatabaseExpressionRoot(Context, databaseTypeFunc);
        }
    }
}