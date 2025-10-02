using AutoHost.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace AutoHost.Middleware;

public class TokenAuthMiddleware
{
    private readonly RequestDelegate _next;

    public TokenAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
    {
        // Try to get token from Authorization header
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

        // If we have a token, validate it and set user context
        if (!string.IsNullOrEmpty(token))
        {
            // Hash the provided token to compare with stored hash
            byte[] tokenBytes;
            try
            {
                tokenBytes = Convert.FromBase64String(token);
            }
            catch
            {
                // Invalid token format - just don't set user context
                await _next(context);
                return;
            }

            var tokenHash = Helpers.TokenHelper.HashToken(token);

            // Check if session exists and is valid
            var session = await dbContext.Sessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Token == tokenHash && s.IsActive);

            if (session != null && session.ExpiresAt >= DateTime.UtcNow)
            {
                // Update last accessed time
                session.LastAccessedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();

                // Store user info in context for use in controllers
                context.Items["UserId"] = session.UserId;
                context.Items["User"] = session.User;
                context.Items["Session"] = session;
            }
        }

        await _next(context);
    }
}