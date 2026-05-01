using System;
using System.Collections.Generic;
using System.Text;

namespace Nexus.Shared.Models
{
    public class User
    {
        public int Id { get; set; } // Primary Key
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "Developer";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
