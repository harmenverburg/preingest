using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Noord.HollandsArchief.Pre.Ingest.Utilities;
using Noord.HollandsArchief.Pre.Ingest.WebApi.EventHub;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Event;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Handler;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Structure;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Handlers
{
    //Check 5
    public class SidecarValidationHandler : AbstractPreingestHandler, IDisposable
    {
        public SidecarValidationHandler(AppSettings settings, IHubContext<PreingestEventHub> eventHub, CollectionHandler preingestCollection) : base(settings, eventHub, preingestCollection)
        {
            this.PreingestEvents += Trigger;
        }
        private String CollectionTitlePath(String fullnameLocation)
        {
            return fullnameLocation.Remove(0, TargetFolder.Length);
        }
        public override void Execute()
        {
            var eventModel = CurrentActionProperties(TargetCollection, this.GetType().Name);            
            OnTrigger(new PreingestEventArgs { Description = "Start sidecar structure validation.", Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Started, PreingestAction = eventModel });
            bool isSucces = false;

            PairNode<ISidecar> sidecarTreeNode = null;
            try
            {
                var collection = new DirectoryInfo(TargetFolder).GetDirectories().First();
                if (collection == null)
                    throw new DirectoryNotFoundException(String.Format("Folder '{0}' not found!", TargetFolder));

                PreingestEventArgs execEventArgs = new PreingestEventArgs { Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Executing, PreingestAction = eventModel };
                sidecarTreeNode = ScanSidecarStructure(collection, execEventArgs);
                //Calculate summary and save json
                SetSummary(collection, sidecarTreeNode, execEventArgs);
                //Validate objects and save json
                StartValidation(collection, sidecarTreeNode, execEventArgs);
                                
                if (eventModel.Summary.Rejected > 0)
                    eventModel.ActionResult.ResultValue = PreingestActionResults.Error;
                else
                    eventModel.ActionResult.ResultValue = PreingestActionResults.Success;

                isSucces = true;
            }
            catch(Exception e)
            {
                isSucces = false;
                Logger.LogError(e, "Exception occured in sidecar structure validation!");

                var anyMessages = new List<String>();
                anyMessages.Clear();
                anyMessages.Add("Exception occured in sidecar structure validation!");
                anyMessages.Add(e.Message);
                anyMessages.Add(e.StackTrace);

                eventModel.Summary.Processed = -1;
                eventModel.Summary.Accepted = -1;
                eventModel.Summary.Rejected = -1;

                eventModel.ActionResult.ResultValue = PreingestActionResults.Failed;
                eventModel.Properties.Messages = anyMessages.ToArray();

                OnTrigger(new PreingestEventArgs { Description = "An exception occured in sidecar structure validation!", Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Failed, PreingestAction = eventModel });
            }
            finally
            {
                if (isSucces)
                    OnTrigger(new PreingestEventArgs { Description = "Sidecar structure validation is done.", Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Completed, PreingestAction = eventModel, SidecarStructure = sidecarTreeNode });
            }
        }
        private PairNode<ISidecar> ScanSidecarStructure(DirectoryInfo collection, PreingestEventArgs eventArgs)
        {
            eventArgs.Description = "Walking through the folder structure.";
            OnTrigger(eventArgs);

            var start = DateTime.Now;
            Logger.LogInformation("Start scanning sidecar structure in '{0}'.", TargetFolder);
            Logger.LogInformation("Start time {0}", start);

            //archief
            var archiveName = collection.Name;
            var archiveMetadataFileName = String.Concat(archiveName, ".metadata");
            var dataFile = collection.GetFiles(archiveMetadataFileName).FirstOrDefault();
            
            ISidecar archiefSidecar = new Archief(archiveName, CollectionTitlePath(collection.FullName));
            archiefSidecar.MetadataFileLocation = dataFile == null ? String.Empty : !String.IsNullOrEmpty(dataFile.FullName) ? dataFile.FullName : String.Empty;
            if (!String.IsNullOrEmpty(archiefSidecar.MetadataFileLocation))
                archiefSidecar.MetadataEncoding = GetEncoding(archiefSidecar.MetadataFileLocation);
            archiefSidecar.PrepareMetadata();

            PairNode<ISidecar> sidecarTreeNode = new Entities.Structure.PairNode<ISidecar>(archiefSidecar, null);
            DeepScan(collection.FullName, sidecarTreeNode, eventArgs);           

            var end = DateTime.Now;
            Logger.LogInformation("End of scanning sidecar structure.");
            Logger.LogInformation("End time {0}", end);
            TimeSpan processTime = (TimeSpan)(end - start);
            Logger.LogInformation(String.Format("Processed in {0} ms.", processTime));

            return sidecarTreeNode;
        }
        private void SetSummary(DirectoryInfo collection, PairNode<ISidecar> sidecarTreeNode, PreingestEventArgs eventArgs)
        {
            //trigger event
            eventArgs.Description = String.Format("Start calculate summary sidecar structure in '{0}'.", TargetFolder);
            OnTrigger(eventArgs);

            var start = DateTime.Now;
            Logger.LogInformation("Start calculate summary sidecar structure in '{0}'.", TargetFolder);
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

            eventArgs.PreingestAction.Properties.Messages = messages;

            var end = DateTime.Now;
            Logger.LogInformation("End of calculation of the sidecar structure.");
            Logger.LogInformation("End time {0}", end);
            TimeSpan processTime = (TimeSpan)(end - start);
            Logger.LogInformation(String.Format("Processed in {0} ms.", processTime));
        }
        private void StartValidation(DirectoryInfo collection, PairNode<ISidecar> sidecarTreeNode, PreingestEventArgs eventArgs)
        {
            eventArgs.Description = String.Format("Start validate sidecar structure in '{0}'.", TargetFolder);
            OnTrigger(eventArgs);

            var start = DateTime.Now;
            Logger.LogInformation("Start validate sidecar structure in '{0}'.", TargetFolder);
            Logger.LogInformation("Start time {0}", start);

            var sidecarObjects = sidecarTreeNode.Flatten().Select(item => item.Data).Reverse().ToList();

            var archief = sidecarObjects.OfType<Archief>().ToList();
            var series = sidecarObjects.OfType<Series>().ToList();
            var record = sidecarObjects.OfType<Record>().ToList();
            var dossier = sidecarObjects.OfType<Dossier>().ToList();
            var bestand = sidecarObjects.OfType<Bestand>().ToList();
            var onbekend = sidecarObjects.OfType<NotDefined>().ToList();

            List<SidecarItem> validationResult = new List<SidecarItem>();           

            eventArgs.Description = "Validate level 'Archief'.";
            OnTrigger(eventArgs);
            archief.ForEach(item => item.Validate());
            var archiefResult = archief.Select(item => new SidecarItem { IsCorrect = (item.ObjectExceptions().Count == 0), Level = item.GetType().Name, TitlePath = item.TitlePath, ErrorMessages = item.ObjectExceptions().Select(m => m.Message).ToArray() }).ToList();
            validationResult.AddRange(archiefResult);

            eventArgs.Description = "Validate level 'Series'.";
            OnTrigger(eventArgs);
            series.ForEach(item => item.Validate());
            var seriesResult = series.Select(item => new SidecarItem { IsCorrect = (item.ObjectExceptions().Count == 0), Level = item.GetType().Name, TitlePath = item.TitlePath, ErrorMessages = item.ObjectExceptions().Select(m => m.Message).ToArray() }).ToList();
            validationResult.AddRange(seriesResult);

            eventArgs.Description = "Validate level 'Dossier'.";
            OnTrigger(eventArgs);
            dossier.ForEach(item => item.Validate());
            var dossierResult = dossier.Select(item => new SidecarItem { IsCorrect = (item.ObjectExceptions().Count == 0), Level = item.GetType().Name, TitlePath = item.TitlePath, ErrorMessages = item.ObjectExceptions().Select(m => m.Message).ToArray() }).ToList();
            validationResult.AddRange(dossierResult);

            eventArgs.Description = "Validate level 'Record'.";
            OnTrigger(eventArgs);
            record.ForEach(item => item.Validate());
            var recordResult = record.Select(item => new SidecarItem { IsCorrect = (item.ObjectExceptions().Count == 0), Level = item.GetType().Name, TitlePath = item.TitlePath, ErrorMessages = item.ObjectExceptions().Select(m => m.Message).ToArray() }).ToList();
            validationResult.AddRange(recordResult);

            eventArgs.Description = "Validate level 'Bestand'.";
            OnTrigger(eventArgs);
            bestand.ForEach(item => item.Validate());
            var bestandResult = bestand.Select(item => new SidecarItem { IsCorrect = (item.ObjectExceptions().Count == 0), Level = item.GetType().Name, TitlePath = item.TitlePath, ErrorMessages = item.ObjectExceptions().Select(m => m.Message).ToArray() }).ToList();
            validationResult.AddRange(bestandResult);

            eventArgs.Description = "Validate level 'NotDefined'.";
            OnTrigger(eventArgs);
            onbekend.ForEach(item => item.Validate());
            var onbekendResult = onbekend.Select(item => new SidecarItem { IsCorrect = (item.ObjectExceptions().Count == 0), Level = item.GetType().Name, TitlePath = item.TitlePath, ErrorMessages = item.ObjectExceptions().Select(m => m.Message).ToArray() }).ToList();
            validationResult.AddRange(onbekendResult);

            eventArgs.PreingestAction.Summary.Processed = validationResult.Count;
            eventArgs.PreingestAction.Summary.Accepted = validationResult.Where(item => item.IsCorrect).Count();
            eventArgs.PreingestAction.Summary.Rejected = validationResult.Where(item => !item.IsCorrect).Count();

            eventArgs.PreingestAction.ActionData = validationResult.ToArray();

            var end = DateTime.Now;
            Logger.LogInformation("End of the validation for the sidecar structure.");
            Logger.LogInformation("End time {0}", end);
            TimeSpan processTime = (TimeSpan)(end - start);
            Logger.LogInformation(String.Format("Processed in {0} ms.", processTime));
        }
        private void DeepScan(string directory, PairNode<ISidecar> sidecarTreeNode, PreingestEventArgs eventArgs)
        {
            eventArgs.Description = String.Format("Running deepscan for sidecar structure in '{0}'.", directory);
            OnTrigger(eventArgs);

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
                    
                    recordSidecar.MetadataFileLocation = dataFile == null? String.Empty : !String.IsNullOrEmpty(dataFile.FullName) ? dataFile.FullName : String.Empty;
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
                        
                        bestandSidecar.MetadataFileLocation = (binaryDataFile != null) ? binaryDataFile.FullName : string.Empty;
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

                    serieSidecar.MetadataFileLocation = dataFile == null ? string.Empty : !String.IsNullOrEmpty(dataFile.FullName) ? dataFile.FullName : string.Empty;
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

                    recordSidecar.MetadataFileLocation = dataFile == null ? String.Empty : !String.IsNullOrEmpty(dataFile.FullName) ? dataFile.FullName : null;
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

                    serieSidecar.MetadataFileLocation = dataFile == null ? String.Empty : !String.IsNullOrEmpty(dataFile.FullName) ? dataFile.FullName : string.Empty;
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

                        unknownSidecar.MetadataFileLocation = unknownFile == null ? String.Empty : !String.IsNullOrEmpty(unknownFile.FullName) ? unknownFile.FullName : String.Empty;
                        if (!String.IsNullOrEmpty(unknownSidecar.MetadataFileLocation))
                            unknownSidecar.MetadataEncoding = GetEncoding(unknownSidecar.MetadataFileLocation);
                        unknownSidecar.PrepareMetadata();
                        
                        childSidecarTreeNode = sidecarTreeNode.AddChild(unknownSidecar);
                    });             

                    childSidecarTreeNode = sidecarTreeNode.AddChild(serieSidecar);
                }

                DeepScan(subdirectory, childSidecarTreeNode, eventArgs);
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
        public void Dispose()
        {
            this.PreingestEvents -= Trigger;
        }
    }
}
