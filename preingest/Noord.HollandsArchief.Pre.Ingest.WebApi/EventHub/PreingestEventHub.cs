using Microsoft.AspNetCore.SignalR;

using System;
using System.Threading.Tasks;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.EventHub
{
    public interface IEventHub
    {
        // here place some method(s) for message from server to client
        Task SendNoticeEventToClient(string message);
        Task StartWorker(string message);
        Task RunNext(string message);
        Task SendCollectionsStatus(string message);
        Task SendCollectionStatus(Guid guid, string message);
    }
       
    public class PreingestEventHub : Hub<IEventHub>
    {    }
}
