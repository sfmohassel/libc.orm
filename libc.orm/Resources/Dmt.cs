using System.Reflection;
using libc.translation;
namespace libc.orm.Resources {
    public static class Dmt {
        public static readonly ILocalizer Instance = new Localizer(new LocalizationSource(Assembly.GetExecutingAssembly(),
            $"{typeof(Dmt).Namespace}.dmt.i18n.json"));
        public static string ColumnNameCannotBeNullOrEmpty => Instance.Get("ColumnNameCannotBeNullOrEmpty");
        public static string TableNameCannotBeNullOrEmpty => Instance.Get("TableNameCannotBeNullOrEmpty");
        public static string ForeignKeyNameCannotBeNullOrEmpty => Instance.Get("ForeignKeyNameCannotBeNullOrEmpty");
        public static string ForeignTableNameCannotBeNullOrEmpty => Instance.Get("ForeignTableNameCannotBeNullOrEmpty");
        public static string PrimaryTableNameCannotBeNullOrEmpty => Instance.Get("PrimaryTableNameCannotBeNullOrEmpty");
        public static string DefaultValueCannotBeNull => Instance.Get("DefaultValueCannotBeNull");
        public static string SequenceNameCannotBeNullOrEmpty => Instance.Get("SequenceNameCannotBeNullOrEmpty");
        public static string IndexNameCannotBeNullOrEmpty => Instance.Get("IndexNameCannotBeNullOrEmpty");
        public static string DestinationSchemaCannotBeNull => Instance.Get("DestinationSchemaCannotBeNull");
        public static string SchemaNameCannotBeNullOrEmpty => Instance.Get("SchemaNameCannotBeNullOrEmpty");
        public static string SqlScriptCannotBeNullOrEmpty => Instance.Get("SqlScriptCannotBeNullOrEmpty");
        public static string SqlStatementCannotBeNullOrEmpty => Instance.Get("SqlStatementCannotBeNullOrEmpty");
        public static string OldColumnNameCannotBeNullOrEmpty => Instance.Get("OldColumnNameCannotBeNullOrEmpty");
        public static string NewColumnNameCannotBeNullOrEmpty => Instance.Get("NewColumnNameCannotBeNullOrEmpty");
        public static string OldTableNameCannotBeNullOrEmpty => Instance.Get("OldTableNameCannotBeNullOrEmpty");
        public static string NewTableNameCannotBeNullOrEmpty => Instance.Get("NewTableNameCannotBeNullOrEmpty");
        public static string UpdateDataExpressionMustSpecifyWhereClauseOrAllRows =>
            Instance.Get("UpdateDataExpressionMustSpecifyWhereClauseOrAllRows");
        public static string UpdateDataExpressionMustNotSpecifyBothWhereClauseAndAllRows =>
            Instance.Get("UpdateDataExpressionMustNotSpecifyBothWhereClauseAndAllRows");
        public static string OperationCannotBeNull => Instance.Get("OperationCannotBeNull");
        public static string ColumnNamesMustBeUnique => Instance.Get("ColumnNamesMustBeUnique");
        public static string ColumnTypeMustBeDefined => Instance.Get("ColumnTypeMustBeDefined");
        public static string ConstraintMustHaveAtLeastOneColumn => Instance.Get("ConstraintMustHaveAtLeastOneColumn");
        public static string ForeignKeyMustHaveOneOrMorePrimaryColumns =>
            Instance.Get("ForeignKeyMustHaveOneOrMorePrimaryColumns");
        public static string ForeignKeyMustHaveOneOrMoreForeignColumns =>
            Instance.Get("ForeignKeyMustHaveOneOrMoreForeignColumns");
        public static string IndexMustHaveOneOrMoreColumns => Instance.Get("IndexMustHaveOneOrMoreColumns");
        public static string ExpressionTableNameMissing => Instance.Get("ExpressionTableNameMissing");
        public static string ExpressionTableNameMissingWithHints => Instance.Get("ExpressionTableNameMissingWithHints");
    }
}