using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noord.HollandsArchief.Pre.Ingest.WorkerService.Entities.CommandKey
{
    public abstract class AbstractKey : IKey
    {
        public ValidationActionType Name { get; set; }

        public bool Equals(IKey other)
        {
            return other != null && other.Name == this.Name;
        }

        public override int GetHashCode()
        {
            return this.Name.ToString().GetHashCode();
        }
    }
}
