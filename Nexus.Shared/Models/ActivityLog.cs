using System;

namespace Nexus.Shared.Models
{
    public class ActivityLog
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public int UserId { get; set; }        // Who performed the action
        public string Action { get; set; } = string.Empty;  // e.g. "Moved to In Progress"
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public User? User { get; set; }
    }
}