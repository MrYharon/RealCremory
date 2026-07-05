using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cremory.API.Data;
using Cremory.API.Models;

namespace Cremory.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeviceTokensController : ControllerBase
    {
        private readonly CremoryDbContext _context;

        public DeviceTokensController(CremoryDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Register([FromBody] RegisterDeviceTokenRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Token))
                return BadRequest(new { error = "Token is required" });

            var existing = await _context.DeviceTokens
                .FirstOrDefaultAsync(t => t.Token == request.Token);

            if (existing != null)
            {
                existing.LastUsedAt = DateTime.UtcNow;
            }
            else
            {
                _context.DeviceTokens.Add(new DeviceToken
                {
                    Token = request.Token,
                    Platform = request.Platform ?? "Android",
                    CreatedAt = DateTime.UtcNow,
                    LastUsedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Token registered" });
        }

        public class RegisterDeviceTokenRequest
        {
            public string Token { get; set; } = string.Empty;
            public string? Platform { get; set; }
        }
    }
}
