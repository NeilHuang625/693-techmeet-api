using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace techmeet_api.Hubs
{

    [Authorize]
    public class NotificationHub : Hub
    {
        public class Notification
        {
            public int Id { get; set; }
            public string? Type { get; set; }
            public string? Message { get; set; }
            public bool IsRead { get; set; }
            public DateTime CreatedAt { get; set; }

        }
        public async Task SendNotification(Notification notification)
        {
            var userId = Context.UserIdentifier;
            if (userId == null) return;
            await Clients.User(userId).SendAsync("ReceiveNotification", notification);
        }
    }
}