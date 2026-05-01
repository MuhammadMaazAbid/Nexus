using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexus.Api.Data;
using Nexus.Shared.Models;

namespace Nexus.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    [Authorize] // Ensure that only authenticated users can access these endpoints
    public class CommentsController(AppDbContext context) : ControllerBase
    {
        // GET: api/Comments
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Comment>>> GetComments() => await context.Comments.ToListAsync();

        // GET: api/Comments/ticket/5 (Get all comments for a specific ticket)
        [HttpGet("ticket/{ticketId}")]
        public async Task<ActionResult<IEnumerable<Comment>>> GetCommentsByTicket(int ticketId)
        {
            return await context.Comments.Where(c => c.TicketId == ticketId).ToListAsync();
        }

        // POST: api/Comments
        [HttpPost]
        public async Task<ActionResult<Comment>> PostComment(Comment comment)
        {
            context.Comments.Add(comment);
            await context.SaveChangesAsync();
            return Ok(comment);
        }
    }
}