﻿using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;

using System;
using System.IO;
using System.Collections.Generic;

using Noord.HollandsArchief.Pre.Ingest.Utilities;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using Noord.HollandsArchief.Pre.Ingest.WebApi.EventHub;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Event;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Handlers
{
    public class ContainerChecksumHandler : AbstractPreingestHandler, IDisposable
    {
        public ContainerChecksumHandler(AppSettings settings, IHubContext<PreingestEventHub> eventHub, CollectionHandler preingestCollection) : base(settings, eventHub, preingestCollection)
        {
            this.PreingestEvents += Trigger;
        }
        public String Checksum { get; set; }
        public String DeliveredChecksumValue { get; set; }
        public void Dispose()
        {
            this.PreingestEvents -= Trigger;
        }
        public override void Execute()
        {
            Logger.LogInformation("Calculate checksum for file : '{0}'", TargetCollection);

            var eventModel = CurrentActionProperties(TargetCollection, this.GetType().Name);
            OnTrigger(new PreingestEventArgs { Description = String.Format("Start calculate checksum for container '{0}'.", TargetCollection), Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Started, PreingestAction = eventModel });

            var anyMessages = new List<String>();
            string currentCalculation = string.Empty;
            bool isSuccess = false;
            try
            {
                if (!File.Exists(TargetCollection))
                    throw new FileNotFoundException(String.Format("Collection not found '{0}'!", TargetCollection));

                switch (Checksum.ToUpperInvariant())
                {
                    case "MD5":
                        OnTrigger(new PreingestEventArgs { Description = String.Format("Calculate checksum for container '{0}' with MD5.", TargetCollection), Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Executing, PreingestAction = eventModel });
                        currentCalculation = ChecksumHelper.CreateMD5Checksum(new FileInfo(TargetCollection));
                        break;
                    case "SHA1":
                    case "SHA-1":
                        OnTrigger(new PreingestEventArgs { Description = String.Format("Start calculate checksum for container '{0}' with SHA1.", TargetCollection), Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Executing, PreingestAction = eventModel });
                        currentCalculation = ChecksumHelper.CreateSHA1Checksum(new FileInfo(TargetCollection));
                        break;
                    case "SHA256":
                    case "SHA-256":
                        OnTrigger(new PreingestEventArgs { Description = String.Format("Start calculate checksum for container '{0}' with SHA256.", TargetCollection), Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Executing, PreingestAction = eventModel });
                        currentCalculation = ChecksumHelper.CreateSHA256Checksum(new FileInfo(TargetCollection));
                        break;
                    case "SHA512":
                    case "SHA-512":
                        OnTrigger(new PreingestEventArgs { Description = String.Format("Start calculate checksum for container '{0}' with SHA512.", TargetCollection), Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Executing, PreingestAction = eventModel });
                        currentCalculation = ChecksumHelper.CreateSHA512Checksum(new FileInfo(TargetCollection));
                        break;
                    default:
                        {
                            anyMessages.Add(String.Format("Checksum {0} not defined. No calculation available.", Checksum));
                            Logger.LogWarning(String.Format("Checksum {0} not defined. No calculation available.", Checksum));
                        }
                        break;
                }
                //failed, no value
                if (String.IsNullOrEmpty(currentCalculation))
                    throw new ApplicationException("Calculation returned nothing or empty value!");

                var fileInformation = new FileInfo(TargetCollection);
                anyMessages.Add(String.Concat("Name : ", fileInformation.Name));
                anyMessages.Add(String.Concat("Extension : ", fileInformation.Extension));
                anyMessages.Add(String.Concat("Size : ", fileInformation.Length));
                anyMessages.Add(String.Concat("CreationTime : ", fileInformation.CreationTimeUtc));
                anyMessages.Add(String.Concat("LastAccessTime : ", fileInformation.LastAccessTimeUtc));
                anyMessages.Add(String.Concat("LastWriteTime : ", fileInformation.LastWriteTimeUtc));
                eventModel.Properties.Messages = anyMessages.ToArray();

                var data = new List<String>();
                data.Add(Checksum);
                data.Add(currentCalculation);

                isSuccess = !String.IsNullOrEmpty(currentCalculation);

                eventModel.ActionResult.ResultValue = PreingestActionResults.Success;
                eventModel.Summary.Processed = 1;
                eventModel.Summary.Accepted = 1;
                eventModel.Summary.Rejected = 0;

                if (!String.IsNullOrEmpty(DeliveredChecksumValue))
                {
                    bool isTheSame = DeliveredChecksumValue.Equals(currentCalculation, StringComparison.InvariantCultureIgnoreCase);                    
                    data.Add(String.Format("{0} {1} {2}", DeliveredChecksumValue, isTheSame ? "=" : "≠", currentCalculation));

                    eventModel.Properties.Messages = anyMessages.ToArray();
                    if (!isTheSame)
                    {
                        eventModel.ActionResult.ResultValue = PreingestActionResults.Error;
                        eventModel.Summary.Processed = 1;
                        eventModel.Summary.Accepted = 0;
                        eventModel.Summary.Rejected = 1;
                    }
                }

                eventModel.ActionData = data.ToArray();
            }
            catch (Exception e)
            {
                isSuccess = false;

                anyMessages.Clear();
                anyMessages.Add(String.Format("Calculation checksum from file : '{0}' failed!", TargetCollection));
                anyMessages.Add(e.Message);
                anyMessages.Add(e.StackTrace);

                Logger.LogError(e, "Calculation checksum from file : '{0}' failed!", TargetCollection);

                eventModel.Properties.Messages = anyMessages.ToArray();
                eventModel.ActionResult.ResultValue = PreingestActionResults.Failed;
                eventModel.Summary.Processed = -1;
                eventModel.Summary.Accepted = -1;
                eventModel.Summary.Rejected = -1;

                OnTrigger(new PreingestEventArgs { Description = "An exception occured while calculating the checksum!", Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Failed, PreingestAction = eventModel });
            }
            finally
            {
                if (isSuccess)
                    OnTrigger(new PreingestEventArgs { Description = "Checksum calculation is done.", Initiate = DateTimeOffset.Now, ActionType = PreingestActionStates.Completed, PreingestAction = eventModel });
            }
        }
    }
}
