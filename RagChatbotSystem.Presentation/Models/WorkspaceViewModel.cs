using System;
using System.Collections.Generic;
using RagChatbotSystem.Business.DTOs;

namespace RagChatbotSystem.Presentation.Models
{
    public class WorkspaceViewModel
    {
        // Selected IDs (can be null)
        public Guid? SelectedUserId { get; set; }
        public Guid? SelectedDatasetId { get; set; }
        public Guid? SelectedSessionId { get; set; }

        // Selected objects (can be null)
        public UserDto? SelectedUser { get; set; }
        public DatasetDto? SelectedDataset { get; set; }
        public ChatSessionDto? SelectedSession { get; set; }

        // Lists to populate UI
        public IReadOnlyList<UserDto> Users { get; set; } = Array.Empty<UserDto>();
        public IReadOnlyList<DatasetDto> Datasets { get; set; } = Array.Empty<DatasetDto>();
        public IReadOnlyList<DocumentDto> Documents { get; set; } = Array.Empty<DocumentDto>();
        public IReadOnlyList<ChatSessionDto> Sessions { get; set; } = Array.Empty<ChatSessionDto>();
        public IReadOnlyList<ChatMessageDto> MessageHistory { get; set; } = Array.Empty<ChatMessageDto>();
        public IReadOnlyList<CitationDto> Citations { get; set; } = Array.Empty<CitationDto>();
        public Guid? SelectedMessageId { get; set; }

        // For displaying dynamic messages
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
    }
}
