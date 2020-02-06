using libc.orm.postgres.Management;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Xunit;
namespace libc.orm.tests {
    public class PostgresManagerTests {
        [Fact]
        public void GetDatabaseNames() {
            var cstr = "Server=localhost;Port=5432;Uid=postgres;Pwd=Admin!@#;CommandTimeout=20;Timeout=3;";
            var cstrb = new NpgsqlConnectionStringBuilder(cstr);
            var manager = new PostgresManager(NullLogger.Instance, cstrb);
            var databases = manager.GetAllDatabaseNames();
        }
    }
}