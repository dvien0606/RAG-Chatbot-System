using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using RagChatbotSystem.Business.DTOs;
using RagChatbotSystem.Business.Interfaces;
using RagChatbotSystem.Presentation.Hubs;

namespace RagChatbotSystem.Presentation.Services
{
    public class RealtimeService : IRealtimeService
    {
        private readonly IHubContext<ChatHub> _chatHubContext;
        private readonly IHubContext<DocumentHub> _documentHubContext;
        private readonly IHubContext<NotificationHub> _notificationHubContext;

        public RealtimeService(
            IHubContext<ChatHub> chatHubContext,
            IHubContext<DocumentHub> documentHubContext,
            IHubContext<NotificationHub> notificationHubContext)
        {
            _chatHubContext = chatHubContext;
            _documentHubContext = documentHubContext;
            _notificationHubContext = notificationHubContext;
        }

        public async Task SendChatChunkAsync(Guid sessionId, Guid messageId, string chunk, CancellationToken cancellationToken = default)
        {
            await _chatHubContext.Clients.Group($"session_{sessionId}")
                .SendAsync("ReceiveChatChunk", messageId, chunk, cancellationToken);
        }

        public async Task SendChatCompleteAsync(Guid sessionId, ChatMessageDto message, IReadOnlyList<CitationDto> citations, CancellationToken cancellationToken = default)
        {
            await _chatHubContext.Clients.Group($"session_{sessionId}")
                .SendAsync("ReceiveChatComplete", message, citations, cancellationToken);
        }

        public async Task SendDocumentProgressAsync(Guid datasetId, Guid documentId, string status, int percentComplete, CancellationToken cancellationToken = default)
        {
            await _documentHubContext.Clients.Group($"dataset_{datasetId}")
                .SendAsync("ReceiveDocumentProgress", documentId, status, percentComplete, cancellationToken);
        }

        public async Task SendNotificationAsync(Guid userId, string message, CancellationToken cancellationToken = default)
        {
            await _notificationHubContext.Clients.Group($"user_{userId}")
                .SendAsync("ReceiveNotification", message, cancellationToken);
        }

        public async Task TriggerUiUpdateAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default)
        {
            // Update all notification group and global observers
            await _notificationHubContext.Clients.All.SendAsync("TriggerUiUpdate", entityType, entityId, cancellationToken);
            await _chatHubContext.Clients.All.SendAsync("TriggerUiUpdate", entityType, entityId, cancellationToken);
            await _documentHubContext.Clients.All.SendAsync("TriggerUiUpdate", entityType, entityId, cancellationToken);
        }
    }
}
