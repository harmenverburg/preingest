using Noord.HollandsArchief.Pre.Ingest.WorkerService.Handler.Command;
using Noord.HollandsArchief.Pre.Ingest.WorkerService.Entities.CommandKey;

using System;
using System.Linq;
using System.Net.Http;
using System.Collections.Generic;

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

        public override IPreingestCommand FactoryMethod(Guid guid, HttpClient client)
        {
            bool mayContinue = false;
            String nextActionName = String.Empty;
           
            //ToDO determine next scheduled action/task through getcollections


          
            //if (mayContinue && !String.IsNullOrEmpty(nextActionName))
            //{
            //    IKey key = new DefaultKey(nextActionName);
            //    return this._executionCommand.First(item => item.Key == key).Value;
            //}

            return null;
        }
    }
}
