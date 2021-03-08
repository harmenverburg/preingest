using CsvHelper;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using Noord.HollandsArchief.Pre.Ingest.WebApi.EventHub;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Event;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Handler;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Handlers
{
    public class GreenListHandler : AbstractPreingestHandler, IDisposable
    {
        public GreenListHandler(AppSettings settings, IHubContext<PreingestEventHub> eventHub, CollectionHandler preingestCollection) : base(settings, eventHub, preingestCollection)
        {
            this.PreingestEvents += Trigger;
        }
        public String GreenlistLocation()
        {
            string json = "greenlist.json";

            var exePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            Regex appPathMatcher = new Regex(@"(?<!fil)[A-Za-z]:\\+[\S\s]*?(?=\\+bin)");
            var appRoot = appPathMatcher.Match(exePath).Value;

            string jsonFile = Path.Combine(appRoot, "Datasource", json);

            if (File.Exists(jsonFile))
                return new FileInfo(jsonFile).FullName;
            else
                return null;
        }
        public String DroidCsvOutputLocation()
        {
            var directory = new DirectoryInfo(TargetFolder);
            var files = directory.GetFiles("*.csv");

            if (files.Count() > 0)
            {
                FileInfo droidCsvFile = files.OrderByDescending(item => item.CreationTime).First();
                if (droidCsvFile == null)
                    return null;
                else
                    return droidCsvFile.FullName;
            }
            else
            {
                return null;
            }
        }
        public override void Execute()
        {
            var eventModel = CurrentActionProperties(TargetCollection, this.GetType().Name);
            OnTrigger(new PreingestEventArgs { Description = "Start compare extensions with greenlist.", Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Started, PreingestAction = eventModel });

            var anyMessages = new List<String>();
            bool isSuccess = false;
            try
            {
                string droidCsvFile = DroidCsvOutputLocation();
                if (String.IsNullOrEmpty(droidCsvFile))
                    throw new FileNotFoundException("CSV file not found!", droidCsvFile);

                string greenListLocation = GreenlistLocation();
                if (String.IsNullOrEmpty(greenListLocation))
                    throw new FileNotFoundException("Greenlist JSON file not found!", greenListLocation);

                string extensionJson = File.ReadAllText(greenListLocation);
                var extensionData = JsonConvert.DeserializeObject<GreenListItem[]>(extensionJson).ToList();

                OnTrigger(new PreingestEventArgs { Description = "Read CSV file.",  Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Executing, PreingestAction = eventModel });

                using (var reader = new StreamReader(droidCsvFile))
                {
                    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                    {
                        var records = csv.GetRecords<dynamic>().ToList();

                        var filesByDroid = records.Where(item
                            => item.TYPE == "File" && item.EXT != "metadata").Select(item => new DataItem
                            {
                                Location = item.FILE_PATH,
                                Name = item.NAME,
                                Extension = item.EXT,
                                FormatName = item.FORMAT_NAME,
                                FormatVersion = item.FORMAT_VERSION,
                                Puid = item.PUID,
                                IsExtensionMisMatch = item.EXTENSION_MISMATCH
                            }).ToList();
                                                
                        var actionDataList = new List<DataItem>();
                        eventModel.Summary.Processed = filesByDroid.Count;
                        eventModel.Summary.Accepted = 0;
                        eventModel.Summary.Rejected = 0;
                        filesByDroid.ForEach(file =>                        
                        {
                            OnTrigger(new PreingestEventArgs { Description = string.Format( "Processing {0}", file.Location), Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Executing, PreingestAction = eventModel });

                            //no puid found
                            if (string.IsNullOrEmpty(file.Puid))
                            {
                                actionDataList.Add(new DataItem { Puid = file.Puid, Name = file.Name, Location = file.Location, FormatVersion = file.FormatVersion, FormatName = file.FormatName, Extension = file.Extension, InGreenList = false, Message = "Geen Pronom ID gevonden." });
                                eventModel.Summary.Rejected = eventModel.Summary.Rejected + 1;
                                return;
                            }
                            
                            //extension mismatch
                            bool parseOutput = false;
                            Boolean.TryParse(file.IsExtensionMisMatch, out parseOutput);                            
                            if (parseOutput)
                            {
                                actionDataList.Add(new DataItem { Puid = file.Puid, Name = file.Name, Location = file.Location, FormatVersion = file.FormatVersion, FormatName = file.FormatName, Extension = file.Extension, InGreenList = false, Message = "Verkeerde extensie combinatie gevonden." });
                                eventModel.Summary.Rejected = eventModel.Summary.Rejected + 1;
                                return;
                            }

                            //pronom in nha list
                            bool existsPuidInNhaList = extensionData.Exists(item => item.Puid.Equals(file.Puid, StringComparison.InvariantCultureIgnoreCase));
                            if (existsPuidInNhaList)
                            {
                                file.InGreenList = true;
                                file.Message = "Pronom ID gevonden in NHA groene lijst.";
                                actionDataList.Add(file);
                                eventModel.Summary.Accepted = eventModel.Summary.Accepted + 1;
                                return;
                            }

                            //extension in nha list 
                            bool existsExtInNhaList = extensionData.Exists(item => item.Extension.Equals(file.Extension, StringComparison.InvariantCultureIgnoreCase));
                            if (!existsExtInNhaList)
                            {
                                actionDataList.Add(new DataItem { Puid = file.Puid, Name = file.Name, Location = file.Location, FormatVersion = file.FormatVersion, FormatName = file.FormatName, Extension = file.Extension, InGreenList = false, Message = "Extensie en/of Pronom ID niet gevonden in NHA groene lijst." });
                                eventModel.Summary.Rejected = eventModel.Summary.Rejected + 1;
                            }                               
                            else
                            {
                                file.InGreenList = true;
                                file.Message = String.Format ("Extensie gevonden in NHA groene lijst maar wel met een andere Pronom ID. Droid Pronom = {0}, NHA groene lijst = {1}.", file.Puid, extensionData.FirstOrDefault(item => item.Extension.Equals(file.Extension, StringComparison.InvariantCultureIgnoreCase)).Puid);
                                actionDataList.Add(file);
                                eventModel.Summary.Accepted = eventModel.Summary.Accepted + 1;
                            }
                        }); 
                        
                        eventModel.ActionData = actionDataList.ToArray(); 

                        OnTrigger(new PreingestEventArgs { Description = "Done comparing both lists.", Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Executing, PreingestAction = eventModel });
                    }
                }
                isSuccess = true;

                if (eventModel.Summary.Rejected > 0)
                    eventModel.ActionResult.ResultValue = PreingestActionResults.Error;
                else
                    eventModel.ActionResult.ResultValue = PreingestActionResults.Success;
            }
            catch (Exception e)
            {
                isSuccess = false;
                Logger.LogError(e, "Comparing greenlist with CSV failed!");

                anyMessages.Add(String.Format("Comparing greenlist with CSV failed!"));
                anyMessages.Add(e.Message);
                anyMessages.Add(e.StackTrace);

                //eventModel.Summary.Processed = -1;
                eventModel.Summary.Accepted = 0;
                eventModel.Summary.Rejected = eventModel.Summary.Processed;

                eventModel.Properties.Messages = anyMessages.ToArray();
                eventModel.ActionResult.ResultValue = PreingestActionResults.Failed;               

                OnTrigger(new PreingestEventArgs { Description="An exception occured while comparing greenlist with CSV!", Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Failed, PreingestAction = eventModel });
            }
            finally
            {
                if (isSuccess)
                    OnTrigger(new PreingestEventArgs { Description="Comparing greenlist using CSV from DROID is done.", Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Completed, PreingestAction = eventModel });                
            }
        }
        public void Dispose()
        {
            this.PreingestEvents -= Trigger;
        }

        internal class DataItem
        {
            public string Location { get; set; }
            public string Name { get; set; }
            public string Extension { get; set; }
            public string FormatName { get; set; }
            public string FormatVersion { get; set; }
            public string Puid { get; set; }
            public string IsExtensionMismatch { get; set; }
            public string Message { get; set; }
            public bool InGreenList { get; set; }
        }
    }
}