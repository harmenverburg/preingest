using Microsoft.Extensions.Logging;

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Noord.HollandsArchief.Pre.Ingest.Utilities;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Event;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Handler;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Handlers
{
    public class EncodingHandler : AbstractPreingestHandler
    {
        public EncodingHandler(AppSettings settings) : base(settings) { }

        public override void Execute()
        {
            var anyMessages = new List<String>();
            bool isSucces = false;
            var eventModel = CurrentActionProperties(TargetCollection, this.GetType().Name);

            OnTrigger(new PreingestEventArgs { Description = "Start encoding check on all metadata files.", Initiate = DateTime.Now, ActionType = PreingestActionStates.Started, PreingestAction = eventModel });

            try
            {
                var data = new List<EncodingItem>();

                string[] metadatas = Directory.GetFiles(TargetFolder, "*.metadata", SearchOption.AllDirectories);
                eventModel.Summary.Processed = metadatas.Count();

                Encoding bom = null;
                Encoding stream = null;
                String xml = string.Empty;
                bool isUtf8Bom = false;
                bool isUtf8Stream = false;
                bool isUtf8Xml = false;

                foreach (string file in metadatas)
                {
                    Logger.LogInformation("Get encoding from file : '{0}'", file);

                    bom = EncodingHelper.GetEncodingByBom(file);
                    stream = EncodingHelper.GetEncodingByStream(file);
                    xml = EncodingHelper.GetXmlEncoding(File.ReadAllText(file));

                    isUtf8Bom = (bom.EncodingName.ToUpperInvariant().Contains("UTF-8") || bom.EncodingName.ToUpperInvariant().Contains("UTF8"));
                    isUtf8Stream = (stream.EncodingName.ToUpperInvariant().Contains("UTF-8") || stream.EncodingName.ToUpperInvariant().Contains("UTF8"));
                    isUtf8Xml = (xml.ToUpperInvariant().Equals("UTF-8") || xml.ToUpperInvariant().Equals("UTF8"));

                    data.Add(new EncodingItem
                    {
                        IsUtf8 = (isUtf8Bom && isUtf8Stream && isUtf8Xml),
                        MetadataFile = file,
                        Description = String.Format("Byte Order Mark : {0}, Stream : {1}, XML : {2}",
                    (bom != null) ? bom.EncodingName : "Byte Order Mark niet gevonden",
                    (stream != null) ? stream.EncodingName : "In stream niet gevonden",
                    String.IsNullOrEmpty(xml) ? "In XML niet gevonden" : xml)
                    });

                    OnTrigger(new PreingestEventArgs { Description = String.Format("Running encoding check on '{0}'", file), Initiate = DateTime.Now, ActionType = PreingestActionStates.Executing, PreingestAction = eventModel });
                }

                eventModel.Summary.Accepted = data.Where(item => item.IsUtf8).Count();
                eventModel.Summary.Rejected = data.Where(item => !item.IsUtf8).Count();

                eventModel.ActionData = data.ToArray();

                if (eventModel.Summary.Rejected > 0)
                    eventModel.ActionResult.ResultValue = PreingestActionResults.Error;
                else
                    eventModel.ActionResult.ResultValue = PreingestActionResults.Success;

                isSucces = true;
            }
            catch (Exception e)
            {
                isSucces = false;
                Logger.LogError(e, "An exception occured in get encoding from file!");
                anyMessages.Clear();
                anyMessages.Add("An exception occured in get encoding from file!");
                anyMessages.Add(e.Message);
                anyMessages.Add(e.StackTrace);

                //eventModel.Summary.Processed = -1;
                eventModel.Summary.Accepted = 0;
                eventModel.Summary.Rejected = eventModel.Summary.Processed;

                eventModel.ActionResult.ResultValue = PreingestActionResults.Failed;
                eventModel.Properties.Messages = anyMessages.ToArray();

                OnTrigger(new PreingestEventArgs { Description = "An exception occured in get encoding from file!", Initiate = DateTime.Now, ActionType = PreingestActionStates.Failed, PreingestAction = eventModel });
            }
            finally
            {
                if (isSucces)
                    OnTrigger(new PreingestEventArgs { Description = "Retrieving encoding on metadata files is done.", Initiate = DateTime.Now, ActionType = PreingestActionStates.Completed, PreingestAction = eventModel });
            }
        }
    }
}
