using ChattingApplicationProject.Data;
using ChattingApplicationProject.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChattingApplicationProject.Services
{
    public class UserCleanupService : IUserCleanupService
    {
        private readonly DataContext _context;

        public UserCleanupService(DataContext context)
        {
            _context = context;
        }

        public async Task<int> CleanupExpiredUnconfirmedUsersAsync()
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-7); // Delete users unconfirmed for 7+ days

            var expiredUsers = await _context
                .Users.Where(u =>
                    !u.EmailConfirmed
                    && u.Created < cutoffDate
                    && u.EmailConfirmationTokenExpiry < DateTime.UtcNow
                )
                .ToListAsync();

            if (expiredUsers.Any())
            {
                _context.Users.RemoveRange(expiredUsers);
                await _context.SaveChangesAsync();
            }

            return expiredUsers.Count;
        }

        public async Task<int> CleanupExpiredPasswordResetTokensAsync()
        {
            var expiredTokens = await _context
                .Users.Where(u => u.PasswordResetTokenExpiry < DateTime.UtcNow)
                .ToListAsync();

            foreach (var user in expiredTokens)
            {
                user.PasswordResetToken = null;
                user.PasswordResetTokenExpiry = null;
            }

            if (expiredTokens.Any())
            {
                await _context.SaveChangesAsync();
            }

            return expiredTokens.Count;
        }
    }
}
