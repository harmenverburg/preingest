using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Handlers
{
    public class ExcelCreatorHandler : AbstractPreingestHandler
    {
        public ExcelCreatorHandler(AppSettings settings) : base(settings) { }

        public override void Execute()
        {
            base.Execute();

            throw new NotImplementedException();
        }
    }
}
