using System;

namespace RagChatbotSystem.Business.DTOs
{
    public sealed record DatasetDto(
        Guid DatasetId,
        string Name,
        string? Description,
        Guid CreatedBy,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        int DocumentCount,
        bool IsPublic,
        bool IsApproved);

    public sealed record CreateDatasetRequest(
        string Name,
        string? Description,
        Guid CreatedBy,
        bool IsPublic);

}
