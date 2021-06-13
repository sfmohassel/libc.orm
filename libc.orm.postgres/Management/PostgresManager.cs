using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Dapper;
using libc.orm.DatabaseManagementSystems;
using Microsoft.Extensions.Logging;
using Npgsql;
using SqlKata.Compilers;

namespace libc.orm.postgres.Management
{
    public class PostgresManager : IDbmsManager
    {
        private readonly ILogger log;

        public PostgresManager(ILogger log, NpgsqlConnectionStringBuilder masterConnectionString)
        {
            this.log = log;
            var k = new NpgsqlConnectionStringBuilder(masterConnectionString.ConnectionString);

            if (k.ContainsKey("database"))
                k.Remove("database");
            else if (k.ContainsKey("Database")) k.Remove("Database");

            MasterConnectionString = new NpgsqlConnectionStringBuilder(k.ToString())
            {
                Database = null
            };

            PgDumpPath = "pg_dump";
            PgRestorePath = "pg_restore";
            PSqlPath = "psql";
        }

        public PostgresManager(ILogger log, NpgsqlConnectionStringBuilder masterConnectionString,
            string pgDumpPath, string pgRestorePath, string psqlPath)
            : this(log, masterConnectionString)
        {
            PgDumpPath = pgDumpPath;
            PgRestorePath = pgRestorePath;
            PSqlPath = psqlPath;
        }

        public NpgsqlConnectionStringBuilder MasterConnectionString { get; }
        public string PgDumpPath { get; }
        public string PgRestorePath { get; }
        public string PSqlPath { get; }
        public Dbms Dbms => Dbms.Postgres;

        public bool DatabaseExists(string dbName)
        {
            try
            {
                var k = false;
                var query = $"SELECT 1 AS result FROM pg_database WHERE datname = '{dbName}'";

                using (var db = new NpgsqlConnection(MasterConnectionString.ConnectionString))
                {
                    using (var reader = db.ExecuteReader(query))
                    {
                        while (reader.Read()) k = true;
                    }
                }

                return k;
            }
            catch (Exception ex)
            {
                log.LogCritical(ex.ToString());

                return false;
            }
        }

        public bool CreateDatabase(string dbName)
        {
            try
            {
                var q = $@"
                    CREATE DATABASE ""{dbName}""
                    WITH OWNER = postgres 
                    ENCODING = 'UTF8' 
                    CONNECTION LIMIT = -1;";

                using (var db = new NpgsqlConnection(MasterConnectionString.ConnectionString))
                {
                    db.Execute(q);
                }

                return DatabaseExists(dbName);
            }
            catch (Exception ex)
            {
                log.LogCritical(ex.ToString());

                return false;
            }
        }

        public bool BackupDatabase(string dbName, string backupFullPath)
        {
            try
            {
                var args = new StringBuilder();
                args.Append($" --host {MasterConnectionString.Host}");
                args.Append($" --port {MasterConnectionString.Port}");
                args.Append($" --username {MasterConnectionString.Username}");
                args.Append(" --format custom");
                args.Append(" --blobs");
                args.Append(" --verbose");
                args.Append($" --dbname \"{dbName}\"");
                args.Append($" --file \"{backupFullPath}\"");
                Environment.SetEnvironmentVariable("PGPASSWORD", MasterConnectionString.Password);
                var cmd = new Cmd();
                var k = cmd.Run(PgDumpPath, args.ToString(), true);
                log.LogInformation(k.Output);

                return k.ExitType == CommandLineExitTypes.Ok;
            }
            catch (Exception ex)
            {
                log.LogCritical(ex.ToString());

                return false;
            }
        }

