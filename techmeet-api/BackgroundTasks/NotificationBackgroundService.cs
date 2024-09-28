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

    public class NotificationBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<NotificationBackgroundService> _logger;

        public NotificationBackgroundService(IServiceScopeFactory serviceScopeFactory, ILogger<NotificationBackgroundService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
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
                                Message = $"Event {attendance.Event.Title} is starting in less than 3 hours!",
                                Type = "event_upcoming",
                                CreatedAt = DateTime.UtcNow,
                                IsRead = false
                            };

                            context.Notifications.Add(notification);
                        }
                    }
                }
            }

            await context.SaveChangesAsync();
        }
    }
}