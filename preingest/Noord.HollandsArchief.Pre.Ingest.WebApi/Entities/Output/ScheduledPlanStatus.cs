using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Context;

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Output
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ExecutionStatus
    {
        Done = 0,
        Pending = 1,
        Executing = 2
    }

    public class ExecutionPlanState : ExecutionPlan
    {
        public ExecutionStatus Status { get; set; }
    }

    public class ScheduledPlanStatusHandler
    {
        private List<ExecutionPlanState> _executionPlan = null;

        public ScheduledPlanStatusHandler(List<ExecutionPlan> plan, IEnumerable<QueryResultAction> actions)
        {
            //to do calculation
            _executionPlan = new List<ExecutionPlanState>();
        }

        public ExecutionPlanState[] GetExecutionPlan()
        {
            return _executionPlan.ToArray();
        }
    }
}
