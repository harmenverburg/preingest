using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using System;
using System.Linq;
using System.Collections.Generic;

using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Event;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Output
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ContainerStatus
    {
        Success = 4,
        Failed = 3,
        Error = 2,
        Running = 1,
        New = 0,
        None = -1
    }

    public class ContainerOverallStatusHandler
    {
        private readonly ContainerStatus _status = ContainerStatus.None;
        public ContainerOverallStatusHandler(IEnumerable<QueryResultAction> actions)
        {
            if (_status == ContainerStatus.None && actions.ToList().Count == 0)
            {
                _status = ContainerStatus.New;
            }

            if (_status == ContainerStatus.None)
            {
                var currentAvailableStatus = actions.Select(item
                    => (Enum.Parse(typeof(PreingestActionResults), item.ActionStatus, true) == null)
                    ? PreingestActionResults.None
                    : (PreingestActionResults)Enum.Parse(typeof(PreingestActionResults), item.ActionStatus, true)
                    ).Distinct().OrderBy(item => item);

                PreingestActionResults result = currentAvailableStatus.FirstOrDefault();
                switch(result)
                {
                    case PreingestActionResults.Executing:
                    case PreingestActionResults.None:
                        _status = ContainerStatus.Running;
                        break;
                    case PreingestActionResults.Failed:
                        _status = ContainerStatus.Failed;
                        break;
                    case PreingestActionResults.Error:
                        _status = ContainerStatus.Error;
                        break;
                    case PreingestActionResults.Success:
                        _status = ContainerStatus.Success;
                        break;
                    default:
                        _status = ContainerStatus.None;
                        break;
                }
            }
        }

        public ContainerStatus GetContainerStatus()
        {
            return this._status;
        }
    }
   
}
