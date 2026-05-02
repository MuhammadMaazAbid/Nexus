using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexus.Api.Data;
using Nexus.Shared.Models;
using System.Security.Claims;

namespace Nexus.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TicketsController(AppDbContext context) : ControllerBase
    {
        private int GetUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // GET: api/Tickets
        // Returns all non-archived tickets for projects the current user is a member of
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetTickets()
        {
            var userId = GetUserId();

            // Get all projectIds where user is assigned to at least one ticket
            // OR created the project — this is your "membership" for now
            var userProjectIds = await context.TicketAssignees
                .Where(ta => ta.UserId == userId)
                .Select(ta => ta.Ticket.ProjectId)
                .Distinct()
                .ToListAsync();

            // Also include projects the user created
            var createdProjectIds = await context.Projects
                .Where(p => p.CreatedByUserId == userId)
                .Select(p => p.Id)
                .ToListAsync();

            var allProjectIds = userProjectIds.Union(createdProjectIds).Distinct().ToList();

            var tickets = await context.Tickets
                .Where(t => allProjectIds.Contains(t.ProjectId) && !t.IsArchived)
                .Include(t => t.Assignees)
                    .ThenInclude(a => a.User)
                .Select(t => new
                {
                    t.Id,
                    t.Title,
                    t.Description,
                    t.Priority,
                    t.Status,
                    t.CreatedAt,
                    t.DueDate,
                    t.IsArchived,
                    t.ProjectId,
                    Assignees = t.Assignees.Select(a => new
                    {
                        a.UserId,
                        a.User!.FullName
                    })
                })
                .ToListAsync();

            return Ok(tickets);
        }

        // GET: api/Tickets/archive?projectId=1
        [HttpGet("archive")]
        public async Task<ActionResult<IEnumerable<object>>> GetArchivedTickets([FromQuery] int projectId)
        {
            var tickets = await context.Tickets
                .Where(t => t.ProjectId == projectId && t.IsArchived)
                .Include(t => t.Assignees).ThenInclude(a => a.User)
                .Select(t => new
                {
                    t.Id,
                    t.Title,
                    t.Description,
                    t.Priority,
                    t.Status,
                    t.CreatedAt,
                    t.DueDate,
                    t.ArchivedAt,
                    t.ProjectId,
                    Assignees = t.Assignees.Select(a => new { a.UserId, a.User!.FullName })
                })
                .ToListAsync();

            return Ok(tickets);
        }

        // GET: api/Tickets/5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetTicket(int id)
        {
            var ticket = await context.Tickets
                .Where(t => t.Id == id)
                .Include(t => t.Assignees).ThenInclude(a => a.User)
                .Include(t => t.Comments)
                .Include(t => t.ActivityLogs).ThenInclude(al => al.User)
                .Select(t => new
                {
                    t.Id,
                    t.Title,
                    t.Description,
                    t.Priority,
                    t.Status,
                    t.CreatedAt,
                    t.DueDate,
                    t.IsArchived,
                    t.ProjectId,
                    Assignees = t.Assignees.Select(a => new { a.UserId, a.User!.FullName }),
                    Comments = t.Comments.OrderBy(c => c.CreatedAt).Select(c => new
                    {
                        c.Id,
                        c.Text,
                        c.CreatedAt,
                        c.UserId,
                        c.TicketId
                    }),
                    ActivityLogs = t.ActivityLogs.OrderByDescending(al => al.CreatedAt).Select(al => new
                    {
                        al.Id,
                        al.Action,
                        al.CreatedAt,
                        AuthorName = al.User!.FullName
                    })
                })
                .FirstOrDefaultAsync();

            if (ticket == null) return NotFound();
            return Ok(ticket);
        }

        // POST: api/Tickets
        [HttpPost]
        public async Task<ActionResult<Ticket>> PostTicket([FromBody] CreateTicketRequest request)
        {
            var userId = GetUserId();

            var ticket = new Ticket
            {
                Title = request.Title,
                Description = request.Description,
                Priority = request.Priority,
                Status = "To Do",
                ProjectId = request.ProjectId,
                DueDate = request.DueDate,
                CreatedAt = DateTime.UtcNow
            };

            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            // Assign users
            if (request.AssigneeIds != null && request.AssigneeIds.Any())
            {
                foreach (var assigneeId in request.AssigneeIds)
                {
                    context.TicketAssignees.Add(new TicketAssignee
                    {
                        TicketId = ticket.Id,
                        UserId = assigneeId
                    });
                }
                await context.SaveChangesAsync();
            }

            // Log creation
            context.ActivityLogs.Add(new ActivityLog
            {
                TicketId = ticket.Id,
                UserId = userId,
                Action = "Created this ticket",
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            return Ok(ticket);
        }

        // PATCH: api/Tickets/5/status
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
        {
            var userId = GetUserId();
            var ticket = await context.Tickets.FindAsync(id);
            if (ticket == null) return NotFound();

            var oldStatus = ticket.Status;
            ticket.Status = request.Status;

            // If moved to Done, set ArchivedAt timestamp (archive job uses this)
            if (request.Status == "Done")
                ticket.ArchivedAt = DateTime.UtcNow;
            else
                ticket.ArchivedAt = null; // Moved back out of Done

            context.ActivityLogs.Add(new ActivityLog
            {
                TicketId = id,
                UserId = userId,
                Action = $"Moved from \"{oldStatus}\" to \"{request.Status}\"",
                CreatedAt = DateTime.UtcNow
            });

            await context.SaveChangesAsync();
            return Ok();
        }

        // PATCH: api/Tickets/5/assignees
        [HttpPatch("{id}/assignees")]
        public async Task<IActionResult> UpdateAssignees(int id, [FromBody] UpdateAssigneesRequest request)
        {
            var userId = GetUserId();
            var existing = context.TicketAssignees.Where(ta => ta.TicketId == id);
            context.TicketAssignees.RemoveRange(existing);

            foreach (var assigneeId in request.AssigneeIds)
            {
                context.TicketAssignees.Add(new TicketAssignee
                {
                    TicketId = id,
                    UserId = assigneeId
                });
            }

            context.ActivityLogs.Add(new ActivityLog
            {
                TicketId = id,
                UserId = userId,
                Action = "Updated assignees",
                CreatedAt = DateTime.UtcNow
            });

            await context.SaveChangesAsync();
            return Ok();
        }

        // POST: api/Tickets/run-archive
        // Call this on app startup or a scheduled basis
        [HttpPost("run-archive")]
        public async Task<IActionResult> RunArchiveJob()
        {
            var projects = await context.Projects.ToListAsync();

            foreach (var project in projects)
            {
                var cutoff = DateTime.UtcNow.AddDays(-project.ArchiveAfterDays);

                var toArchive = await context.Tickets
                    .Where(t => t.ProjectId == project.Id
                                && t.Status == "Done"
                                && !t.IsArchived
                                && t.ArchivedAt.HasValue
                                && t.ArchivedAt.Value < cutoff)
                    .ToListAsync();

                foreach (var ticket in toArchive)
                    ticket.IsArchived = true;
            }

            await context.SaveChangesAsync();
            return Ok(new { message = "Archive job complete." });
        }
    }

    // Request DTOs (keep in same file for simplicity)
    public class CreateTicketRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Priority { get; set; } = "Medium";
        public int ProjectId { get; set; }
        public DateTime? DueDate { get; set; }
        public List<int>? AssigneeIds { get; set; }
    }

    public class UpdateStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }

    public class UpdateAssigneesRequest
    {
        public List<int> AssigneeIds { get; set; } = new();
    }
}