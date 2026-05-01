using System;
using System.Collections.Generic;
using System.Text;

namespace Nexus.Shared.Models
{
    public class Comment
    {
        public int Id { get; set; } // Primary Key
        public string Text { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign Keys
        public int TicketId { get; set; } // Which ticket is this comment on?
        public int UserId { get; set; }   // Who wrote the comment?
    }
}
