using Microsoft.EntityFrameworkCore;
using Nexus.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// --- ADD THESE TWO LINES ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// ---------------------------

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // --- ADD THESE TWO LINES ---
    app.UseSwagger();
    app.UseSwaggerUI();
    // ---------------------------
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();