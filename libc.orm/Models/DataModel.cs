using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using libc.orm.Models.Interfaces;
using NodaTime;
namespace libc.orm.Models {
    public abstract class DataModel : IDataGuid {
        protected DataModel() {
            Id = Guid.NewGuid();
            var now = SystemClock.Instance.GetCurrentInstant().ToUnixTimeTicks();
            CreateUtc = now;
            UpdateUtc = now;
        }
        public string Description { get; set; }
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; set; }
        public long CreateUtc { get; set; }
        public long UpdateUtc { get; set; }
    }
}