using System.Collections.Generic;
using System.Linq;
using libc.orm.DatabaseConnection;
using libc.orm.DatabaseManagementSystems;
using libc.orm.DatabaseMigration.DdlMigration;
using libc.orm.DatabaseMigration.DdlProcessing;
using Microsoft.Extensions.Logging;
namespace libc.orm.DatabaseMigration.DdlMigrationRunning {
    public class MigrationRunnerOptions {
        private static readonly Dictionary<Dbms, string> journalSchema =
            new Dictionary<Dbms, string> {
                {
                    Dbms.Sqlite, @"
create table if not exists [{0}] ( 
    MigrationVersion integer constraint [pk_{0}_SchemaVersion] primary key not null, 
    MigrationName text not null, 
    CreateUtc integer not null 
)
"
                }, {
                    Dbms.MySql, @"
create table if not exists `{0}` (
    `MigrationVersion` int not null,
    `MigrationName` varchar(400) not null,
    `CreateUtc` bigint not null,
    primary key (`MigrationVersion`),
    index `ix_{0}_CreateUtc` (`CreateUtc` ASC)
);
"
                }, {
                    Dbms.SqlServer, @"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[{0}]') AND type in (N'U'))
BEGIN
create table [{0}] (
        [MigrationVersion] [int] NOT NULL,
        [MigrationName] [varchar](400) NOT NULL,
        [CreateUtc] [bigint] NOT NULL,
        CONSTRAINT [pk_{0}_MigrationVersion] PRIMARY KEY CLUSTERED ( [MigrationVersion] ASC ) 
        WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY]
END
"
                }, {
                    Dbms.Postgres, @"
create table if not exists ""{0}"" (
    ""MigrationVersion"" int not null,
    ""MigrationName"" varchar(400) not null,
    ""CreateUtc"" bigint not null,
    primary key (""MigrationVersion"")
)
"
                }
            };
        private static Dbms[] supportedDbmsList;
        public MigrationRunnerOptions(string nameInLogs, Dbms dbms, IEnumerable<IMigration> migrations, ILogger logger,
            MigrationTransactionMode transactionMode, Database db, IProcessor processor,
            string journalTableName = "schema_journal") {
            NameInLogs = nameInLogs;
            Dbms = dbms;
            Migrations = new List<IMigration>(migrations);
            Logger = logger;
            TransactionMode = transactionMode;
            JournalTableName = journalTableName;
            Db = db;
            Processor = processor;
        }
        public static Dbms[] SupportedDbmsList => supportedDbmsList ?? (supportedDbmsList = journalSchema.Keys.ToArray());
        public string NameInLogs { get; }
        public Dbms Dbms { get; }
        public List<IMigration> Migrations { get; }
        public ILogger Logger { get; }
        public MigrationTransactionMode TransactionMode { get; }
        public string JournalTableName { get; }
        public Database Db { get; }
        public IProcessor Processor { get; }
        public string GetJournalTableQuery() {
            return journalSchema[Dbms];
        }
        public MigrationContext CreateMigrationContext() {
            return new MigrationContext(Processor, Processor.Options.ConnectionString);
        }
    }
}