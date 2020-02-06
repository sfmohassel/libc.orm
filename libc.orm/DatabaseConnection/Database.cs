using System;
using System.Data;
using SqlKata;
using SqlKata.Compilers;
namespace libc.orm.DatabaseConnection {
    public class Database {
        private readonly Func<IDbConnection> connectionFactory;
        private readonly Action<SqlResult> logger;
        public Database(Compiler compiler, Func<IDbConnection> connectionFactory, Action<SqlResult> logger) {
            Compiler = compiler;
            this.connectionFactory = connectionFactory;
            this.logger = logger;
        }
        public Compiler Compiler { get; }
        public DbConn Connect() {
            return new DbConn(connectionFactory(), Compiler, logger);
        }
    }
}