using Microsoft.AspNetCore.SignalR;

using System;
using System.Threading.Tasks;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.EventHub
{
    public interface IEventHub
    {
        // here place some method(s) for message from server to client
        Task SendNoticeEventToClient(string message);
        Task CollectionsStatus(string jsonData);
        Task CollectionStatus(Guid guid, string jsonData);
    }
       
    public class PreingestEventHub : Hub<IEventHub>
    {    }
}
