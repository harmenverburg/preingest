using CsvHelper;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Event;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Handler;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Handlers
{
    public class GreenListHandler : AbstractPreingestHandler
    {
        public GreenListHandler(AppSettings settings) : base(settings) { }

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
                var extensionData = JsonConvert.DeserializeObject<GreenListItem[]>(extensionJson);

                OnTrigger(new PreingestEventArgs { Description = "Read CSV file.",  Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Executing, PreingestAction = eventModel });
                using (var reader = new StreamReader(droidCsvFile))
                {
                    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                    {
                        var records = csv.GetRecords<dynamic>().ToList();

                        var filesByDroid = records.Where(item
                            => item.TYPE == "File" && item.EXT != "metadata").Select(item => new
                            {
                                SessionId = SessionGuid,
                                Location = item.FILE_PATH,
                                Name = item.NAME,
                                Extension = item.EXT,
                                FormatName = item.FORMAT_NAME,
                                FormatVersion = item.FORMAT_VERSION,
                                Puid = item.PUID
                            }).ToList();

                        eventModel.Summary.Processed = filesByDroid.Count;

                        OnTrigger(new PreingestEventArgs { Description = "Filter only files (no folders or files with .metadata extension).", Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Executing, PreingestAction = eventModel });

                        var doesContainsGreenListResult = filesByDroid.Where(item
                            => extensionData.Select(green
                                => green.Extension.ToUpper()).ToList().Contains(item.Extension.ToUpper())).ToList();
                        
                        OnTrigger(new PreingestEventArgs { Description = "Filter the content.", Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Executing, PreingestAction = eventModel });

                        var doNotcontainsGreenListResult = filesByDroid.Except(doesContainsGreenListResult).ToList();

                        OnTrigger(new PreingestEventArgs { Description = "Compare both lists.", Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Executing, PreingestAction = eventModel });

                        var endResult = doesContainsGreenListResult.Select(item
                            => new { item.SessionId, item.Puid, item.Name, item.Location, item.FormatVersion, item.FormatName, item.Extension, InGreenList = true })
                            .Concat(doNotcontainsGreenListResult.Select(item
                            => new { item.SessionId, item.Puid, item.Name, item.Location, item.FormatVersion, item.FormatName, item.Extension, InGreenList = false }))
                            .ToList();

                        OnTrigger(new PreingestEventArgs { Description = "Done comparing both lists.", Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Executing, PreingestAction = eventModel });
                        
                        eventModel.Summary.Accepted = doesContainsGreenListResult.Count();
                        eventModel.Summary.Rejected = doNotcontainsGreenListResult.Count();
                        
                        eventModel.ActionData = endResult.ToArray();
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
    }
}