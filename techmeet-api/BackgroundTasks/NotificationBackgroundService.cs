namespace techmeet_api.BackgroundTasks
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using techmeet_api.Data;
    using techmeet_api.Models;
    using Microsoft.Extensions.Logging;
    using Microsoft.AspNetCore.SignalR;
    using techmeet_api.Hubs;


    public class NotificationBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<NotificationBackgroundService> _logger;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationBackgroundService(IServiceScopeFactory serviceScopeFactory, ILogger<NotificationBackgroundService> logger, IHubContext<NotificationHub> hubContext)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Running notification generation task...");
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    await GenerateNotificationsAsync(context);
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }


        public class NotificationSendObject
        {
            public int Id { get; set; }
            public string UserId { get; set; }
            public string Message { get; set; }
            public string Type { get; set; }
            public bool IsRead { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        private async Task GenerateNotificationsAsync(ApplicationDbContext context)
        {
            var users = await context.Users.Include(u => u.Attendances).Include(a => a.Events).ToListAsync();

            foreach (var user in users)
            {
                if (user.VIPExpirationDate != null && user.VIPExpirationDate <= DateTime.UtcNow.AddDays(30))
                {
                    // Check if the same notification already exists
                    var existingNotification = await context.Notifications.AnyAsync(n => n.UserId == user.Id && n.Type == "membership_expiry");

                    if (!existingNotification)
                    {

                        var notification = new Notification
                        {
                            UserId = user.Id,
                            Message = "Your VIP membership is expiring in 30 days!",
                            Type = "membership_expiry",
                            CreatedAt = DateTime.UtcNow,
                            IsRead = false
                        };

                        context.Notifications.Add(notification);
                        await context.SaveChangesAsync();

                        // Reload the notification from the database to get any changes, such as the ID
                        context.Entry(notification).Reload();
                        Console.WriteLine($"Notification ID: {notification.Id}");

                        await _hubContext.Clients.User(user.Id).SendAsync("ReceiveNotification", new NotificationSendObject
                        {
                            Id = notification.Id,
                            UserId = notification.UserId,
                            Message = notification.Message,
                            Type = notification.Type,
                            IsRead = notification.IsRead,
                            CreatedAt = notification.CreatedAt
                        });
                    }
                }

                foreach (var attendance in user.Attendances)
                {
                    if (attendance.Event != null && attendance.Event.StartTime <= DateTime.UtcNow.AddHours(3))
                    {
                        // Check if the same notification already exists
                        var existingNotification = await context.Notifications.AnyAsync(n => n.UserId == user.Id && n.EventId == attendance.EventId && n.Type == "event_upcoming");

                        if (!existingNotification)
                        {

                            var notification = new Notification
                            {
                                UserId = user.Id,
                                EventId = attendance.EventId,
                                Message = $"\"{attendance.Event.Title}\" is starting in 3 hours!",
                                Type = "event_upcoming",
                                CreatedAt = DateTime.UtcNow,
                                IsRead = false
                            };

                            context.Notifications.Add(notification);
                            await context.SaveChangesAsync();

                            // Reload the notification from the database to get any changes, such as the ID
                            context.Entry(notification).Reload();

                            await _hubContext.Clients.User(user.Id).SendAsync("ReceiveNotification", new NotificationSendObject
                            {
                                Id = notification.Id,
                                UserId = notification.UserId,
                                Message = notification.Message,
                                Type = notification.Type,
                                IsRead = notification.IsRead,
                                CreatedAt = notification.CreatedAt
                            });
                        }
                    }
                }
            }
        }
    }
}