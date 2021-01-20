using Microsoft.AspNetCore.SignalR;

using System.Threading.Tasks;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.EventHub
{
    public interface IEventHub
    {
        // here place some method(s) for message from server to client
        Task SendNoticeEventToClient(string message);
        Task PushInQueue(string message);
    }
       
    public class PreingestEventHub : Hub<IEventHub>
    {    }
}
