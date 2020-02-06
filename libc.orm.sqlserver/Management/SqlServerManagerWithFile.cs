using System;
using System.Data.SqlClient;
using System.IO;
using Dapper;
using Microsoft.Extensions.Logging;
namespace libc.orm.sqlserver.Management {
    [Obsolete]
    public class SqlServerManagerWithFile : SqlServerManager {
        private readonly ILogger log;
        public SqlServerManagerWithFile(ILogger log, SqlConnectionStringBuilder masterConnectionStringBuilder,
            SqlServerRecoveryModels recovery, string databaseDirectory) : base(log,
            masterConnectionStringBuilder, recovery) {
            this.log = log;
            DatabaseDirectory = databaseDirectory;
        }
        public string DatabaseDirectory { get; }
        public override bool CreateDatabase(string dbName) {
            try {
                string[] files = {
                    Path.Combine(DatabaseDirectory, dbName + ".mdf"),
                    Path.Combine(DatabaseDirectory, dbName + ".ldf")
                };
                var query = "CREATE DATABASE " + dbName +
                            " ON PRIMARY" +
                            " (NAME = " + dbName + "_data," +
                            " FILENAME = '" + files[0] + "'," +
                            " SIZE = 5MB," +
                            " FILEGROWTH = 10%)" +
                            " LOG ON" +
                            " (NAME = " + dbName + "_log," +
                            " FILENAME = '" + files[1] + "'," +
                            " SIZE = 1MB," +
                            " FILEGROWTH = 10%)" +
                            ";";
                using (var db = new SqlConnection(MasterConnectionString.ConnectionString)) {
                    db.Execute(query);
                }

                //set recovery model
                var r = Recovery == SqlServerRecoveryModels.Simple
                    ? "SIMPLE"
                    : "FULL";
                var query2 = $"ALTER DATABASE {dbName} SET RECOVERY {r};";
                using (var db = new SqlConnection(MasterConnectionString.ConnectionString)) {
                    db.Execute(query2);
                }

                //set auto close to off
                var query3 = $"ALTER DATABASE {dbName} SET AUTO_CLOSE OFF";
                using (var db = new SqlConnection(MasterConnectionString.ConnectionString)) {
                    db.Execute(query3);
                }
                return DatabaseExists(dbName);
            } catch (Exception ex) {
                log.LogCritical(ex.ToString());
                return false;
            }
        }
        public override bool RestoreDatabase(string dbName, string backupFullPath) {
            try {
                string[] files = {
                    Path.Combine(DatabaseDirectory, dbName + ".mdf"),
                    Path.Combine(DatabaseDirectory, dbName + ".ldf")
                };
                var query = $"RESTORE DATABASE [{dbName}] FROM " +
                            $"DISK = N'{backupFullPath}' WITH  FILE = 1, " +
                            $"MOVE N'{dbName}_data' TO N'{files[0]}', " +
                            $"MOVE N'{dbName}_log' TO N'{files[1]}', " +
                            "NOUNLOAD,  REPLACE,  STATS = 5";
                if (DatabaseExists(dbName)) {
                    var singleUserModeQuery =
                        $"ALTER DATABASE [{dbName}] SET Single_User WITH Rollback Immediate";
                    var multiUserModeQuery = $"ALTER DATABASE [{dbName}] SET Multi_User";
                    using (var db = new SqlConnection(MasterConnectionString.ConnectionString)) {
                        db.Execute(singleUserModeQuery);
                        db.Execute(query);
                        db.Execute(multiUserModeQuery);
                    }
                } else {
                    using (var db = new SqlConnection(MasterConnectionString.ConnectionString)) {
                        db.Execute(query);
                    }
                }
                return true;
            } catch (Exception ex) {
                log.LogCritical(ex.ToString());
                return false;
            }
        }
    }
}