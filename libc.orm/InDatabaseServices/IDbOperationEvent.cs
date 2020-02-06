using System.Collections.Generic;
using libc.models;
using libc.orm.DatabaseConnection;
namespace libc.orm.InDatabaseServices {
    public interface IDbOperationEvent {
        DbConn Db { get; set; }
        List<FluentResult> Results { get; set; }
    }
}