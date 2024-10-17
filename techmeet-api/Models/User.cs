using Microsoft.AspNetCore.Identity;

namespace techmeet_api.Models
{
    public class User : IdentityUser
    {
        public string? Nickname { get; set; }
        public DateTime? VIPExpirationDate { get; set; }
        public string? ProfilePhotoUrl { get; set; }

        // add a navigation property to the Event class
        public virtual ICollection<Event>? Events { get; set; }
        public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
        public virtual ICollection<Waitlist>? Waitlists { get; set; }
        public virtual ICollection<Notification>? Notifications { get; set; }
        public virtual ICollection<ChatMessage>? MessagesSent { get; set; }
        public virtual ICollection<ChatMessage>? MessagesReceived { get; set; }

    }
}