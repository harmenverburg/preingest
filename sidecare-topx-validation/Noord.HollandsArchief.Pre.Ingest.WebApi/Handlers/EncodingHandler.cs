using Microsoft.Extensions.Logging;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Handlers
{
    public class EncodingHandler : AbstractPreIngestChecksHandler
    {
        public EncodingHandler(AppSettings settings) : base(settings) {  }

        private String TargetFolder { get => Path.Combine(ApplicationSettings.DataFolderName, SessionGuid.ToString()); }

        public override void Execute()
        {
            string targetFolder = TargetFolder;
            string[] metadatas = Directory.GetFiles(targetFolder, "*.metadata", SearchOption.AllDirectories);

            var result = new List<ProcessResult>();

            foreach (string file in metadatas)
            {
                Logger.LogInformation("Get encoding from file : '{0}'", file);
                Encoding bom = null;
                Encoding current = null;
                String encoding = string.Empty;

                bool isSucces = false;
                try
                {
                    bom = GetEncodingByBom(file);
                    current = GetEncodingByStream(file);
                    string xml = File.ReadAllText(file);
                    encoding = GetXmlEncoding(xml);

                    isSucces = true;
                }
                catch(Exception e)
                {
                    Logger.LogError(e, "Get encoding from file : '{0}' failed!", file);
                    isSucces = false;
                }
                finally
                {
                    if (isSucces)
                    {
                        ProcessResult process = new ProcessResult(SessionGuid)
                        {
                            CollectionItem = file,
                            Code = "Encoding",
                            CreationTimestamp = DateTime.Now,
                            ActionName = this.GetType().Name,
                            Message = String.Format("Byte Order Mark : {0}, Stream : {1}, XML : {2}", (bom != null) ? bom.EncodingName : "Byte Order Mark niet gevonden", (current != null) ? current.EncodingName : "In stream niet gevonden", String.IsNullOrEmpty(encoding) ? "In XML niet gevonden" : encoding)
                        };
                        result.Add(process);
                    }
                }
            }

            SaveJson(new DirectoryInfo(TargetFolder), this, result.ToArray());
        }


        private Encoding GetEncodingByBom(string filename)
        {
            // Read the BOM
            var bom = new byte[4];
            using (var file = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                file.Read(bom, 0, 4);
            }

            // Analyze the BOM
            if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76) return Encoding.UTF7;
            if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) return Encoding.UTF8;
            if (bom[0] == 0xff && bom[1] == 0xfe && bom[2] == 0 && bom[3] == 0) return Encoding.UTF32; //UTF-32LE
            if (bom[0] == 0xff && bom[1] == 0xfe) return Encoding.Unicode; //UTF-16LE
            if (bom[0] == 0xfe && bom[1] == 0xff) return Encoding.BigEndianUnicode; //UTF-16BE
            if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff) return new UTF32Encoding(true, true);  //UTF-32BE

            // We actually have no idea what the encoding is if we reach this point, so
            // you may wish to return null instead of defaulting to ASCII
            return Encoding.ASCII;
        }

        private Encoding GetEncodingByStream(string filename)
        {
            // This is a direct quote from MSDN:  
            // The CurrentEncoding value can be different after the first
            // call to any Read method of StreamReader, since encoding
            // autodetection is not done until the first call to a Read method.

            using (var reader = new StreamReader(filename, Encoding.Default, true))
            {
                if (reader.Peek() >= 0) // you need this!
                    reader.Read();

                return reader.CurrentEncoding;
            }
        }

        private string GetXmlEncoding(string xmlString)
        {
            if (string.IsNullOrWhiteSpace(xmlString)) throw new ArgumentException("The provided string value is null or empty.");

            using (var stringReader = new StringReader(xmlString))
            {
                var settings = new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment };

                using (var xmlReader = XmlReader.Create(stringReader, settings))
                {
                    if (!xmlReader.Read()) throw new ArgumentException(
                        "The provided XML string does not contain enough data to be valid XML (see https://msdn.microsoft.com/en-us/library/system.xml.xmlreader.read)");

                    var result = xmlReader.GetAttribute("encoding");
                    return result;
                }
            }
        }
    }
}
