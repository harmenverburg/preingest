using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Structure
{
    public interface ISidecar
    {
        Guid InternalId { get; }
        String Name { get; }
        String TitlePath { get; }
        ToPX.topxType Metadata { get; set; }
        PronomItem PronomMetadataInfo { get; set; }
        String MetadataEncoding { get; set; }
        bool HasMetadata { get; }
        void PrepareMetadata(bool validateMetadata = false);
        void Validate();
        String MetadataFileLocation { get; set; }
        ISidecar Parent { get; set; }
        Boolean CompareAggregationLevel { get; }
        Boolean HasIdentification { get; }
        Boolean HasName { get; }
    }
}
