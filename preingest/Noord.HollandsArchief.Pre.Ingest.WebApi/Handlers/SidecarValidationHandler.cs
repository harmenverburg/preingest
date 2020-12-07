using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Noord.HollandsArchief.Pre.Ingest.Utilities;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Structure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Handlers
{
    //Check 5
    public class SidecarValidationHandler : AbstractPreingestHandler
    {        
        public SidecarValidationHandler(AppSettings settings) : base(settings)  { }
        private String TargetFolder { get => Path.Combine(ApplicationSettings.DataFolderName, SessionGuid.ToString()); }
        private String CollectionTitlePath(String fullnameLocation)
        {
            return fullnameLocation.Remove(0, TargetFolder.Length);
        }
        public override void Execute()
        {
            var collection = new DirectoryInfo(TargetFolder).GetDirectories().First();
            if (collection == null)
                return;

            PairNode<ISidecar> sidecarTreeNode = ScanSidecarStructure(collection);
           
            //Calculate summary and save json
            SetSummary(collection, sidecarTreeNode);           
            //Validate objects and save json
            StartValidation(collection, sidecarTreeNode);  
            //Save binary with validation
            SaveBinary(new DirectoryInfo(TargetFolder), this, sidecarTreeNode);                      
        }

        public PairNode<ISidecar> ScanSidecarStructure(DirectoryInfo collection)
        {
            var start = DateTime.Now;
            Logger.LogInformation("Start scanning sidecare structure in '{0}'.", TargetFolder);
            Logger.LogInformation("Start time {0}", start);

            //archief
            var archiveName = collection.Name;
            var archiveMetadataFileName = String.Concat(archiveName, ".metadata");
            var dataFile = collection.GetFiles(archiveMetadataFileName).FirstOrDefault();
            
            ISidecar archiefSidecar = new Archief(archiveName, CollectionTitlePath(collection.FullName));
            archiefSidecar.MetadataFileLocation = !String.IsNullOrEmpty(dataFile.FullName) ? dataFile.FullName : null;
            if (!String.IsNullOrEmpty(archiefSidecar.MetadataFileLocation))
                archiefSidecar.MetadataEncoding = GetEncoding(archiefSidecar.MetadataFileLocation);
            archiefSidecar.PrepareMetadata();

            PairNode<ISidecar> sidecarTreeNode = new Entities.Structure.PairNode<ISidecar>(archiefSidecar, null);
            DeepScan(collection.FullName, sidecarTreeNode);
           
            var end = DateTime.Now;
            Logger.LogInformation("End of scanning sidecare structure.");
            Logger.LogInformation("End time {0}", end);
            TimeSpan processTime = (TimeSpan)(end - start);
            Logger.LogInformation(String.Format("Processed in {0} ms.", processTime));

            return sidecarTreeNode;
        }

        public void SetSummary(DirectoryInfo collection, PairNode<ISidecar> sidecarTreeNode)
        {
            var start = DateTime.Now;
            Logger.LogInformation("Start calculate summary sidecare structure in '{0}'.", TargetFolder);
            Logger.LogInformation("Start time {0}", start);

            var sidecarObjects = sidecarTreeNode.Flatten().Select(item => item.Data).Reverse().ToList();

            var archief = sidecarObjects.OfType<Archief>().ToList();
            var series = sidecarObjects.OfType<Series>().ToList();
            var record = sidecarObjects.OfType<Record>().ToList();
            var dossier = sidecarObjects.OfType<Dossier>().ToList();
            var bestand = sidecarObjects.OfType<Bestand>().ToList();
            var onbekend = sidecarObjects.OfType<NotDefined>().ToList();

            String[] messages = new String[] 
            {
                String.Format("Archief : {0} item(s)", archief.Count),
                String.Format("Series : {0} item(s)", series.Count),
                String.Format("Record : {0} item(s)", record.Count),
                String.Format("Dossier : {0} item(s)", dossier.Count),
                String.Format("Bestand : {0} item(s)", bestand.Count),
                String.Format("Onbekend : {0} item(s)", onbekend.Count)
            };          

            var result = new ProcessResult(SessionGuid)
            {
                CollectionItem = collection.FullName,
                Code = collection.Name,
                CreationTimestamp = DateTime.Now,
                ActionName = this.GetType().Name,
                Messages = messages
            };

            SaveJson(new DirectoryInfo(TargetFolder), this, "Samenvatting", new[] { result });

            var end = DateTime.Now;
            Logger.LogInformation("End of calculation of the sidecare structure.");
            Logger.LogInformation("End time {0}", end);
            TimeSpan processTime = (TimeSpan)(end - start);
            Logger.LogInformation(String.Format("Processed in {0} ms.", processTime));
        }

        public void StartValidation(DirectoryInfo collection, PairNode<ISidecar> sidecarTreeNode)
        {
            var start = DateTime.Now;
            Logger.LogInformation("Start to validate sidecare structure in '{0}'.", TargetFolder);
            Logger.LogInformation("Start time {0}", start);

            var sidecarObjects = sidecarTreeNode.Flatten().Select(item => item.Data).Reverse().ToList();

            var archief = sidecarObjects.OfType<Archief>().ToList();
            var series = sidecarObjects.OfType<Series>().ToList();
            var record = sidecarObjects.OfType<Record>().ToList();
            var dossier = sidecarObjects.OfType<Dossier>().ToList();
            var bestand = sidecarObjects.OfType<Bestand>().ToList();
            var onbekend = sidecarObjects.OfType<NotDefined>().ToList();

            archief.ForEach(item => item.Validate());
            series.ForEach(item => item.Validate());
            dossier.ForEach(item => item.Validate());
            record.ForEach(item => item.Validate());
            bestand.ForEach(item => item.Validate());
            onbekend.ForEach(item => item.Validate());

            var archiefResult = archief.Where(item => item.ObjectExceptions().Count > 0).Select(item => new ProcessResult(SessionGuid)
            {
                CollectionItem = item.TitlePath,
                Code = "Archief",
                CreationTimestamp = DateTime.Now,
                ActionName = this.GetType().Name,
                Messages = item.ObjectExceptions().Select(message => message.Message).ToArray()
            });

            SaveJson(new DirectoryInfo(TargetFolder), this, "Archief", archiefResult.ToArray());

            var seriesResult = series.Where(item => item.ObjectExceptions().Count > 0).Select(item => new ProcessResult(SessionGuid)
            {
                CollectionItem = item.TitlePath,
                Code = "Series",
                CreationTimestamp = DateTime.Now,
                ActionName = this.GetType().Name,
                Messages = item.ObjectExceptions().Select(message => message.Message).ToArray()
            });

            SaveJson(new DirectoryInfo(TargetFolder), this, "Series", seriesResult.ToArray());

            var dossierResult = dossier.Where(item => item.ObjectExceptions().Count > 0).Select(item => new ProcessResult(SessionGuid)
            {
                CollectionItem = item.TitlePath,
                Code = "Dossier",
                CreationTimestamp = DateTime.Now,
                ActionName = this.GetType().Name,
                Messages = item.ObjectExceptions().Select(message => message.Message).ToArray()
            });

            SaveJson(new DirectoryInfo(TargetFolder), this, "Dossier", dossierResult.ToArray());

            var recordResult = record.Where(item => item.ObjectExceptions().Count > 0).Select(item => new ProcessResult(SessionGuid)
            {
                CollectionItem = item.TitlePath,
                Code = "Record",
                CreationTimestamp = DateTime.Now,
                ActionName = this.GetType().Name,
                Messages = item.ObjectExceptions().Select(message => message.Message).ToArray()
            });

            SaveJson(new DirectoryInfo(TargetFolder), this, "Record", recordResult.ToArray());

            var bestandResult = bestand.Where(item => item.ObjectExceptions().Count > 0).Select(item => new ProcessResult(SessionGuid)
            {
                CollectionItem = item.TitlePath,
                Code = "Bestand",
                CreationTimestamp = DateTime.Now,
                ActionName = this.GetType().Name,
                Messages = item.ObjectExceptions().Select(message => message.Message).ToArray()
            });

            SaveJson(new DirectoryInfo(TargetFolder), this, "Bestand", bestandResult.ToArray());

            var onbekendResult = onbekend.Select(item => new ProcessResult(SessionGuid)
            {
                CollectionItem = item.TitlePath,
                Code = "Onbekend",
                CreationTimestamp = DateTime.Now,
                ActionName = this.GetType().Name,
                Message = "Onbekend, niet ok!"
            });

            SaveJson(new DirectoryInfo(TargetFolder), this, "Onbekend", onbekendResult.ToArray());

            var end = DateTime.Now;
            Logger.LogInformation("End of the validation for the sidecare structure.");
            Logger.LogInformation("End time {0}", end);
            TimeSpan processTime = (TimeSpan)(end - start);
            Logger.LogInformation(String.Format("Processed in {0} ms.", processTime));
        }
        
        private void DeepScan(string directory, PairNode<ISidecar> sidecarTreeNode)
        {
            this.Logger.LogDebug("Processing folder '{0}'", directory);

            string[] subdirectoryEntries = Directory.GetDirectories(directory);

            foreach (string subdirectory in subdirectoryEntries)
            {
                DirectoryInfo di = new DirectoryInfo(subdirectory);

                int subDirectoriesCount = di.GetDirectories().Count();
                int subFilesCount = di.GetFiles().Where(item => !item.Extension.Equals(".metadata")).Count();

                PairNode<ISidecar> childSidecarTreeNode = null;
                
                //geen onderliggende mappen , wel documenten dan aanname is einde van sidecar Record + Bestand
                if (subDirectoriesCount == 0 && subFilesCount > 0)//suppose to be Record
                {
                    //boven een record moet het een dossier zijn
                    if(sidecarTreeNode.Data is Series)
                    {
                        Dossier dossier = ((Dossier)(sidecarTreeNode.Data as Series));
                        var parentTreeNode = sidecarTreeNode.Parent;
                        bool result = parentTreeNode.RemoveChild(sidecarTreeNode);
                        sidecarTreeNode = parentTreeNode.AddChild(dossier);
                    }

                    var recordName = di.Name;
                    var recordMetadataFileName = String.Concat(recordName, ".metadata");
                    var dataFile = di.GetFiles(recordMetadataFileName).FirstOrDefault();
                    ISidecar recordSidecar = new Record(recordName, CollectionTitlePath(di.FullName), (sidecarTreeNode.Data as Dossier));
                    
                    recordSidecar.MetadataFileLocation = !String.IsNullOrEmpty(dataFile.FullName) ? dataFile.FullName : null;
                    if (!String.IsNullOrEmpty(recordSidecar.MetadataFileLocation))
                        recordSidecar.MetadataEncoding = GetEncoding(recordSidecar.MetadataFileLocation);
                    recordSidecar.PrepareMetadata();                                                     

                    childSidecarTreeNode = sidecarTreeNode.AddChild(recordSidecar);

                    var binaries = di.GetFiles().Where(item => !item.Extension.Equals(".metadata"));
                    foreach (FileInfo binary in binaries)
                    {
                        string binaryMetadataFilename = String.Concat(binary.Name, ".metadata");

                        Bestand bestandSidecar = new Bestand(binary.Name, CollectionTitlePath(binary.FullName), binary.FullName, (childSidecarTreeNode.Data as Record));
                        var binaryDataFile = di.GetFiles(binaryMetadataFilename).FirstOrDefault();
                        
                        bestandSidecar.MetadataFileLocation = (binaryDataFile != null) ? binaryDataFile.FullName : null;
                        if (!String.IsNullOrEmpty(bestandSidecar.MetadataFileLocation))
                            bestandSidecar.MetadataEncoding = GetEncoding(bestandSidecar.MetadataFileLocation);

                        bestandSidecar.BinaryFileLocation = binary.FullName;
                        bestandSidecar.BinaryFileLength = binary.Length;

                        bestandSidecar.PrepareMetadata();                        

                        childSidecarTreeNode.AddChild(bestandSidecar);
                    }
                }//alleen onderliggende mappen, dan aanname is Serie
                else if (subDirectoriesCount > 0 && subFilesCount == 0)//suppose to be Serie
                {
                    var serieName = di.Name;
                    var serieMetadataFileName = String.Concat(serieName, ".metadata");
                    var dataFile = di.GetFiles(serieMetadataFileName).FirstOrDefault();
                    ISidecar serieSidecar = null;
                    
                    if (sidecarTreeNode.Parent == null)
                        serieSidecar = new Series(serieName, CollectionTitlePath(di.FullName), (sidecarTreeNode.Data as Archief));
                    else                    
                        serieSidecar = new Series(serieName, CollectionTitlePath(di.FullName), (sidecarTreeNode.Data as Series));

                    serieSidecar.MetadataFileLocation = !String.IsNullOrEmpty(dataFile.FullName) ? dataFile.FullName : null;
                    if (!String.IsNullOrEmpty(serieSidecar.MetadataFileLocation))
                        serieSidecar.MetadataEncoding = GetEncoding(serieSidecar.MetadataFileLocation);
                    serieSidecar.PrepareMetadata();                    

                    childSidecarTreeNode = sidecarTreeNode.AddChild(serieSidecar);
                }
                else if (subDirectoriesCount == 0 && subFilesCount == 0)//suppose to be Serie
                {
                    //boven een record moet het een dossier zijn
                    if (sidecarTreeNode.Data is Series)
                    {
                        Dossier dossier = ((Dossier)(sidecarTreeNode.Data as Series));
                        var parentTreeNode = sidecarTreeNode.Parent;
                        bool result = parentTreeNode.RemoveChild(sidecarTreeNode);
                        sidecarTreeNode = parentTreeNode.AddChild(dossier);
                    }

                    var recordName = di.Name;
                    var recordMetadataFileName = String.Concat(recordName, ".metadata");
                    var dataFile = di.GetFiles(recordMetadataFileName).FirstOrDefault();
                    ISidecar recordSidecar = new Record(recordName, CollectionTitlePath(di.FullName), (sidecarTreeNode.Data as Dossier));

                    recordSidecar.MetadataFileLocation = !String.IsNullOrEmpty(dataFile.FullName) ? dataFile.FullName : null;
                    if (!String.IsNullOrEmpty(recordSidecar.MetadataFileLocation))
                        recordSidecar.MetadataEncoding = GetEncoding(recordSidecar.MetadataFileLocation);
                    recordSidecar.PrepareMetadata();

                    childSidecarTreeNode = sidecarTreeNode.AddChild(recordSidecar);

                    (recordSidecar as Record).ObjectExceptions().Clear();
                    (recordSidecar as Record).ObjectExceptions().Add(new SidecarException("Een leeg 'Record'. Geen bestanden gevonden!"));                   
                }
                else//not defined too many folders with too many files
                {    
                    var serieName = di.Name;
                    var serieMetadataFileName = String.Concat(serieName, ".metadata");
                    var dataFile = di.GetFiles(serieMetadataFileName).FirstOrDefault();

                    ISidecar serieSidecar = null;

                    if (sidecarTreeNode.Parent == null)
                        serieSidecar = new Series(serieName, CollectionTitlePath(di.FullName), (sidecarTreeNode.Data as Archief));
                    else
                        serieSidecar = new Series(serieName, CollectionTitlePath(di.FullName), (sidecarTreeNode.Data as Series));

                    serieSidecar.MetadataFileLocation = !String.IsNullOrEmpty(dataFile.FullName) ? dataFile.FullName : null;
                    if (!String.IsNullOrEmpty(serieSidecar.MetadataFileLocation))
                        serieSidecar.MetadataEncoding = GetEncoding(serieSidecar.MetadataFileLocation);
                    serieSidecar.PrepareMetadata();

                    var restOfTheFiles = di.GetFiles().Where<FileInfo>(item => item.FullName != dataFile.FullName).ToList();
                    restOfTheFiles.ForEach(unknown =>
                    {
                        var unknownName = unknown.Name;

                        var unknownMetadataFileName = String.Concat(serieName, ".metadata");
                        var unknownFile = di.GetFiles(unknownMetadataFileName).FirstOrDefault();

                        ISidecar unknownSidecar = new NotDefined(unknownName, CollectionTitlePath(di.FullName), sidecarTreeNode.Data);

                        unknownSidecar.MetadataFileLocation = !String.IsNullOrEmpty(unknownFile.FullName) ? unknownFile.FullName : null;
                        if (!String.IsNullOrEmpty(unknownSidecar.MetadataFileLocation))
                            unknownSidecar.MetadataEncoding = GetEncoding(unknownSidecar.MetadataFileLocation);
                        unknownSidecar.PrepareMetadata();
                        
                        childSidecarTreeNode = sidecarTreeNode.AddChild(unknownSidecar);
                    });             

                    childSidecarTreeNode = sidecarTreeNode.AddChild(serieSidecar);
                }

                DeepScan(subdirectory, childSidecarTreeNode);
            }
        }

        private String GetEncoding(string metadata)
        {
            string result = string.Empty;

            Encoding bom = null;
            Encoding current = null;
            String encoding = string.Empty;

            try
            {
                bom = EncodingHelper.GetEncodingByBom(metadata);
                current = EncodingHelper.GetEncodingByStream(metadata);
                string xml = File.ReadAllText(metadata);
                encoding = EncodingHelper.GetXmlEncoding(xml);

                result = String.Format("Byte Order Mark : {0}, Stream : {1}, XML : {2}", (bom != null) ? bom.EncodingName : "Byte Order Mark niet gevonden", (current != null) ? current.EncodingName : "In stream niet gevonden", String.IsNullOrEmpty(encoding) ? "In XML niet gevonden" : encoding);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Get encoding from file : '{0}' failed!", metadata);
                result = "Er is een fout opgetreden bij het pepalen van de encoding!";
            }
           
            return result;
        }
    }
}