        public bool RestoreDatabase(string dbName, string backupFullPath)
        {
            // remove database
            if (DatabaseExists(dbName))
                if (!DeleteDatabase(dbName))
                    return false;

            // create database
            if (!CreateDatabase(dbName)) return false;

            // restore database
            try
            {
                var args = new StringBuilder();
                args.Append($" --host {MasterConnectionString.Host}");
                args.Append($" --port {MasterConnectionString.Port}");
                args.Append($" --username {MasterConnectionString.Username}");
                args.Append(" --verbose");
                args.Append($" --dbname \"{dbName}\"");
                args.Append($" \"{backupFullPath}\"");
                Environment.SetEnvironmentVariable("PGPASSWORD", MasterConnectionString.Password);
                var cmd = new Cmd();
                var k = cmd.Run(PgRestorePath, args.ToString(), true);
                log.LogInformation(k.Output);

                return k.ExitType == CommandLineExitTypes.Ok;
            }
            catch (Exception ex)
            {
                log.LogCritical(ex.ToString());

                return false;
            }
        }

        public bool DeleteDatabase(string dbName)
        {
            // close connections to database
            CloseConnections(dbName);

            // delete database
            try
            {
                var cmd = new Cmd();
                var args = new StringBuilder();

                //drop database
                args.Clear();
                args.Append($" --host {MasterConnectionString.Host}");
                args.Append($" --port {MasterConnectionString.Port}");
                args.Append($" --username {MasterConnectionString.Username}");
                args.Append(" --dbname postgres");
                args.Append($" -c \"drop database \"{dbName}\";\"");
                Environment.SetEnvironmentVariable("PGPASSWORD", MasterConnectionString.Password);
                var res3 = cmd.Run(PSqlPath, args.ToString(), true);
                log.LogInformation(res3.Output);

                return res3.ExitType == CommandLineExitTypes.Ok;
            }
            catch (Exception ex)
            {
                log.LogCritical(ex.ToString());

                return false;
            }
        }

        public bool CloseConnections(string dbName)
        {
            try
            {
                var args = new StringBuilder();
                var cmd = new Cmd();
                //view all connections
                args.Append($" --host {MasterConnectionString.Host}");
                args.Append($" --port {MasterConnectionString.Port}");
                args.Append($" --username {MasterConnectionString.Username}");
                args.Append(" --dbname postgres");
                args.Append($" -c \"SELECT * FROM pg_stat_activity WHERE datname = '{dbName}';\"");
                Environment.SetEnvironmentVariable("PGPASSWORD", MasterConnectionString.Password);
                var res1 = cmd.Run(PSqlPath, args.ToString(), true);
                log.LogInformation(res1.Output);

                //close all connections
                args.Clear();
                args.Append($" --host {MasterConnectionString.Host}");
                args.Append($" --port {MasterConnectionString.Port}");
                args.Append($" --username {MasterConnectionString.Username}");
                args.Append(" --dbname postgres");

                args.Append(
                    $" -c \"SELECT pg_terminate_backend (pg_stat_activity.pid) FROM pg_stat_activity WHERE pg_stat_activity.datname = '{dbName}';\"");

                Environment.SetEnvironmentVariable("PGPASSWORD", MasterConnectionString.Password);
                var res2 = cmd.Run(PSqlPath, args.ToString(), true);
                log.LogInformation(res2.Output);

                return res2.ExitType == CommandLineExitTypes.Ok;
            }
            catch (Exception ex)
            {
                log.LogCritical(ex.ToString());

                return false;
            }
        }

        public string GetConnectionString(string dbName)
        {
            return new NpgsqlConnectionStringBuilder(MasterConnectionString.ConnectionString)
            {
                Database = dbName
            }.ConnectionString;
        }

        public IDbConnection EmptyConnectionFactory()
        {
            return new NpgsqlConnection();
        }

        public Func<IDbConnection> CreateConnectionFactory(Func<string> connectionStringFactory)
        {
            return () => new NpgsqlConnection(connectionStringFactory());
        }

        public Compiler CreateCompiler()
        {
            return new PostgresCompiler();
        }

        public List<string> GetAllDatabaseNames()
        {
            var query = @"
SELECT datname FROM pg_database
WHERE datistemplate = false;";

            using (var connection = new NpgsqlConnection(MasterConnectionString.ConnectionString))
            {
                return connection.Query<string>(query).AsList();
            }
        }
    }
}