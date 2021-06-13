using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using libc.orm.DatabaseManagementSystems;
using Microsoft.Extensions.Logging;
using SqlKata.Compilers;

namespace libc.orm.sqlite.Management
{
    public class SqliteManager : IDbmsManager
    {
        private readonly ILogger log;

        public SqliteManager(ILogger log, string dbFullPath)
        {
            this.log = log;
            DbFullPath = dbFullPath;

            Builder = new SQLiteConnectionStringBuilder
            {
                DataSource = DbFullPath,
                BinaryGUID = false
            };
        }

        public SQLiteConnectionStringBuilder Builder { get; }
        public string DbFullPath { get; }
        public Dbms Dbms => Dbms.Sqlite;

        public bool DatabaseExists(string dbName)
        {
            try
            {
                var exists = File.Exists(DbFullPath);

                return exists;
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
                using (var source = new SQLiteConnection(Builder.ConnectionString))
                {
                    source.Open();
                    source.Close();
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
                using (var source = new SQLiteConnection(Builder.ConnectionString))
                {
                    source.Open();

                    using (var destination = new SQLiteConnection($"Data Source={backupFullPath}"))
                    {
                        source.BackupDatabase(destination, "main", "main", -1, null, 500);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                log.LogCritical(ex.ToString());

                return false;
            }
        }

        public bool RestoreDatabase(string dbName, string backupFullPath)
        {
            try
            {
                using (var source = new SQLiteConnection($"Data Source={backupFullPath}"))
                {
                    source.Open();

                    using (var destination = new SQLiteConnection(Builder.ConnectionString))
                    {
                        source.BackupDatabase(destination, "main", "main", -1, null, 500);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                log.LogCritical(ex.ToString());

                return false;
            }
        }

        public bool DeleteDatabase(string dbName)
        {
            try
            {
                var path = Path.GetFullPath(Builder.DataSource);

                try
                {
                    if (File.Exists(path)) File.Delete(path);
                }
                catch
                {
                    // ignored
                }

                return true;
            }
            catch (Exception ex)
            {
                log.LogCritical(ex.ToString());

                return false;
            }
        }

        public bool CloseConnections(string dbName)
        {
            return true;
        }

        public string GetConnectionString(string dbName)
        {
            return new SQLiteConnectionStringBuilder(Builder.ConnectionString)
            {
                DataSource = dbName,
                BinaryGUID = false
            }.ConnectionString;
        }

        public IDbConnection EmptyConnectionFactory()
        {
            return new SQLiteConnection();
        }

        public Func<IDbConnection> CreateConnectionFactory(Func<string> connectionStringFactory)
        {
            return () => new SQLiteConnection(connectionStringFactory());
        }

        public Compiler CreateCompiler()
        {
            return new SqliteCompiler();
        }

        public List<string> GetAllDatabaseNames()
        {
            return new List<string>();
        }
    }
}