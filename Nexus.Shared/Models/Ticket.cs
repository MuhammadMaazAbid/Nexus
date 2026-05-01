using System;
using System.Collections.Generic;
using System.Text;

namespace Nexus.Shared.Models
{
    public class Ticket
    {
        public int Id { get; set; } // Primary Key
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Priority { get; set; } = "Medium";
        public string Status { get; set; } = "To-Do";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign Key linking to the Project this ticket belongs to
        public int ProjectId { get; set; }
    }
}
