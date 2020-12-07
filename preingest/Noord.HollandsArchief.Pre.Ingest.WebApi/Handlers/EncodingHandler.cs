using Microsoft.Extensions.Logging;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using Noord.HollandsArchief.Pre.Ingest.Utilities;
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
    public class EncodingHandler : AbstractPreingestHandler
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
                    bom = EncodingHelper.GetEncodingByBom(file);
                    current = EncodingHelper.GetEncodingByStream(file);
                    string xml = File.ReadAllText(file);
                    encoding = EncodingHelper.GetXmlEncoding(xml);

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


    }
}
