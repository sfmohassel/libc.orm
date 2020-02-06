#region License

// Copyright (c) 2007-2018, Sean Chambers <schambers80@gmail.com>
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using JetBrains.Annotations;
using libc.orm.DatabaseMigration.Abstractions;
using Microsoft.Extensions.Logging;
namespace libc.orm.DatabaseMigration.DdlProcessing {
    public abstract class GenericProcessorBase : ProcessorBase {
        [NotNull]
        private readonly Lazy<DbProviderFactory> _dbProviderFactory;
        [NotNull]
        private readonly Lazy<IDbConnection> _lazyConnection;
        [CanBeNull]
        private IDbConnection _connection;
        private bool _disposed;
        protected GenericProcessorBase([NotNull] Func<DbProviderFactory> factoryAccessor,
            [NotNull] IMigrationGenerator generator,
            [NotNull] ILogger logger,
            [NotNull] ProcessorOptions options)
            : base(generator, logger, options) {
            _dbProviderFactory = new Lazy<DbProviderFactory>(factoryAccessor.Invoke);
            _lazyConnection = new Lazy<IDbConnection>(
                () => {
                    var connection = DbProviderFactory.CreateConnection();
                    Debug.Assert(connection != null, nameof(Connection) + " != null");
                    connection.ConnectionString = options.ConnectionString;
                    connection.Open();
                    return connection;
                });
        }
        public IDbConnection Connection {
            get => _connection ?? _lazyConnection.Value;
            protected set => _connection = value;
        }
        [CanBeNull]
        public IDbTransaction Transaction { get; private set; }
        [NotNull]
        protected DbProviderFactory DbProviderFactory => _dbProviderFactory.Value;
        protected virtual void EnsureConnectionIsOpen() {
            if (Connection != null && Connection.State != ConnectionState.Open) Connection.Open();
        }
        protected virtual void EnsureConnectionIsClosed() {
            if ((_connection != null || _lazyConnection.IsValueCreated && Connection != null) &&
                Connection.State != ConnectionState.Closed) Connection.Close();
        }
        public override void BeginTransaction() {
            if (Transaction != null) return;
            EnsureConnectionIsOpen();
            Logger.LogSay("Beginning Transaction");
            Transaction = Connection?.BeginTransaction();
        }
        public override void RollbackTransaction() {
            if (Transaction == null) return;
            Logger.LogSay("Rolling back transaction");
            Transaction.Rollback();
            Transaction.Dispose();
            WasCommitted = true;
            Transaction = null;
        }
        public override void CommitTransaction() {
            if (Transaction == null) return;
            Logger.LogSay("Committing Transaction");
            Transaction.Commit();
            Transaction.Dispose();
            WasCommitted = true;
            Transaction = null;
        }
        protected override void Dispose(bool isDisposing) {
            if (!isDisposing || _disposed)
                return;
            _disposed = true;
            RollbackTransaction();
            EnsureConnectionIsClosed();
            if (_connection != null || _lazyConnection.IsValueCreated && Connection != null) Connection.Dispose();
        }
        protected virtual IDbCommand CreateCommand(string commandText) {
            return CreateCommand(commandText, Connection, Transaction);
        }
        protected virtual IDbCommand CreateCommand(string commandText, IDbConnection connection,
            IDbTransaction transaction) {
            IDbCommand result = DbProviderFactory.CreateCommand();
            Debug.Assert(result != null, nameof(result) + " != null");
            result.Connection = connection;
            if (transaction != null)
                result.Transaction = transaction;
            result.CommandText = commandText;
            if (Options.Timeout != null) result.CommandTimeout = (int) Options.Timeout.Value.TotalSeconds;
            return result;
        }
    }
}