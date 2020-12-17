using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Model;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Handlers
{
    public interface PreIngest
    {
        void Execute();
        Guid SessionGuid { get; }
        void SetSessionGuid(Guid guid);
        ILogger Logger { get; set; }
    }
}
