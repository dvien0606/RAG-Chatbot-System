using System;
using System.Collections.Generic;

namespace RagChatbotSystem.DataAccess.Models
{
    public class User
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsApproved { get; set; } = true;

        // Navigation properties
        public virtual ICollection<DatasetPermission> DatasetPermissions { get; set; } = new List<DatasetPermission>();
        public virtual ICollection<Dataset> Datasets { get; set; } = new List<Dataset>();
        public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
        public virtual ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();
    }
}

