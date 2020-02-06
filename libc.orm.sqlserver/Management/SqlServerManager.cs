using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using libc.orm.DatabaseManagementSystems;
using Microsoft.Extensions.Logging;
using SqlKata.Compilers;
namespace libc.orm.sqlserver.Management {
    public class SqlServerManager : IDbmsManager {
        private readonly ILogger log;
        public SqlServerManager(ILogger log, SqlConnectionStringBuilder masterConnectionString,
            SqlServerRecoveryModels recovery) {
            this.log = log;
            MasterConnectionString = new SqlConnectionStringBuilder(masterConnectionString.ConnectionString) {
                InitialCatalog = "master"
            };
            Recovery = recovery;
        }
        public SqlConnectionStringBuilder MasterConnectionString { get; }
        public SqlServerRecoveryModels Recovery { get; }
        public Dbms Dbms => Dbms.SqlServer;
        public bool DatabaseExists(string dbName) {
            try {
                var m = new SqlConnectionStringBuilder(GetConnectionString(dbName)) {
                    ConnectTimeout = 1
                };
                using (var db = new SqlConnection(m.ConnectionString)) {
                    db.Open();
                    db.Close();
                }
                return true;
            } catch (Exception ex) {
                log.LogCritical(ex.ToString());
                return false;
            }
        }
        public virtual bool CreateDatabase(string dbName) {
            try {
                //create database
                var query = $"CREATE DATABASE [{dbName}]";
                using (var db = new SqlConnection(MasterConnectionString.ConnectionString)) {
                    db.Execute(query);
                }

                //set recovery model
                var r = Recovery == SqlServerRecoveryModels.Simple ? "SIMPLE" : "FULL";
                var query2 = $"ALTER DATABASE [{dbName}] SET RECOVERY {r};";
                using (var db = new SqlConnection(MasterConnectionString.ConnectionString)) {
                    db.Execute(query2);
                }

                //set auto close to off
                var query3 = $"ALTER DATABASE [{dbName}] SET AUTO_CLOSE OFF";
                using (var db = new SqlConnection(MasterConnectionString.ConnectionString)) {
                    db.Execute(query3);
                }
                return DatabaseExists(dbName);
            } catch (Exception ex) {
                log.LogCritical(ex.ToString());
                return false;
            }
        }
        public bool BackupDatabase(string dbName, string backupFullPath) {
            try {
                var query = $@"
BACKUP DATABASE [{dbName}] TO DISK = N'{backupFullPath}' 
WITH NOFORMAT, INIT, NAME = N'{dbName}-Full Database Backup', SKIP, NOREWIND, NOUNLOAD, STATS = 10
";
                using (var db = new SqlConnection(GetConnectionString(dbName))) {
                    db.Execute(query);
                }
                return true;
            } catch (Exception ex) {
                log.LogCritical(ex.ToString());
                return false;
            }
        }
        public virtual bool RestoreDatabase(string dbName, string backupFullPath) {
            try {
                var query = $"RESTORE DATABASE [{dbName}] FROM DISK='{backupFullPath}' WITH REPLACE";
                if (DatabaseExists(dbName)) {
                    var singleUserModeQuery =
                        $"ALTER DATABASE [{dbName}] SET Single_User WITH Rollback Immediate";
                    var multiUserModeQuery = $"ALTER DATABASE [{dbName}] SET Multi_User";
                    using (var db = new SqlConnection(MasterConnectionString.ConnectionString)) {
                        try {
                            db.Execute(singleUserModeQuery);
                            db.Execute(query);
                        } finally {
                            db.Execute(multiUserModeQuery);
                        }
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
        public bool DeleteDatabase(string dbName) {
            try {
                var singleUserModeQuery = $"ALTER DATABASE [{dbName}] SET Single_User WITH Rollback Immediate";
                var query = $"DROP DATABASE [{dbName}]";
                var multiUserModeQuery = $"ALTER DATABASE [{dbName}] SET Multi_User";
                using (var db = new SqlConnection(MasterConnectionString.ConnectionString)) {
                    try {
                        db.Execute(singleUserModeQuery);
                        db.Execute(query);
                    } catch {
                        db.Execute(multiUserModeQuery);
                    }
                }
                return !DatabaseExists(dbName);
            } catch (Exception ex) {
                log.LogCritical(ex.ToString());
                return false;
            }
        }
        public bool CloseConnections(string dbName) {
            try {
                var singleUserModeQuery = $"ALTER DATABASE [{dbName}] SET Single_User WITH Rollback Immediate";
                using (var db = new SqlConnection(MasterConnectionString.ConnectionString)) {
                    db.Execute(singleUserModeQuery);
                }
                return true;
            } catch (Exception ex) {
                log.LogCritical(ex.ToString());
                return false;
            }
        }
        public string GetConnectionString(string dbName) {
            return new SqlConnectionStringBuilder(MasterConnectionString.ConnectionString) {
                InitialCatalog = dbName
            }.ConnectionString;
        }
        public IDbConnection EmptyConnectionFactory() {
            return new SqlConnection();
        }
        public Func<IDbConnection> CreateConnectionFactory(Func<string> connectionStringFactory) {
            return () => new SqlConnection(connectionStringFactory());
        }
        public Compiler CreateCompiler() {
            return new SqlServerCompiler();
        }
        public List<string> GetAllDatabaseNames() {
            const string query =
                "SELECT name FROM master.dbo.sysdatabases WHERE name NOT IN ('master', 'tempdb', 'model', 'msdb');";
            List<SqlServerDatabase> list;
            using (var conn = new SqlConnection(MasterConnectionString.ConnectionString)) {
                list = conn.Query<SqlServerDatabase>(query).AsList();
            }
            return list.Select(a => a.name).ToList();
        }
        // ReSharper disable once ClassNeverInstantiated.Local
        private class SqlServerDatabase {
            // ReSharper disable once UnusedMember.Global
            public string name { get; set; }
        }
    }
}