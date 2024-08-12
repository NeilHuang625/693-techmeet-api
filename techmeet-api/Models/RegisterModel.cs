namespace techmeet_api.Models
{
    public class RegisterModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string? Nickname { get; set; }
    }
}