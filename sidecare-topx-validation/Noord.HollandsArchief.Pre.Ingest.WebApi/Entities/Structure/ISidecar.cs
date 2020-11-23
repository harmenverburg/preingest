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
        bool HasMetadata { get; }
        void PrepareMetadata(string fileLocation);
        void Validate();
        String MetadataFileLocation { get; }
        ISidecar Parent { get; set; }
        Boolean CompareAggregationLevel { get; }
        Boolean HasIdentification { get; }
        Boolean HasName { get; }
    }
}
