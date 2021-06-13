using System.ComponentModel.DataAnnotations;
using libc.orm.DatabaseMigration.Abstractions.Expressions.Base;
using libc.orm.DatabaseMigration.DdlProcessing;
using libc.orm.Resources;

namespace libc.orm.DatabaseMigration.Abstractions.Expressions
{
    /// <summary>
    ///     Expression to delete constraints
    /// </summary>
    public class DeleteDefaultConstraintExpression : MigrationExpressionBase, ISchemaExpression
    {
        /// <summary>
        ///     Gets or sets the table name
        /// </summary>
        [Required(ErrorMessageResourceType = typeof(Dmt), ErrorMessageResourceName = "TableNameCannotBeNullOrEmpty")]
        public virtual string TableName { get; set; }

        /// <summary>
        ///     Gets or sets the column name
        /// </summary>
        [Required(ErrorMessageResourceType = typeof(Dmt), ErrorMessageResourceName = "ColumnNameCannotBeNullOrEmpty")]
        public virtual string ColumnName { get; set; }

        /// <inheritdoc />
        public virtual string SchemaName { get; set; }

        public override void ExecuteWith(IProcessor processor)
        {
            processor.Process(this);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return base.ToString() +
                   string.Format("{0}.{1} {2}",
                       SchemaName,
                       TableName,
                       ColumnName);
        }
    }
}