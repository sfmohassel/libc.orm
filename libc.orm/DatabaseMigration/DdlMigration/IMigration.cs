namespace libc.orm.DatabaseMigration.DdlMigration {
    public interface IMigration {
        int Version { get; }
        string Name { get; }
        /// <summary>
        ///     Gets the connection string passed to the task runner
        /// </summary>
        string ConnectionString { get; }
        /// <summary>
        ///     Collects all Up migration expressions in the <paramref name="context" />.
        /// </summary>
        /// <param name="context">The context to use while collecting the Up migration expressions</param>
        void GetUpExpressions(MigrationContext context);
        /// <summary>
        ///     Collects all Down migration expressions in the <paramref name="context" />.
        /// </summary>
        /// <param name="context">The context to use while collecting the Down migration expressions</param>
        void GetDownExpressions(MigrationContext context);
    }
}