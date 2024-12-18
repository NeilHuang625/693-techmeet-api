namespace techmeet_api.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string? Message { get; set; }
        public string? Type { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? EventId { get; set; }
        public User? User { get; set; }
        public Event? Event { get; set; }
    }
}