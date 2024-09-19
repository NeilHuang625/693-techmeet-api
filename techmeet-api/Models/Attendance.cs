namespace techmeet_api.Models
{
    public class Attendance
    {
        public string UserId { get; set; }
        public int EventId { get; set; }

        public DateTime AttendedAt { get; set; }

        public User User { get; set; }
        public Event Event { get; set; }
    }
}