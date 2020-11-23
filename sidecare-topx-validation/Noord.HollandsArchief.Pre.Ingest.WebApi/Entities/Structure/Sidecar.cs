using Microsoft.Extensions.Logging;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.ToPX;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Structure
{
    [Serializable()]
    public abstract class Sidecar : ISidecar, IDisposable
    {
        private Guid _internalId = Guid.Empty;
        private List<SidecarException> _exception = null;

        public Sidecar(String name, String path, ISidecar parent, bool loadOnScan = false)
        {
            this._internalId = Guid.NewGuid();
            this._exception = new List<SidecarException>();

            this.Name = name;
            this.TitlePath = path;
            this.Parent = parent;  
            this.LoadMetadataOnScan = loadOnScan;
        }

        public Guid InternalId { get => _internalId; }            

        public bool HasMetadata => (this.Metadata != null);
        
        public String TitlePath { get; set; }
        
        public string Name { get; set; }       
        
        public topxType Metadata { get; set; }

        public abstract bool CompareAggregationLevel { get; }

        public List<SidecarException> ObjectExceptions()
        {
            return _exception;
        }

        public virtual bool HasIdentification
        {
            get
            {
                bool result = false;

                if (!this.HasMetadata)
                    return result;

                var aggregatieTypeObject = (this.Metadata.Item as aggregatieType);
                if (aggregatieTypeObject == null)
                    return result;

                if (aggregatieTypeObject.identificatiekenmerk == null)
                    return result;

                result = !String.IsNullOrEmpty((aggregatieTypeObject.identificatiekenmerk.Value));

                return result;
            }
        }

        public virtual bool HasName
        {
            get
            {
                bool result = false;

                if (!this.HasMetadata)
                    return result;

                var aggregatieTypeObject = (this.Metadata.Item as aggregatieType);
                if (aggregatieTypeObject == null)
                    return result;

                if (aggregatieTypeObject.naam == null)
                    return result;

                var naam = aggregatieTypeObject.naam.FirstOrDefault();

                if (naam == null)
                    return result;

                result = !(string.IsNullOrEmpty(naam.Value));

                return result;
            }
        }

        public ISidecar Parent { get; set; }

        public string MetadataFileLocation { get; set; }
                
        public void PrepareMetadata(string fileLocation)
        {     
            XDocument xml = null;
            try
            {
                if (String.IsNullOrEmpty (fileLocation) || !File.Exists(fileLocation))                
                    throw new FileNotFoundException(String.Format ("Metadata file with location '{0}' is empty or not found!", fileLocation));

                this.MetadataFileLocation = fileLocation;
                
                //if true, then load metadata file in memory
                if (!LoadMetadataOnScan)
                    return;

                xml = XDocument.Load(fileLocation);
                var xsd = this.ToPXSchemaDefinitionLocation();

                if(xsd == null)
                    throw new FileNotFoundException("ToPX xsd file not found!");

                SchemaValidationHandler.Validate(xml.ToString(), xsd);
            }
            catch (Exception e)
            {
                this._exception.Add(new SidecarException(e));                
            }

            try
            {
                if (xml == null)
                    throw new ApplicationException("Xml metadata object is null!");
            
                topxType toPXObject = DeserializerHelper.DeSerializeObject<topxType>(xml.ToString());
                if (toPXObject == null)
                    throw new NullReferenceException("ToPX object did not deserialized. Result is null!");

                this.Metadata = toPXObject;
            }
            catch (Exception e)
            {                
                this._exception.Add(new SidecarException(e));
            }
        }

        public abstract void Validate();

        public void Dispose()
        {
            UnloadMetadata();
        }

        protected Boolean LoadMetadataOnScan { get; set; }

        protected void UnloadMetadata()
        {
            this.Metadata = null;
            this.LoadMetadataOnScan = false;
        }

        protected void LoadMetadata()
        {
            LoadMetadataOnScan = true;
            PrepareMetadata(this.MetadataFileLocation);            
        }

        protected String ToPXSchemaDefinitionLocation()
        {
            string xsd = "ToPX-2.3_2.xsd";

            var exePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
            Regex appPathMatcher = new Regex(@"(?<!fil)[A-Za-z]:\\+[\S\s]*?(?=\\+bin)");
            var appRoot = appPathMatcher.Match(exePath).Value;

            string xsdFile = Path.Combine(appRoot, "Schema", xsd);

            if (File.Exists(xsdFile))
                return new FileInfo(xsdFile).FullName;
            else
                return null;
        }

    }
}
