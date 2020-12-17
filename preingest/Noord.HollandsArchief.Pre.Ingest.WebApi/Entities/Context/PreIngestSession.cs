using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Context
{
    [Table("Sessions")]
    public class PreIngestSession
    {
        [Key]
        [Column("ProcessId")]
        public Guid ProcessId { get; set; }

        [Column("FolderSessionId")]
        public Guid FolderSessionId { get; set; }

        [Column("Name")]
        public String Name { get; set; }

        [Column("Description")]
        public String Description { get; set; }

        [Column("Creation")]
        public DateTime Creation { get; set; }

        [Column("ResultFiles")]
        public String ResultFiles { get; set; }

        [ForeignKey("ProcessId")]
        public ICollection<ProcessStatusItem> Status { get; set; }
    }
}
