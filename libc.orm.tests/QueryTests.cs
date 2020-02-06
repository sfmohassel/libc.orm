using System;
using libc.orm.DatabaseConnection;
using SqlKata;
using SqlKata.Compilers;
using Xunit;
namespace libc.orm.tests {
    public class QueryTests {
        [Fact]
        public void Postgres() {
            var compiler = new PostgresCompiler();
            var helper = compiler.GetHelper();
            var query = new Query()
                .From(new Query("category_translations").As("t"))
                .Join(new Query("categories").As("c"), j => j.On("c.Id", "t.CategoryId"))
                // فعلا سطح اول
                .Where("c.ParentId", Guid.Empty)
                .Where("t.Culture", "fa")
                .Where("c.Visible", true)
                .Select("t.Id as TranslationId", "t.Title", "t.Subtitle", "t.Thumbs");
            var queryString = compiler.Compile(query);

            query = new Query()
                .From(new Query("table").As("table_alias"))
                .Select(helper.Select.NoAlias<TableModel>(false, "table_alias", "Password"));
            queryString = compiler.Compile(query);

            var c2 = new SqlServerCompiler();
            var h2 = c2.GetHelper();
            var query2 = new Query()
                .From(new Query("table").As("table_alias"))
                .Select(h2.Select.NoAlias<TableModel>(false, "table_alias", "Password"));
            var s2 = c2.Compile(query2);
        }
        public class TableModel {
            public string UserName { get; set; }
            public string Password { get; set; }
        }
    }
}