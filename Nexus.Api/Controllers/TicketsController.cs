using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexus.Api.Data;
using Nexus.Shared.Models;

namespace Nexus.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TicketsController(AppDbContext context) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Ticket>>> GetTickets() => await context.Tickets.ToListAsync();

        [HttpPost]
        public async Task<ActionResult<Ticket>> PostTicket(Ticket ticket)
        {
            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();
            return Ok(ticket);
        }
    }
}