using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RagChatbotSystem.Business.DTOs;
using RagChatbotSystem.Business.Interfaces;
using RagChatbotSystem.DataAccess.Repositories;
using RagChatbotSystem.DataAccess.Models;

namespace RagChatbotSystem.Business.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGenericRepository<User> _userRepository;

        public UserService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _userRepository = _unitOfWork.Repository<User>();
        }

        public async Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
        {
            return await _userRepository.GetQueryable()
                .AsNoTracking()
                .OrderBy(u => u.FullName)
                .Select(u => new UserDto(
                    u.UserId,
                    u.FullName,
                    u.Email,
                    u.Role,
                    u.CreatedAt,
                    u.IsApproved))
                .ToListAsync(cancellationToken);
        }

        public async Task<UserDto?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _userRepository.GetQueryable()
                .AsNoTracking()
                .Where(u => u.UserId == userId)
                .Select(u => new UserDto(
                    u.UserId,
                    u.FullName,
                    u.Email,
                    u.Role,
                    u.CreatedAt,
                    u.IsApproved))
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<UserDto> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.FullName))
            {
                throw new ArgumentException("Full name is required.", nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                throw new ArgumentException("Email is required.", nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                throw new ArgumentException("Password is required.", nameof(request));
            }

            var email = request.Email.Trim().ToLower();
            var emailExists = await _userRepository.GetQueryable().AnyAsync(u => u.Email.ToLower() == email, cancellationToken);
            if (emailExists)
            {
                throw new InvalidOperationException("A user with this email already exists.");
            }

            var roleName = string.IsNullOrWhiteSpace(request.Role) ? "Student" : request.Role.Trim();
            var user = new User
            {
                UserId = Guid.NewGuid(),
                FullName = request.FullName.Trim(),
                Email = request.Email.Trim(),
                PasswordHash = Helpers.PasswordHasherHelper.HashPassword(request.Password),
                Role = roleName,
                CreatedAt = DateTime.UtcNow,
                IsApproved = roleName != "Teacher" // Chỉ Giáo viên mới phải chờ duyệt, học sinh và admin thì được duyệt ngay
            };

            await _userRepository.AddAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ToDto(user);
        }

        public async Task<UserDto?> AuthenticateUserAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            var normalizedEmail = email.Trim().ToLower();
            var user = await _userRepository.GetQueryable()
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);

            if (user == null)
            {
                return null;
            }

            var isValid = Helpers.PasswordHasherHelper.VerifyPassword(password, user.PasswordHash);
            if (!isValid)
            {
                return null;
            }

            if (!user.IsApproved)
            {
                throw new InvalidOperationException("Tài khoản giáo viên của bạn đang chờ Admin phê duyệt.");
            }

            return ToDto(user);
        }

        public async Task<bool> ApproveUserAsync(Guid userId, bool approve, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetQueryable()
                .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
            if (user == null)
            {
                return false;
            }
            user.IsApproved = approve;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }

        private static UserDto ToDto(User user)
        {
            return new UserDto(user.UserId, user.FullName, user.Email, user.Role, user.CreatedAt, user.IsApproved);
        }
    }
}

