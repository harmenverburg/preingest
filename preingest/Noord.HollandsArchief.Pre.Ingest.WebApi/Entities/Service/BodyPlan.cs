using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Service
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ValidationActionType
    {
        ContainerChecksumHandler,
        ExportingHandler,
        ReportingPdfHandler,
        ReportingDroidXmlHandler,
        ReportingPlanetsXmlHandler,
        ProfilesHandler,
        EncodingHandler,
        UnpackTarHandler,
        MetadataValidationHandler,
        NamingValidationHandler,
        GreenListHandler,
        ExcelCreatorHandler,
        ScanVirusValidationHandler,
        SidecarValidationHandler,
        SipCreatorHandler,
        TransformationHandler
    }


    public class BodyPlan
    {
        public ValidationActionType ActionName { get; set; }
        public bool ContinueOnFailed { get; set; }
        public bool ContinueOnError { get; set; }
    }
}
