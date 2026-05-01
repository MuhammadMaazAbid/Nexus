using System;
using System.Collections.Generic;
using System.Text;

namespace Nexus.Shared.Models
{
    public class Project
    {
        public int Id { get; set; } // Primary Key
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign Key linking to the User who created it
        public int CreatedByUserId { get; set; }
    }
}
