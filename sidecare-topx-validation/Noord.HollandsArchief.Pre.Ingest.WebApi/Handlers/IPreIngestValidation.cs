using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Handlers
{
    public interface IPreIngestValidation
    {
        void Execute();
        Guid SessionGuid { get; }
        void SetSessionGuid(Guid guid);
        ILogger Logger { get; set; }
    }
}
