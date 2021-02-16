using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;

using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;

using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using Noord.HollandsArchief.Pre.Ingest.WebApi.EventHub;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Event;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Handler;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Handlers
{
    //Check 5
    public class TransformationHandler : AbstractPreingestHandler, IDisposable
    {
        public TransformationHandler(AppSettings settings, IHubContext<PreingestEventHub> eventHub, CollectionHandler preingestCollection) : base(settings, eventHub, preingestCollection)
        {
            this.PreingestEvents += Trigger;
        }
        private String GetProcessingUrl(string servername, string port, string pad)
        {
            string reluri = pad.Remove(0, "/data/".Length);
            return String.Format(@"http://{0}:{1}/transform/topx2xip/{2}", servername, port, reluri);
        }
        public override void Execute()
        {
            var eventModel = CurrentActionProperties(TargetCollection, this.GetType().Name);
            OnTrigger(new PreingestEventArgs { Description = "Start transforming *.metadata files to *.xip files", Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Started, PreingestAction = eventModel });

            var anyMessages = new List<String>();
            bool isSucces = false;
            var transformation = new List<TransformationItem>();
            try
            {
                string[] metadatas = Directory.GetFiles(TargetFolder, "*.metadata", SearchOption.AllDirectories);
                eventModel.Summary.Processed = metadatas.Count();

                foreach (string file in metadatas)
                {
                    Logger.LogInformation("Transformatie : {0}", file);
                    string requestUri = GetProcessingUrl(ApplicationSettings.XslWebServerName, ApplicationSettings.XslWebServerPort, System.Net.WebUtility.UrlEncode(file));
                    try
                    {
                        WebRequest request = WebRequest.Create(requestUri);
                        using (WebResponse response = request.GetResponseAsync().Result)
                        {
                            XDocument xDoc = XDocument.Load(response.GetResponseStream());

                            if (xDoc.Root.Name.Equals("message"))
                            {
                                transformation.Add(new TransformationItem { IsTranformed = false, MetadataFilename = file, RequestUri = requestUri, ErrorMessage = new string[] { String.Format("XIP transformatie niet gelukt voor '{0}'. Antwoord: {1}", requestUri, xDoc.ToString()) } });
                            }
                            else
                            {
                                //bye bye old xip (to be sure)
                                if (File.Exists(String.Concat(file, ".xip")))
                                    File.Delete(String.Concat(file, ".xip"));
                                
                                //hello xip
                                xDoc.Save(String.Concat(file, ".xip"));

                                //bye bye metadata
                                if (File.Exists(file))
                                    File.Delete(file);

                                //welcome new metadata
                                File.Move(String.Concat(file, ".xip"), String.Concat(file), true);

                                transformation.Add(new TransformationItem { IsTranformed = true, MetadataFilename = file, RequestUri = requestUri, ErrorMessage = new string[0] });
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        var errorMessages = new List<String>();
                        Logger.LogError(e, String.Format("Exception occured in XIP transformation with request '{0}' for metadata file '{1}'!", requestUri, file));
                        
                        errorMessages.Add(String.Format("Exception occured in XIP transformation with request '{0}' for metadata file '{1}'!", requestUri, file));
                        errorMessages.Add(e.Message);
                        errorMessages.Add(e.StackTrace);

                        //error
                        transformation.Add(new TransformationItem
                        {
                            IsTranformed = false,
                            ErrorMessage = errorMessages.ToArray(),
                            MetadataFilename = file,
                            RequestUri = requestUri
                        });
                    }

                    OnTrigger(new PreingestEventArgs { Description = String.Format("Processing file '{0}'", file), Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Executing, PreingestAction = eventModel });
                }

                eventModel.Summary.Accepted = transformation.Where(item => item.IsTranformed).Count();
                eventModel.Summary.Rejected = transformation.Where(item => !item.IsTranformed).Count();

                eventModel.ActionData = transformation.ToArray();

                if (eventModel.Summary.Rejected > 0)
                    eventModel.ActionResult.ResultValue = PreingestActionResults.Error;
                else
                    eventModel.ActionResult.ResultValue = PreingestActionResults.Success;

                isSucces = true;
            }
            catch(Exception e)
            {
                isSucces = false;

                Logger.LogError(e, "An exception occured in metadata transformation!");
                anyMessages.Clear();
                anyMessages.Add("An exception occured in metadata transformation!");
                anyMessages.Add(e.Message);
                anyMessages.Add(e.StackTrace);

                //eventModel.Summary.Processed = -1;
                eventModel.Summary.Accepted = 0;
                eventModel.Summary.Rejected = eventModel.Summary.Processed;

                eventModel.ActionResult.ResultValue = PreingestActionResults.Failed;
                eventModel.Properties.Messages = anyMessages.ToArray();

                OnTrigger(new PreingestEventArgs { Description = "An exception occured in metadata transformation!", Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Failed, PreingestAction = eventModel });
            }
            finally
            {
                if (isSucces)
                    OnTrigger(new PreingestEventArgs { Description="Transformation is done.", Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Completed, PreingestAction = eventModel });
            }           
        }
        public void Dispose()
        {
            this.PreingestEvents -= Trigger;
        }
    }
}
