namespace Nexus.Shared.Models
{
    public class TicketAssignee
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public int UserId { get; set; }

        // Navigation
        public User? User { get; set; }
        public Ticket? Ticket { get; set; }
    }
}