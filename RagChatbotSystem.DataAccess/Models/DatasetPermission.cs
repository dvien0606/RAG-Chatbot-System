using System;

namespace RagChatbotSystem.DataAccess.Models
{
    public class DatasetPermission
    {
        public Guid PermissionId { get; set; }
        public Guid DatasetId { get; set; }
        public Guid UserId { get; set; }
        public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Dataset Dataset { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}
