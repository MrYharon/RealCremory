using Microsoft.AspNetCore.SignalR;

namespace Cremory.API.Hubs
{
    public class OrderHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }
    }
}

