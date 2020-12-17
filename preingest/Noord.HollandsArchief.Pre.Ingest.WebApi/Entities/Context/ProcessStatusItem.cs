using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Context
{
    [Table("Status")]
    public class ProcessStatusItem
    {
        [Key]
        [Column("StatusId")]
        public Guid StatusId { get; set; }

        public Guid ProcessId { get; set; }
        public PreIngestSession Session { get; set; }

        [Column("Name")]
        public String Name { get; set; }

        [Column("Creation")]
        public DateTime Creation { get; set; }

        [ForeignKey("StatusId")]
        public ICollection<StatusMessageItem> Messages { get; set; }
    }

}
