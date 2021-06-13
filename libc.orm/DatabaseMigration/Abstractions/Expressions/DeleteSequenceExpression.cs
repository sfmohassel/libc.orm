using System.ComponentModel.DataAnnotations;
using libc.orm.DatabaseMigration.Abstractions.Expressions.Base;
using libc.orm.DatabaseMigration.DdlProcessing;
using libc.orm.Resources;

namespace libc.orm.DatabaseMigration.Abstractions.Expressions
{
    /// <summary>
    ///     Expression to delete a sequence
    /// </summary>
    public class DeleteSequenceExpression : MigrationExpressionBase, ISchemaExpression
    {
        /// <summary>
        ///     Gets or sets the sequence name
        /// </summary>
        [Required(ErrorMessageResourceType = typeof(Dmt),
            ErrorMessageResourceName = nameof(Dmt.SequenceNameCannotBeNullOrEmpty))]
        public virtual string SequenceName { get; set; }

        /// <inheritdoc />
        public virtual string SchemaName { get; set; }

        public override void ExecuteWith(IProcessor processor)
        {
            processor.Process(this);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return base.ToString() + SequenceName;
        }
    }
}