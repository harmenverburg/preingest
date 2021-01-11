using Microsoft.Extensions.Logging;

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Collections.Generic;

using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Event;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Handlers
{
    //Check 5
    public class SipCreatorHandler : AbstractPreingestHandler
    {
        public SipCreatorHandler(AppSettings settings) : base(settings)  {  }

        private String GetProcessingUrl(string servername, string port, string folder)
        {
            //TODO CHANGE URL (OOK bij EXCEL) http://xslwebhost:port/0ee4629b-3394-6986-b859-430c0256ecd1/Provincie%20Noord%Holland
            return String.Format(@"http://{0}:{1}/sipcreator?reluri={2}", servername, port, folder);
        }

        public override void Execute()
        {
            base.Execute();

            var eventModel = CurrentActionProperties(TargetCollection, this.GetType().Name);
            OnTrigger(new PreingestEventArgs () { Initiate = DateTime.Now, ActionType = PreingestActionStates.Started, PreingestAction = eventModel });

            var anyMessages = new List<String>();
            bool isSucces = false;

            try
            {
                var archive = new DirectoryInfo(TargetFolder).GetDirectories().FirstOrDefault();
                if (archive == null)
                {
                    Logger.LogInformation("Sip Creator : In '{0}' zijn geen onderliggende mappen gevonden. Minimaal 1 verwacht.", TargetFolder);
                    throw new DirectoryNotFoundException(String.Format("Sip Creator : In '{0}' zijn geen onderliggende mappen gevonden. Minimaal 1 verwacht.", TargetFolder));
                }

                Logger.LogInformation("Sip Creator : Gevonden map '{0}'.", archive.Name);
                string requestUri = GetProcessingUrl(ApplicationSettings.XslWebServerName, ApplicationSettings.XslWebServerPort, archive.Name);

                eventModel.Summary.Processed = 1;
                using (HttpClient client = new HttpClient())
                {
                    OnTrigger(new PreingestEventArgs { Initiate = DateTime.Now, ActionType = PreingestActionStates.Executing, PreingestAction = eventModel });
                    var httpResponse = client.GetAsync(requestUri).Result;

                    if (!httpResponse.IsSuccessStatusCode)
                        throw new Exception("Failed to request data!");

                    //var rootError = JsonConvert.DeserializeObject<Root>(httpResponse.Content.ReadAsStringAsync().Result);

                    //if (rootError == null)
                    //    throw new ApplicationException("Sip Creator request failed!");

                    //if (rootError.SchematronValidationReport != null && rootError.SchematronValidationReport.errors != null
                    //    && rootError.SchematronValidationReport.errors.Count > 0)
                    //{
                    //    //TODO TESTEN
                    //    var result = rootError.SchematronValidationReport.errors.SelectMany(item => new string[] { item.message, item.FailedAssertLocation, item.FiredRuleContext, item.FailedAssertTest }).ToArray();

                    //    eventModel.Summary.Accepted = 0;
                    //    eventModel.Summary.Rejected = 1;
                    //    eventModel.ActionResult.ResultName = PreingestActionResults.Error;
                    //    eventModel.ActionData = result;
                    //}
                    //else
                    //{
                    //    eventModel.Summary.Accepted = 1;
                    //    eventModel.Summary.Rejected = 0;
                    //    eventModel.ActionResult.ResultName = PreingestActionResults.Success;
                    //}
                }

                isSucces = true;
            }
            catch (Exception e)
            {
                isSucces = false;
                Logger.LogError(e, "An exception occured in SIP request!");
                anyMessages.Clear();
                anyMessages.Add("An exception occured in SIP request!");
                anyMessages.Add(e.Message);
                anyMessages.Add(e.StackTrace);

                eventModel.Summary.Processed = -1;
                eventModel.Summary.Accepted = -1;
                eventModel.Summary.Rejected = eventModel.Summary.Processed;

                eventModel.ActionResult.ResultName = PreingestActionResults.Failed;
                eventModel.Properties.Messages = anyMessages.ToArray();

                OnTrigger(new PreingestEventArgs { Initiate = DateTime.Now, ActionType = PreingestActionStates.Failed, PreingestAction = eventModel });
            }
            finally
            {
                if (isSucces)
                    OnTrigger(new PreingestEventArgs { Initiate = DateTime.Now, ActionType = PreingestActionStates.Completed, PreingestAction = eventModel });
            }
        }
    }
}
