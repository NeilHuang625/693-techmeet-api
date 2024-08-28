using Microsoft.EntityFrameworkCore;
using techmeet_api.Data;

namespace techmeet_api.Repositories
{
    public class JwtBlacklistService : IJwtBlacklistService
    {
        private readonly ApplicationDbContext _context;
        public JwtBlacklistService(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<bool> IsTokenBlacklisted(string jwt)
        {
            return await _context.RevokedTokens.AnyAsync(r => r.Token == jwt);
        }
    }
}