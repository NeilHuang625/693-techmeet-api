namespace techmeet_api.Repositories
{
    public interface IJwtBlacklistService
    {
        Task<bool> IsTokenBlacklisted(string jwt);
    }
}