namespace techmeet_api.Models
{
    public class Waitlist
    {
        public string? UserId { get; set; }
        public int? EventId { get; set; }
        public DateTime AddedAt { get; set; }
        public User? User { get; set; }
        public Event? Event { get; set; }
    }
}