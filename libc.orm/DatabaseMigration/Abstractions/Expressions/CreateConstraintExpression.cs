using System.Collections.Generic;
using libc.orm.DatabaseMigration.Abstractions.Expressions.Base;
using libc.orm.DatabaseMigration.Abstractions.Model;
using libc.orm.DatabaseMigration.Abstractions.Validation;
using libc.orm.DatabaseMigration.DdlProcessing;
namespace libc.orm.DatabaseMigration.Abstractions.Expressions {
    /// <summary>
    ///     The expression to create a constraint
    /// </summary>
    public class CreateConstraintExpression : MigrationExpressionBase, ISupportAdditionalFeatures, IConstraintExpression,
        IValidationChildren {
        /// <summary>
        ///     Initializes a new instance of the <see cref="CreateConstraintExpression" /> class.
        /// </summary>
        public CreateConstraintExpression(ConstraintType type) {
            // ReSharper disable once VirtualMemberCallInConstructor
            Constraint = new ConstraintDefinition(type);
        }
        /// <inheritdoc />
        public virtual ConstraintDefinition Constraint { get; set; }
        /// <inheritdoc />
        public IDictionary<string, object> AdditionalFeatures => Constraint.AdditionalFeatures;
        /// <inheritdoc />
        IEnumerable<object> IValidationChildren.Children {
            get {
                yield return Constraint;
            }
        }
        public override void ExecuteWith(IProcessor processor) {
            processor.Process(this);
        }
        /// <inheritdoc />
        public override IMigrationExpression Reverse() {
            //constraint type is private in ConstraintDefinition
            return new DeleteConstraintExpression(Constraint.IsPrimaryKeyConstraint
                ? ConstraintType.PrimaryKey
                : ConstraintType.Unique) {
                Constraint = Constraint
            };
        }
        /// <inheritdoc />
        public override string ToString() {
            return base.ToString() + Constraint.ConstraintName;
        }
    }
}