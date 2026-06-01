using System;

namespace RagChatbotSystem.Business.DTOs
{
    public sealed record UserDto(
        Guid UserId,
        string FullName,
        string Email,
        string Role,
        DateTime CreatedAt,
        bool IsApproved);

    public sealed record CreateUserRequest(
        string FullName,
        string Email,
        string? Role,
        string Password);

}
