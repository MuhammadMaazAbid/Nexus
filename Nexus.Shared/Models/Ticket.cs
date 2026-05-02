using System;
using System.Collections.Generic;

namespace Nexus.Shared.Models
{
    public class Ticket
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Priority { get; set; } = "Medium";
        public string Status { get; set; } = "To Do";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DueDate { get; set; }
        public bool IsArchived { get; set; } = false;
        public DateTime? ArchivedAt { get; set; }

        // Foreign Key
        public int ProjectId { get; set; }

        // Navigation (for EF joins — ignored by Blazor frontend)
        public List<TicketAssignee> Assignees { get; set; } = new();
        public List<Comment> Comments { get; set; } = new();
        public List<ActivityLog> ActivityLogs { get; set; } = new();
    }
}