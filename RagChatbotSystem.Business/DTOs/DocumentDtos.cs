using System;

namespace RagChatbotSystem.Business.DTOs
{
    public sealed record DocumentDto(
        Guid DocumentId,
        Guid DatasetId,
        string FileName,
        string FilePath,
        string FileType,
        long FileSize,
        string Status,
        Guid UploadedBy,
        DateTime UploadedAt,
        DateTime UpdatedAt);

    public sealed record StoredFileDto(
        string OriginalFileName,
        string StoredFileName,
        string RelativePath,
        string FileType,
        long FileSize);

    public sealed record ChunkDto(
        Guid ChunkId,
        Guid DocumentId,
        int ChunkIndex,
        string Content,
        int PageNumber,
        float[]? Embedding);
}
