using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexus.Api.Data;
using Nexus.Shared.Models;

namespace Nexus.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    [Authorize]
    public class ProjectsController(AppDbContext context) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Project>>> GetProjects() => await context.Projects.ToListAsync();

        [HttpPost]
        public async Task<ActionResult<Project>> PostProject(Project project)
        {
            context.Projects.Add(project);
            await context.SaveChangesAsync();
            return Ok(project);
        }
    }
}