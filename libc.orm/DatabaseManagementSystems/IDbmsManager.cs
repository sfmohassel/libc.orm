using System;
using System.Collections.Generic;
using System.Data;
using SqlKata.Compilers;

namespace libc.orm.DatabaseManagementSystems
{
    public interface IDbmsManager
    {
        Dbms Dbms { get; }

        bool DatabaseExists(string dbName);

        bool CreateDatabase(string dbName);

        bool BackupDatabase(string dbName, string backupFullPath);

        bool RestoreDatabase(string dbName, string backupFullPath);

        bool DeleteDatabase(string dbName);

        bool CloseConnections(string dbName);

        string GetConnectionString(string dbName);

        IDbConnection EmptyConnectionFactory();

        Func<IDbConnection> CreateConnectionFactory(Func<string> connectionStringFactory);

        Compiler CreateCompiler();

        List<string> GetAllDatabaseNames();
    }
}