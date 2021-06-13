namespace libc.orm.DatabaseMigration.DdlProcessing
{
    public class FormatHelper
    {
        public static string FormatSqlEscape(string sql)
        {
            return sql.Replace("'", "''");
        }
    }
}