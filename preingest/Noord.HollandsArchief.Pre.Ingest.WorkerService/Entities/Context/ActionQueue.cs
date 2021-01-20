using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noord.HollandsArchief.Pre.Ingest.WorkerService.Entities.Context
{
    [Table("Queue")]
    public class ActionQueue
    {
        [Key]
        [Column("SessionId")]
        public Guid SessionId { get; set; }
        [Column("ExecutionOrder")]
        public Int32 ExecutionOrder { get; set; }
        [Column("IsExecuted")]
        public String ActionName { get; set; }
        [Column("IsExecuted")]
        public bool IsExecuted { get; set; }
        [Column("IsCompleted")]
        public bool IsCompleted { get; set; }
        [Column("ContinueOnError")]
        public bool ContinueOnError { get; set; }
        [Column("ContinueOnFailed")]
        public bool ContinueOnFailed { get; set; }
    }
}
