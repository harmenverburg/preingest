using Microsoft.AspNetCore.SignalR;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using System;
using System.Threading.Tasks;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.EventHub
{
    public interface IEventHub
    {
        // here place some method(s) for message from server to client
        Task SendNoticeEventToClient(string message);
    }
       
    public class PreingestEventHub : Hub<IEventHub>
    {

    }
}
