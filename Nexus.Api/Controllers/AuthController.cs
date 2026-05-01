using BCryptNet = BCrypt.Net.BCrypt;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Nexus.Shared.Models;
using Nexus.Shared.DTOs;
using Nexus.Api.Data; // Ensure this points to your actual Data namespace

namespace Nexus.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly AppDbContext _context; // Injected Database Context

        public AuthController(IConfiguration config, AppDbContext context)
        {
            _config = config;
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            // 1. Check if user already exists in the database
            var userExists = await _context.Users.AnyAsync(u => u.Email == request.Email);
            if (userExists) return BadRequest("This email is already in use.");

            // 2. Hash the password using BCrypt for security
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // 3. Create the new User object
            var newUser = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = passwordHash,
                Role = "Member", // Defaulting first user to Admin for your startup
                CreatedAt = DateTime.UtcNow
            };

            // 4. Save to the SQL Database
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Registration successful." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            // 1. Find the user by email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            // 2. Verify existence and password hash
            if (user is null || !BCryptNet.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized("Invalid email or password.");
            }

            // 3. Generate the real JWT token using database values
            var token = GenerateJwtToken(user.Id.ToString(), user.Email, user.Role);

            return Ok(new { Token = token });
        }

        private string GenerateJwtToken(string userId, string email, string role)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(8),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}