using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using libc.orm.DatabaseMigration.Abstractions.Expressions.Base;
using libc.orm.DatabaseMigration.DdlProcessing;
using libc.orm.Resources;

namespace libc.orm.DatabaseMigration.Abstractions.Expressions
{
    /// <summary>
    ///     Expression to create a schema
    /// </summary>
    public class CreateSchemaExpression : MigrationExpressionBase, ISupportAdditionalFeatures
    {
        /// <summary>
        ///     Gets or sets the schema name
        /// </summary>
        [Required(ErrorMessageResourceType = typeof(Dmt), ErrorMessageResourceName = "SchemaNameCannotBeNullOrEmpty")]
        public virtual string SchemaName { get; set; }

        /// <inheritdoc />
        public IDictionary<string, object> AdditionalFeatures { get; } = new Dictionary<string, object>();

        public override void ExecuteWith(IProcessor processor)
        {
            processor.Process(this);
        }

        /// <inheritdoc />
        public override IMigrationExpression Reverse()
        {
            return new DeleteSchemaExpression
            {
                SchemaName = SchemaName
            };
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return base.ToString() + SchemaName;
        }
    }
}