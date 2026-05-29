using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RagChatbotSystem.Business.Interfaces;
using RagChatbotSystem.DataAccess.Data;
using RagChatbotSystem.DataAccess.Models;

namespace RagChatbotSystem.Business.Services
{
    public class AccountService : IAccountService
    {
        private const string DefaultRole = "user";
        private readonly AppDbContext _context;

        public AccountService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User> FindOrCreateGoogleUserAsync(string email, string fullName)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Email is required.", nameof(email));
            }

            var normalizedEmail = email.Trim().ToLowerInvariant();
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (user != null)
            {
                return user;
            }

            user = new User
            {
                UserId = Guid.NewGuid(),
                Email = normalizedEmail,
                FullName = string.IsNullOrWhiteSpace(fullName) ? normalizedEmail : fullName.Trim(),
                Role = DefaultRole,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }

        public Task<User?> GetUserByIdAsync(Guid userId)
        {
            return _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        }
    }
}
