using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexus.Api.Data;
using Nexus.Shared.Models;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace Nexus.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CommentsController(AppDbContext context) : ControllerBase
    {
        private int GetUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // GET: api/Comments?ticketId=5
        [HttpGet]
        public async Task<IActionResult> GetComments([FromQuery] int ticketId)
        {
            var comments = await context.Comments
                .Where(c => c.TicketId == ticketId)
                .OrderBy(c => c.CreatedAt)
                .Join(context.Users,
                    c => c.UserId,
                    u => u.Id,
                    (c, u) => new
                    {
                        c.Id,
                        c.Text,
                        c.CreatedAt,
                        c.TicketId,
                        c.UserId,
                        AuthorName = u.FullName
                    })
                .ToListAsync();

            return Ok(comments);
        }

        // POST: api/Comments
        [HttpPost]
        public async Task<IActionResult> PostComment([FromBody] Comment comment)
        {
            var userId = GetUserId();
            comment.UserId = userId;
            comment.CreatedAt = DateTime.UtcNow;

            context.Comments.Add(comment);
            await context.SaveChangesAsync();

            // @mention detection — scan for @FullName patterns
            await ProcessMentions(comment);

            return Ok(comment);
        }

        private async Task ProcessMentions(Comment comment)
        {
            // Match @FirstName or @FirstName_LastName patterns
            var matches = Regex.Matches(comment.Text, @"@(\w+)");
            if (!matches.Any()) return;

            var author = await context.Users.FindAsync(comment.UserId);
            var allUsers = await context.Users.ToListAsync();

            foreach (Match match in matches)
            {
                var mentionedFragment = match.Groups[1].Value.ToLower();

                // Find user whose name starts with the mentioned fragment
                var mentionedUser = allUsers.FirstOrDefault(u =>
                    u.FullName.Replace(" ", "").ToLower().StartsWith(mentionedFragment) ||
                    u.FullName.Split(' ')[0].ToLower() == mentionedFragment);

                if (mentionedUser == null || mentionedUser.Id == comment.UserId) continue;

                context.Notifications.Add(new Notification
                {
                    UserId = mentionedUser.Id,
                    TicketId = comment.TicketId,
                    Message = $"{author?.FullName ?? "Someone"} mentioned you in a comment.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await context.SaveChangesAsync();
        }
    }
}