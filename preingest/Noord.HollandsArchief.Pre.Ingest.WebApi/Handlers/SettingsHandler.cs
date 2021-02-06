using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using Noord.HollandsArchief.Pre.Ingest.WebApi.EventHub;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Event;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Handler;

using System;
using System.Collections.Generic;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Handlers
{
    public class SettingsHandler : AbstractPreingestHandler, IDisposable
    {
        public SettingsHandler(AppSettings settings, IHubContext<PreingestEventHub> eventHub, CollectionHandler preingestCollection) : base(settings, eventHub, preingestCollection)
        {
            this.PreingestEvents += Trigger;
        }

        public BodySettings CurrentSettings { get; set; }

        public void Dispose()
        {
            this.PreingestEvents -= Trigger;
        }

        public override void Execute()
        {
            var eventModel = CurrentActionProperties(TargetCollection, this.GetType().Name);
            OnTrigger(new PreingestEventArgs { Description = String.Format("Saving settings for folder '{0}'.", SessionGuid), Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Started, PreingestAction = eventModel });
           
            bool isSuccess = false;
            var anyMessages = new List<String>();

            try
            {
                if (CurrentSettings == null)
                    throw new ApplicationException("Settings is null!");

                eventModel.ActionData = CurrentSettings;
                eventModel.ActionResult.ResultValue = PreingestActionResults.Success;
                eventModel.Summary.Processed = 1;
                eventModel.Summary.Accepted = 1;
                eventModel.Summary.Rejected = 0;

                isSuccess = true;
            }
            catch (Exception e)
            {
                isSuccess = false;
                anyMessages.Clear();
                anyMessages.Add(String.Format("Saving settings for folder '{0}' failed!", TargetCollection));
                anyMessages.Add(e.Message);
                anyMessages.Add(e.StackTrace);

                Logger.LogError(e, "Saving settings for folder '{0}' failed!", TargetCollection);

                eventModel.ActionResult.ResultValue = PreingestActionResults.Failed;
                eventModel.Properties.Messages = null;
                eventModel.Summary.Processed = 1;
                eventModel.Summary.Accepted = 0;
                eventModel.Summary.Rejected = 1;

                OnTrigger(new PreingestEventArgs { Description = "An exception occured while saving settings!", Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Failed, PreingestAction = eventModel });
            }
            finally
            {
                if( isSuccess)
                    OnTrigger(new PreingestEventArgs { Description = "Saving settings is done.", Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Completed, PreingestAction = eventModel });
            }
        }
    }
}
