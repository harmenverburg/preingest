using Noord.HollandsArchief.Pre.Ingest.WorkerService.Handler.Command;
using Noord.HollandsArchief.Pre.Ingest.WorkerService.Entities.EventHub;
using Noord.HollandsArchief.Pre.Ingest.WorkerService.Entities.CommandKey;

using System;
using System.Linq;
using System.Collections.Generic;

using Newtonsoft.Json;

using Microsoft.Extensions.Logging;

namespace Noord.HollandsArchief.Pre.Ingest.WorkerService.Handler.Creator
{
    public class PreingestCommandCreator : CommandCreator
    {
        private readonly IDictionary<IKey, IPreingestCommand> _executionCommand = null;
        private Uri _webapiUrl = null;
        public PreingestCommandCreator(ILogger<PreingestEventHubHandler> logger, Uri webapiUrl) : base(logger)
        {
            _webapiUrl = webapiUrl;
            _executionCommand = new Dictionary<IKey, IPreingestCommand>();
            _executionCommand.Add(new DefaultKey(ValidationActionType.ContainerChecksumHandler), new CalculateChecksumCommand(logger, webapiUrl));
            _executionCommand.Add(new DefaultKey(ValidationActionType.ExportingHandler), new DroidCsvExportingCommand(logger, webapiUrl));
            _executionCommand.Add(new DefaultKey(ValidationActionType.ReportingPdfHandler), new DroidPdfReportingCommand(logger,webapiUrl));
            _executionCommand.Add(new DefaultKey(ValidationActionType.ReportingDroidXmlHandler), new DroidXmlReportingCommand(logger, webapiUrl));
            _executionCommand.Add(new DefaultKey(ValidationActionType.ReportingPlanetsXmlHandler), new DroidPlanetsReportingCommand(logger, webapiUrl));
            _executionCommand.Add(new DefaultKey(ValidationActionType.ProfilesHandler), new DroidProfilingCommand(logger, webapiUrl));
            _executionCommand.Add(new DefaultKey(ValidationActionType.EncodingHandler), new EncodingCheckCommand(logger, webapiUrl));
            _executionCommand.Add(new DefaultKey(ValidationActionType.UnpackTarHandler), new ExpandArchiveCommand(logger, webapiUrl));
            _executionCommand.Add(new DefaultKey(ValidationActionType.MetadataValidationHandler), new MetadataSchemaCheckCommand(logger, webapiUrl));
            _executionCommand.Add(new DefaultKey(ValidationActionType.NamingValidationHandler), new NamingCheckCommand(logger, webapiUrl));
            _executionCommand.Add(new DefaultKey(ValidationActionType.GreenListHandler), new NhaGreenlistCheckCommand(logger, webapiUrl));
            _executionCommand.Add(new DefaultKey(ValidationActionType.ExcelCreatorHandler), new PreingestExcelReportingCommand(logger, webapiUrl));
            _executionCommand.Add(new DefaultKey(ValidationActionType.ScanVirusValidationHandler), new ScanVirusCommand(logger, webapiUrl));
            _executionCommand.Add(new DefaultKey(ValidationActionType.SidecarValidationHandler), new SidecarCheckCommand(logger, webapiUrl));
            _executionCommand.Add(new DefaultKey(ValidationActionType.SipCreatorHandler), new SipCreateCommand(logger, webapiUrl));
            _executionCommand.Add(new DefaultKey(ValidationActionType.TransformationHandler), new XipCreateCommand(logger, webapiUrl));
        }

        public override IPreingestCommand FactoryMethod(Guid guid, dynamic data)
        {
            if (data == null)
            {
                Logger.LogInformation("FactoryMethod : {0}.", "Incoming data is empty");
                return null;
            }

            Plan[] plans = data.scheduledPlan == null ? null : JsonConvert.DeserializeObject<Plan[]>(data.scheduledPlan.ToString());
            PreingestAction[] actions = data.preingest == null ? null : JsonConvert.DeserializeObject<PreingestAction[]>(data.preingest.ToString());

            if (plans == null)
            {
                Logger.LogInformation("FactoryMethod : {0}.", "No scheduled plan found, just exit");
                return null;
            }

            Queue<Plan> queue = new Queue<Plan>(plans);

            Plan next = null;
            Plan previous = null;
            while(queue.Count > 0)
            {
                Plan item = queue.Peek();
                //found one running (should not), just break it
                if (item.Status == ExecutionStatus.Executing)
                    break;

                //found one done (previous), peek if null done else next
                if (item.Status == ExecutionStatus.Done)
                {
                    previous = queue.Dequeue();
                    Plan peek = queue.Count > 0 ? queue.Peek() : null;

                    if (peek == null) //done just exit
                    {
                        Logger.LogInformation("FactoryMethod : {0}.", "Peek queue. See nothing. Probably done with the plan");
                        return null;
                    }

                    if (peek.Status == ExecutionStatus.Done)                    
                        continue;                    

                    if (peek.Status == ExecutionStatus.Pending)
                    {
                        next = queue.Dequeue();
                        break;
                    }
                }
                //found one pending, just fire next
                if (item.Status == ExecutionStatus.Pending)
                {
                    next = queue.Dequeue();
                    break;
                }
            }

            if (next == null)
            {
                Logger.LogInformation("FactoryMethod : {0}.", "Exit the factory method, no next task planned");
                return null;
            }

            if (previous == null && next != null)
            {
                IKey key = new DefaultKey(next.ActionName);
                if (!this._executionCommand.ContainsKey(key))
                {
                    Logger.LogInformation("FactoryMethod : No key found in dictionary with {0}.", key);
                    return null;
                }

                IPreingestCommand command = this._executionCommand[key];
                return command;
            }

            if (previous != null && next != null)
            {
                if (actions == null)
                {
                    Logger.LogInformation("FactoryMethod : Plan described previous and next action, but there is no actions list returned. Hmmm....");
                    return null;
                }

                var action = actions.Where(item => item.Name == previous.ActionName.ToString()).FirstOrDefault();
                if (action == null)
                {
                    Logger.LogInformation("FactoryMethod : No action found in the list with the name {0}.", previous.ActionName.ToString());
                    return null;
                }

                IKey key = new DefaultKey(next.ActionName);
                if (!this._executionCommand.ContainsKey(key))
                {
                    Logger.LogInformation("FactoryMethod : No key found in dictionary with {0}.", key);
                    return null;
                }

                bool isOk2Run = false;
                switch(action.ActionStatus)
                {
                    case "Error":
                        if(previous.ContinueOnError)
                        {
                            isOk2Run = true;
                        }
                        break;
                    case "Failed":
                        if (previous.ContinueOnFailed)
                        {
                            isOk2Run = true;
                        }
                        break;
                    case "Success":
                        {
                            isOk2Run = true;
                        }
                        break;
                    default:
                        isOk2Run = false;
                        break;
                }

                if (isOk2Run)
                {
                    IPreingestCommand command = this._executionCommand[key];
                    return command;
                }
            }

            return null;
        }
    }
}
