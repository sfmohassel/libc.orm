using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using libc.orm.DatabaseMigration.Abstractions.Expressions.Base;
using libc.orm.DatabaseMigration.DdlMigration;
using libc.orm.DatabaseMigration.DdlProcessing;
using libc.orm.Internals;
using libc.orm.Models;
using Microsoft.Extensions.Logging;
using NodaTime;
using SqlKata;
namespace libc.orm.DatabaseMigration.DdlMigrationRunning {
    public sealed class MigrationRunner {
        private readonly int currentVersion;
        private readonly MigrationRunnerOptions o;
        public MigrationRunner(MigrationRunnerOptions o) {
            this.o = o;
            // ensure journal table
            ensureJournalTable();
            // read journals
            var journals = readJournalTable();
            // read current version
            currentVersion = getCurrentVersion(journals);
        }
        /// <summary>
        ///     Executes up-migrations to be run up to <see cref="upToVersion" />.
        /// </summary>
        /// <param name="upToVersion"></param>
        /// <returns></returns>
        public DbFluentResult Up(int? upToVersion = null) {
            var migrations = GetUpMigrationsToExecute(upToVersion);
            return Up(migrations);
        }
        /// <summary>
        ///     Executes down-migrations to be run down to <see cref="downToVersion" />
        /// </summary>
        /// <param name="downToVersion"></param>
        /// <returns></returns>
        public DbFluentResult Down(int? downToVersion = null) {
            var migrations = GetDownMigrationsToExecute(downToVersion);
            return Down(migrations);
        }
        private DbFluentResult Up(IEnumerable<IMigration> migrations) {
            var orderedMigrations = migrations.OrderBy(a => a.Version).ToArray();
            return run(orderedMigrations, (migration, context) => migration.GetUpExpressions(context), UpMigrationDone);
        }
        private DbFluentResult Down(IEnumerable<IMigration> migrations) {
            var orderedMigrations = migrations.OrderByDescending(a => a.Version).ToArray();
            return run(orderedMigrations, (migration, context) => migration.GetDownExpressions(context), DownMigrationDone);
        }
        private void UpMigrationDone(IMigration migration) {
            using (var db = o.Db.Connect()) {
                db.Execute(
                    new Query(o.JournalTableName)
                        .AsInsert(new MigrationJournal {
                            MigrationVersion = migration.Version,
                            MigrationName = migration.Name,
                            CreateUtc = SystemClock.Instance.GetCurrentInstant().ToUnixTimeTicks()
                        })
                );
            }
        }
        private void DownMigrationDone(IMigration migration) {
            using (var db = o.Db.Connect()) {
                db.Execute(
                    new Query(o.JournalTableName)
                        .Where(nameof(MigrationJournal.MigrationVersion), migration.Version)
                        .AsDelete()
                );
            }
        }
        private DbFluentResult run(IReadOnlyCollection<IMigration> orderedMigrations,
            Action<IMigration, MigrationContext> getExpressions, Action<IMigration> onMigrationDone) {
            var fres = new DbFluentResult();
            try {
                if (orderedMigrations.Count == 0) {
                    info("No migrations to run.");
                    return fres;
                }
                if (o.TransactionMode == MigrationTransactionMode.NoTransaction)
                    foreach (var migration in orderedMigrations) {
                        // create context
                        var context = o.CreateMigrationContext();
                        // fill context with expressions
                        getExpressions(migration, context);
                        // execute expressions
                        executeExpressions(o.Processor, context.Expressions);
                        // update journal
                        onMigrationDone(migration);
                    }
                else if (o.TransactionMode == MigrationTransactionMode.TransactionPerMigration)
                    foreach (var migration in orderedMigrations) {
                        // create context
                        var context = o.CreateMigrationContext();
                        // fill context with expressions
                        getExpressions(migration, context);
                        // execute expressions
                        try {
                            o.Processor.BeginTransaction();
                            executeExpressions(o.Processor, context.Expressions);
                            o.Processor.CommitTransaction();
                        } catch {
                            o.Processor.RollbackTransaction();
                            throw;
                        }
                        // update journal
                        onMigrationDone(migration);
                    }
                else
                    throw new ArgumentOutOfRangeException();
                return fres;
            } catch (Exception ex) {
                error(
                    $"Error while executing migrations {orderedMigrations.Select(a => a.Version).ConcatString(",", "[", "]")}:");
                error(ex.ToString());
                return fres.AddError(ex.Message);
            }
        }
        private static void executeExpressions(IProcessor processor, IEnumerable<IMigrationExpression> expressions) {
            foreach (var expression in expressions) expression.ExecuteWith(processor);
        }
        /// <summary>
        ///     Returns up-migrations to be run up to <see cref="upToVersion" />.
        /// </summary>
        /// <param name="upToVersion">set to null if you want up to latest</param>
        /// <returns></returns>
        public List<IMigration> GetUpMigrationsToExecute(int? upToVersion = null) {
            if (o.Migrations == null || o.Migrations.Count == 0) return new List<IMigration>();
            int d;
            if (upToVersion == null) {
                var max = o.Migrations.OrderByDescending(a => a.Version).FirstOrDefault();
                d = max?.Version ?? throw new Exception("impossible GetUpMigrationsToExecute");
            } else {
                d = upToVersion.Value;
            }
            return o.Migrations
                .Where(a => a.Version > currentVersion && a.Version <= d)
                .OrderBy(a => a.Version)
                .ToList();
        }
        /// <summary>
        ///     Returns down-migrations to be run down to <see cref="downToVersion" />
        /// </summary>
        /// <param name="downToVersion">set null if you want down to the first version</param>
        /// <returns></returns>
        public List<IMigration> GetDownMigrationsToExecute(int? downToVersion = null) {
            if (o.Migrations == null || o.Migrations.Count == 0) return new List<IMigration>();
            int d;
            if (downToVersion == null) {
                var min = o.Migrations.OrderBy(a => a.Version).FirstOrDefault();
                d = min?.Version ?? throw new Exception("impossible GetDownMigrationsToExecute");
            } else {
                d = downToVersion.Value;
            }
            return o.Migrations
                .Where(a => a.Version <= currentVersion && a.Version >= d)
                .OrderByDescending(a => a.Version)
                .ToList();
        }
        private int getCurrentVersion(IEnumerable<MigrationJournal> journals) {
            var k = journals.OrderByDescending(a => a.MigrationVersion).FirstOrDefault();
            var res = k?.MigrationVersion ?? int.MinValue;
            info($"Journal table \"{o.JournalTableName}\" is read. Current version is {res}");
            return res;
        }
        private List<MigrationJournal> readJournalTable() {
            info($"Trying to read journal table \"{o.JournalTableName}\"...");
            try {
                List<MigrationJournal> journals;
                using (var db = o.Db.Connect()) {
                    journals = db.Query<MigrationJournal>(new Query(o.JournalTableName)).AsList();
                }
                return journals;
            } catch (Exception ex) {
                error($"Error in reading journal table \"{o.JournalTableName}\":");
                error(ex.ToString());
                return null;
            }
        }
        private void ensureJournalTable() {
            info($"Trying to ensure that journal table \"{o.JournalTableName}\" exists...");
            try {
                var query = o.GetJournalTableQuery();
                using (var db = o.Db.Connect()) {
                    db.Execute(string.Format(query, o.JournalTableName));
                }
                info($"Journal table \"{o.JournalTableName}\" is up.");
            } catch (Exception ex) {
                error($"Error in ensuring journal table \"{o.JournalTableName}\":");
                error(ex.ToString());
            }
        }
        private void info(string s) {
            o.Logger?.LogInformation($"{o.NameInLogs} - {s}");
        }
        private void error(string s) {
            o.Logger?.LogCritical($"{o.NameInLogs} - {s}");
        }
    }
}