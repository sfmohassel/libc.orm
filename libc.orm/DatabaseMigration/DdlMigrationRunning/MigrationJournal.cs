using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace libc.orm.DatabaseMigration.DdlMigrationRunning {
    public class MigrationJournal {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int MigrationVersion { get; set; }
        public string MigrationName { get; set; }
        public long CreateUtc { get; set; }
    }
}