using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using System;

namespace Noord.HollandsArchief.Pre.Ingest.WorkerService.Entities.CommandKey
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
        TransformationHandler,
        SipZipMetadataValidationHandler,
        SipZipCopyHandler
    }
    public interface IKey : IEquatable<IKey>
    {
        ValidationActionType Name { get; set; }
    }
}
