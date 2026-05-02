using System;

namespace Nexus.Shared.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }       // Recipient
        public int? TicketId { get; set; }    // Which ticket triggered it
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}