using System.ComponentModel.DataAnnotations.Schema;

namespace techmeet_api.Models
{
    public class Event
    {
        [NotMapped]
        public IFormFile ImageFile { get; set; }
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public string City { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? ImageUrl { get; set; }
        public int MaxAttendees { get; set; }
        public int? CurrentAttendees { get; set; }
        public bool Promoted { get; set; }
        public string UserId { get; set; }
        public int CategoryId { get; set; }

        // add a navigation property to the User class
        public User User { get; set; }
        public Category Category { get; set; }
        public virtual ICollection<Attendance> Attendances { get; set; }
        public virtual ICollection<Waitlist> Waitlists { get; set; }
        public virtual ICollection<Notification> Notifications { get; set; }

    }
}