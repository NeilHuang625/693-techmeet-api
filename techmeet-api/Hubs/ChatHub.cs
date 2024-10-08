namespace techmeet_api.Hubs
{
    using Microsoft.AspNetCore.SignalR;
    using Microsoft.AspNetCore.Authorization;
    using System.Threading.Tasks;

    [Authorize]
    public class ChatHub : Hub
    {
        public async Task SendMessageToUser(string userId, string message)
        {
            await Clients.User(userId).SendAsync("ReceiveMessage", message);
        }
    }
}