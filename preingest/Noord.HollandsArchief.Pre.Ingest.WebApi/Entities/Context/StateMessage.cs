using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Context
{
    [Table("Messages")]
    public class StateMessage
    {
        [Key]
        [Column("MessageId")]
        public Guid MessageId { get; set; }

        public Guid StatusId { get; set; }
        public ActionStates Status { get; set; }

        [Column("Creation")]
        public DateTime Creation { get; set; }

        [Column("Description")]
        public String Description { get; set; }
    }
}
