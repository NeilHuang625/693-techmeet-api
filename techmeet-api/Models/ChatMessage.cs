namespace techmeet_api.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public string? SenderId { get; set; }
        public string? ReceiverId { get; set; }
        public string? Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }

        public User? Receiver { get; set; }
        public User? Sender { get; set; }

    }
}