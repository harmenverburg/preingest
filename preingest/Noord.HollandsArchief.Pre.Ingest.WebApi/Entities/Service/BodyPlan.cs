using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Service
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ValidationActionType
    {
        SettingsHandler,
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


    public class BodyPlan : IEquatable<BodyPlan>
    {
        public ValidationActionType ActionName { get; set; }
        public bool ContinueOnFailed { get; set; }
        public bool ContinueOnError { get; set; }
                
        public bool Equals(BodyPlan other)
        {
            return other != null && other.ActionName == this.ActionName;
        }

        public override int GetHashCode()
        {
            return this.ActionName.GetHashCode();
        }
    }
}
