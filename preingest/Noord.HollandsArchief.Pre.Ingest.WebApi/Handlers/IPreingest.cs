using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Event;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Handlers
{
    public interface IPreingest
    {
        AppSettings ApplicationSettings { get; }
        void Execute();
        Guid SessionGuid { get; }
        Guid ActionProcessId { get; set; }
        void SetSessionGuid(Guid guid);
        ILogger Logger { get; set; }
        public String TarFilename { get; set; }
        public String TargetCollection { get; }
        public String TargetFolder { get; }
        Guid AddProcessAction(String name, String description, String result);
        void UpdateProcessAction(Guid actionId, String result, String summary);
        void AddStartState(Guid processId);
        void AddCompleteState(Guid processId);
        void AddFailedState(Guid processId, string message);
        void ValidateAction();
        void Trigger(object sender, PreingestEventArgs e);
    }
}
