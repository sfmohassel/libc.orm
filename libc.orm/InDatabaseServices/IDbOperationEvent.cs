using System.Collections.Generic;
using libc.orm.DatabaseConnection;
using libc.orm.Models;
namespace libc.orm.InDatabaseServices {
    public interface IDbOperationEvent {
        DbConn Db { get; set; }
        List<DbFluentResult> Results { get; set; }
    }
}