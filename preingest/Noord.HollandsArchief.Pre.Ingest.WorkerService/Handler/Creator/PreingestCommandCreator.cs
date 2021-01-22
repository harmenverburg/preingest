using Noord.HollandsArchief.Pre.Ingest.WorkerService.Handler.Command;
using Noord.HollandsArchief.Pre.Ingest.WorkerService.Entities.EventHub;
using Noord.HollandsArchief.Pre.Ingest.WorkerService.Entities.CommandKey;

using System;
using System.Linq;
using System.Collections.Generic;
using Noord.HollandsArchief.Pre.Ingest.WorkerService.Model;


namespace Noord.HollandsArchief.Pre.Ingest.WorkerService.Handler.Creator
{
    public class PreingestCommandCreator : CommandCreator
    {
        private readonly IDictionary<IKey, IPreingestCommand> _executionCommand = null;
        public PreingestCommandCreator()
        {
            _executionCommand = new Dictionary<IKey, IPreingestCommand>();
            _executionCommand.Add(new DefaultKey(ValidationActionType.ContainerChecksumHandler), new CalculateChecksumCommand());
            _executionCommand.Add(new DefaultKey(ValidationActionType.ExportingHandler), new DroidCsvExportingCommand());
            _executionCommand.Add(new DefaultKey(ValidationActionType.ReportingPdfHandler), new DroidPdfReportingCommand());
            _executionCommand.Add(new DefaultKey(ValidationActionType.ReportingDroidXmlHandler), new DroidXmlReportingCommand());
            _executionCommand.Add(new DefaultKey(ValidationActionType.ReportingPlanetsXmlHandler), new DroidPlanetsReportingCommand());
            _executionCommand.Add(new DefaultKey(ValidationActionType.ProfilesHandler), new DroidProfilingCommand());
            _executionCommand.Add(new DefaultKey(ValidationActionType.EncodingHandler), new EncodingCheckCommand());
            _executionCommand.Add(new DefaultKey(ValidationActionType.UnpackTarHandler), new ExpandArchiveCommand());
            _executionCommand.Add(new DefaultKey(ValidationActionType.MetadataValidationHandler), new MetadataSchemaCheckCommand());
            _executionCommand.Add(new DefaultKey(ValidationActionType.NamingValidationHandler), new NamingCheckCommand());
            _executionCommand.Add(new DefaultKey(ValidationActionType.GreenListHandler), new NhaGreenlistCheckCommand());
            _executionCommand.Add(new DefaultKey(ValidationActionType.ExcelCreatorHandler), new PreingestExcelReportingCommand());
            _executionCommand.Add(new DefaultKey(ValidationActionType.ScanVirusValidationHandler), new ScanVirusCommand());
            _executionCommand.Add(new DefaultKey(ValidationActionType.SidecarValidationHandler), new SidecarCheckCommand());
            _executionCommand.Add(new DefaultKey(ValidationActionType.SipCreatorHandler), new SipCreateCommand());
            _executionCommand.Add(new DefaultKey(ValidationActionType.TransformationHandler), new XipCreateCommand());
        }

        public override IPreingestCommand FactoryMethod(EventMessage eve)
        {
            bool mayContinue = false;
            String nextActionName = String.Empty;
           
            //ToDO
          
            //if (mayContinue && !String.IsNullOrEmpty(nextActionName))
            //{
            //    IKey key = new DefaultKey(nextActionName);
            //    return this._executionCommand.First(item => item.Key == key).Value;
            //}

            return null;
        }
    }
}
