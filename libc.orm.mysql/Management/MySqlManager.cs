using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Dapper;
using libc.orm.DatabaseManagementSystems;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using SqlKata.Compilers;
namespace libc.orm.mysql.Management {
    public class MySqlManager : IDbmsManager {
        private readonly ILogger log;
        public MySqlManager(ILogger log, MySqlConnectionStringBuilder masterConnectionString) {
            this.log = log;
            MasterConnectionString = new MySqlConnectionStringBuilder(masterConnectionString.GetConnectionString(true)) {
                Database = null
            };
            MySqlDumpPath = "mysqldump";
            MySqlPath = "mysql";
        }
        public MySqlManager(ILogger log, MySqlConnectionStringBuilder masterConnectionString, string mySqlDumpPath,
            string mySqlPath) : this(log, masterConnectionString) {
            MySqlDumpPath = mySqlDumpPath;
            MySqlPath = mySqlPath;
        }
        public MySqlConnectionStringBuilder MasterConnectionString { get; }
        public string MySqlDumpPath { get; }
        public string MySqlPath { get; }
        public Dbms Dbms => Dbms.MySql;
        public bool DatabaseExists(string dbName) {
            try {
                bool k;
                using (var dbconn = new MySqlConnection(MasterConnectionString.ConnectionString)) {
                    var q = $"SELECT COUNT(*) FROM information_schema.schemata WHERE SCHEMA_NAME='{dbName}'";
                    var m = dbconn.ExecuteScalar<int>(q);
                    k = m > 0;
                }
                return k;
            } catch (Exception ex) {
                log.LogError(ex.ToString());
                return false;
            }
        }
        public bool CreateDatabase(string dbName) {
            try {
                using (var dbConn = new MySqlConnection(MasterConnectionString.ConnectionString)) {
                    dbConn.Execute($"CREATE DATABASE `{dbName}`;");
                }
                return DatabaseExists(dbName);
            } catch (Exception ex) {
                log.LogCritical(ex.ToString());
                return false;
            }
        }
        public bool BackupDatabase(string dbName, string backupFullPath) {
            try {
                var cmd = new Cmd();
                var args = new StringBuilder();
                args.Append($" --host={MasterConnectionString.Server} ");
                args.Append($" --port={MasterConnectionString.Port} ");
                args.Append(" --single-transaction=TRUE ");
                args.Append(" --user=root ");
                args.Append($" --password={MasterConnectionString.Password} ");
                args.Append(" --routines --events --triggers ");
                args.Append($" \"{dbName}\" ");
                args.Append($" --result-file=\"{backupFullPath}\" ");
                var k = cmd.Run(MySqlDumpPath, args.ToString(), true);
                log.LogInformation(k.Output);
                return true;
            } catch (Exception ex) {
                log.LogCritical(ex.ToString());
                return false;
            }
        }
        public bool RestoreDatabase(string dbName, string backupFullPath) {
            // remove database
            if (DatabaseExists(dbName))
                if (!DeleteDatabase(dbName))
                    return false;
            // create database
            if (!CreateDatabase(dbName)) return false;

            // restore
            try {
                var cmd = new Cmd();
                var args = new StringBuilder();
                args.Append($" --host={MasterConnectionString.Server} ");
                args.Append($" --port={MasterConnectionString.Port} ");
                args.Append(" --user=root ");
                args.Append($" --password={MasterConnectionString.Password} ");
                args.Append($" --database={dbName} ");
                args.Append($" -e \"source {backupFullPath}\" ");
                var k = cmd.Run(MySqlPath, args.ToString(), true);
                log.LogInformation(k.Output);
                return true;
            } catch (Exception ex) {
                log.LogCritical(ex.ToString());
                return false;
            }
        }
        public bool DeleteDatabase(string dbName) {
            // close connections to database
            CloseConnections(dbName);

            // delete database
            try {
                using (var dbConn = new MySqlConnection(MasterConnectionString.ConnectionString)) {
                    dbConn.Execute($"drop schema `{dbName}`;");
                }
                return !DatabaseExists(dbName);
            } catch (Exception ex) {
                log.LogCritical(ex.ToString());
                return false;
            }
        }
        public bool CloseConnections(string dbName) {
            return true;
        }
        public string GetConnectionString(string dbName) {
            return new MySqlConnectionStringBuilder(MasterConnectionString.ConnectionString) {
                Database = dbName
            }.ConnectionString;
        }
        public IDbConnection EmptyConnectionFactory() {
            return new MySqlConnection();
        }
        public Func<IDbConnection> CreateConnectionFactory(Func<string> connectionStringFactory) {
            return () => new MySqlConnection(connectionStringFactory());
        }
        public Compiler CreateCompiler() {
            return new MySqlCompiler();
        }
        public List<string> GetAllDatabaseNames() {
            using (var connection = new MySqlConnection(MasterConnectionString.ConnectionString)) {
                return connection.Query<string>("SHOW DATABASES;").AsList();
            }
        }
    }
}