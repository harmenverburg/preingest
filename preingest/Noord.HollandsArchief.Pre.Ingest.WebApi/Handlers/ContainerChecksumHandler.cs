using Microsoft.Extensions.Logging;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using Noord.HollandsArchief.Pre.Ingest.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Handlers
{
    public class ContainerChecksumHandler : AbstractPreingestHandler
    {
        public ContainerChecksumHandler(AppSettings settings) : base(settings){ }
        public String TarFilename { get; set; }

        public String Checksum { get; set; }

        private String TargetCollection { get => Path.Combine(ApplicationSettings.DataFolderName, TarFilename); }

        public override void Execute()
        {
            Logger.LogInformation("Calculate checksum for file : '{0}'", TargetCollection);

            var result = new List<ProcessResult>();

            if (File.Exists(TargetCollection))
            {
                bool isSucces = false;
                string currentCalculation = string.Empty;
                try
                {                    
                    switch (Checksum.ToUpperInvariant())
                    {
                        case "MD5":
                            currentCalculation = ChecksumHelper.CreateMD5Checksum(new FileInfo(TargetCollection));
                            break;
                        case "SHA1":
                        case "SHA-1":
                            currentCalculation = ChecksumHelper.CreateSHA1Checksum(new FileInfo(TargetCollection));
                            break;
                        case "SHA256":
                        case "SHA-256":
                            currentCalculation = ChecksumHelper.CreateSHA256Checksum(new FileInfo(TargetCollection));
                            break;
                        case "SHA512":
                        case "SHA-512":
                            currentCalculation = ChecksumHelper.CreateSHA512Checksum(new FileInfo(TargetCollection));
                            break;
                        default:
                            {
                                Logger.LogWarning("Checksum {0} not defined. No calculation available.", Checksum);
                            }
                            break;
                    }

                    isSucces = !String.IsNullOrEmpty(currentCalculation);
                }
                catch(Exception e)
                {
                    Logger.LogError(e, "Calculation checksum from file : '{0}' failed!", TargetCollection);
                    isSucces = false;
                }
                finally
                {
                    if (isSucces)
                    {
                        ProcessResult process = new ProcessResult(SessionGuid)
                        {
                            CollectionItem = TargetCollection,
                            Code = "Checksum",
                            CreationTimestamp = DateTime.Now,
                            ActionName = this.GetType().Name,
                            Message = String.Format("{0} : {1}", Checksum.ToUpperInvariant(), currentCalculation)
                        };
                        result.Add(process);
                    }
                }
            }
            else
            {
                Logger.LogInformation("Container not found '{0}'", TargetCollection);
            }

            if(result.Count == 0)
                result.Add(new ProcessResult(SessionGuid)
                {
                    CollectionItem = TargetCollection,
                    Code = "Checksum",
                    CreationTimestamp = DateTime.Now,
                    ActionName = this.GetType().Name,
                    Message = "Geen resultaten."
                });

            SaveJson(Path.Combine(ApplicationSettings.DataFolderName, String.Format("{0}.json", TarFilename)), this, result.ToArray());
        }

    }
}
