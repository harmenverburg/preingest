using Noord.HollandsArchief.Pre.Ingest.WorkerService.Handler.Command;
using Noord.HollandsArchief.Pre.Ingest.WorkerService.Entities.EventHub;
using Noord.HollandsArchief.Pre.Ingest.WorkerService.Entities.CommandKey;

using System;
using System.Linq;
using System.Net.Http;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace Noord.HollandsArchief.Pre.Ingest.WorkerService.Handler.Creator
{
    public class PreingestCommandCreator : CommandCreator
    {
        private readonly IDictionary<IKey, IPreingestCommand> _executionCommand = null;
        private Uri _webapiUrl = null;
        public PreingestCommandCreator(Uri webapiUrl)
        {
            _webapiUrl = webapiUrl;

            _executionCommand = new Dictionary<IKey, IPreingestCommand>();
            _executionCommand.Add(new DefaultKey(ValidationActionType.ContainerChecksumHandler), new CalculateChecksumCommand(webapiUrl));
            _executionCommand.Add(new DefaultKey(ValidationActionType.ExportingHandler), new DroidCsvExportingCommand(webapiUrl));
            _executionCommand.Add(new DefaultKey(ValidationActionType.ReportingPdfHandler), new DroidPdfReportingCommand(webapiUrl));
            _executionCommand.Add(new DefaultKey(ValidationActionType.ReportingDroidXmlHandler), new DroidXmlReportingCommand(webapiUrl));
            _executionCommand.Add(new DefaultKey(ValidationActionType.ReportingPlanetsXmlHandler), new DroidPlanetsReportingCommand(webapiUrl));
            _executionCommand.Add(new DefaultKey(ValidationActionType.ProfilesHandler), new DroidProfilingCommand(webapiUrl));
            _executionCommand.Add(new DefaultKey(ValidationActionType.EncodingHandler), new EncodingCheckCommand(webapiUrl));
            _executionCommand.Add(new DefaultKey(ValidationActionType.UnpackTarHandler), new ExpandArchiveCommand(webapiUrl));
            _executionCommand.Add(new DefaultKey(ValidationActionType.MetadataValidationHandler), new MetadataSchemaCheckCommand(webapiUrl));
            _executionCommand.Add(new DefaultKey(ValidationActionType.NamingValidationHandler), new NamingCheckCommand(webapiUrl));
            _executionCommand.Add(new DefaultKey(ValidationActionType.GreenListHandler), new NhaGreenlistCheckCommand(webapiUrl));
            _executionCommand.Add(new DefaultKey(ValidationActionType.ExcelCreatorHandler), new PreingestExcelReportingCommand(webapiUrl));
            _executionCommand.Add(new DefaultKey(ValidationActionType.ScanVirusValidationHandler), new ScanVirusCommand(webapiUrl));
            _executionCommand.Add(new DefaultKey(ValidationActionType.SidecarValidationHandler), new SidecarCheckCommand(webapiUrl));
            _executionCommand.Add(new DefaultKey(ValidationActionType.SipCreatorHandler), new SipCreateCommand(webapiUrl));
            _executionCommand.Add(new DefaultKey(ValidationActionType.TransformationHandler), new XipCreateCommand(webapiUrl));
        }

        public override IPreingestCommand FactoryMethod(Guid guid, dynamic data)
        {
            if (data == null)
                return null;

            Plan[] plans = data.scheduledPlan == null ? null : JsonConvert.DeserializeObject<Plan[]>(data.scheduledPlan.ToString());
            Settings settings = data.settings == null ? null : JsonConvert.DeserializeObject<Settings>(data.settings.ToString());
            PreingestAction[] actions = data.preingest == null ? null : JsonConvert.DeserializeObject<PreingestAction[]>(data.preingest.ToString());

            if (plans == null)
                return null;

            Queue<Plan> queue = new Queue<Plan>(plans);

            Plan next = null;
            Plan previous = null;
            while(queue.Peek() != null)
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
                        return null;

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
                return null;
            }

            if (previous == null && next != null)
            {
                IKey key = new DefaultKey(next.ActionName);
                if (!this._executionCommand.ContainsKey(key))
                    return null;

                IPreingestCommand command = this._executionCommand[key];
                if (command != null)
                {
                    command.CurrentSettings = settings;
                    command.CurrentSessionId = guid;
                }
                return command;
            }

            if (previous != null && next != null)
            {
                if (actions == null)
                    return null;

                var action = actions.Where(item => item.Name == previous.ActionName.ToString()).FirstOrDefault();
                if (action == null)
                    return null;


                switch(action.ActionStatus)
                {
                    case "Error":
                        if(previous.ContinueOnError)
                        {
                            IKey key = new DefaultKey(next.ActionName);
                            if (!this._executionCommand.ContainsKey(key))
                                return null;

                            IPreingestCommand command = this._executionCommand[key];
                            if (command != null)
                            {
                                command.CurrentSettings = settings;
                                command.CurrentSessionId = guid;
                            }
                            return command;
                        }
                        break;
                    case "Failed":
                        if (previous.ContinueOnFailed)
                        {
                            IKey key = new DefaultKey( next.ActionName);
                            if (!this._executionCommand.ContainsKey(key))
                                return null;

                            IPreingestCommand command = this._executionCommand[key];
                            if (command != null)
                            {
                                command.CurrentSettings = settings;
                                command.CurrentSessionId = guid;
                            }                            
                            return command;
                        }
                        break;
                    case "Success":
                        {
                            IKey key = new DefaultKey(next.ActionName);
                            if (!this._executionCommand.ContainsKey(key))
                                return null;

                            IPreingestCommand command = this._executionCommand[key];
                            if (command != null)
                            {
                                command.CurrentSettings = settings;
                                command.CurrentSessionId = guid;
                            }
                            return command;
                        }
                        break;
                    default:
                        return null;
                }
            }

            return null;
        }
    }
}
