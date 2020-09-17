using System.Collections.Generic;
using System.Linq;
using libc.orm.DatabaseConnection;
using libc.orm.Models;
using Newtonsoft.Json;
namespace libc.orm.InDatabaseServices {
    public class DbOperationEvent : IDbOperationEvent {
        [JsonIgnore]
        public DbConn Db { get; set; }
        public List<DbFluentResult> Results { get; set; } = new List<DbFluentResult>();
        public bool IsOk() {
            return Results.Count == 0 || Results.All(a => a.IsOk());
        }
        public string[] Errors() {
            return Results.SelectMany(a => a.Errors).ToArray();
        }
        public string[] Messages() {
            return Results.SelectMany(a => a.Messages).ToArray();
        }
        public DbFluentResult Into(DbFluentResult res) {
            return res.AddError(Errors()).AddMessage(Messages());
        }
    }
}