using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoHost.Data;
using AutoHost.Models;
using AutoHost.Services;

namespace AutoHost.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConnectionsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ConnectionsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet(Name = "ConnectionsGet")]
    [ProducesResponseType(typeof(ConnectionsListResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    public async Task<ActionResult<ConnectionsListResponse>> GetConnections()
    {
        // Get authenticated user ID from context
        var userId = HttpContext.Items["UserId"] as int?;
        if (userId == null)
        {
            return Unauthorized(new ErrorResponse
            {
                Error = "Authentication required",
                ErrorCode = AuthErrorCode.AuthenticationRequired
            });
        }

        var connections = await _context.ApiKeys
            .Where(k => k.UserId == userId.Value && k.IsActive)
            .OrderByDescending(k => k.CreatedAt)
            .Select(k => new ConnectionInfo
            {
                Id = k.Id,
                ServiceType = k.ServiceType,
                Protocol = k.Protocol,
                Description = k.Description,
                Key = k.Key,
                CreatedAt = k.CreatedAt,
                LastUsedAt = k.LastUsedAt
            })
            .ToListAsync();

        return Ok(new ConnectionsListResponse { Connections = connections });
    }

    [HttpGet("registry", Name = "ConnectionsGetRegistry")]
    [ProducesResponseType(typeof(ServiceRegistryResponse), 200)]
    public ActionResult<ServiceRegistryResponse> GetRegistry()
    {
        return Ok(new ServiceRegistryResponse
        {
            Services = ServiceRegistry.Services.Values.ToList(),
            Protocols = ServiceRegistry.Protocols.Values.ToList()
        });
    }

    [HttpPost(Name = "ConnectionsCreate")]
    [ProducesResponseType(typeof(CreateConnectionResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    public async Task<ActionResult<CreateConnectionResponse>> CreateConnection([FromBody] CreateConnectionRequest request)
    {
        // Get authenticated user ID from context
        var userId = HttpContext.Items["UserId"] as int?;
        if (userId == null)
        {
            return Unauthorized(new ErrorResponse
            {
                Error = "Authentication required",
                ErrorCode = AuthErrorCode.AuthenticationRequired
            });
        }

        // Validate API key
        if (string.IsNullOrWhiteSpace(request.ApiKey))
        {
            return BadRequest(new ErrorResponse
            {
                Error = "API key is required",
                ErrorCode = AuthErrorCode.ApiKeyRequired
            });
        }

        // Validate service/protocol mapping
        if (!ServiceRegistry.IsValidMapping(request.ServiceType, request.Protocol))
        {
            return BadRequest(new ErrorResponse
            {
                Error = $"Invalid protocol '{request.Protocol}' for service '{request.ServiceType}'. " +
                        $"Supported protocols: {string.Join(", ", ServiceRegistry.GetSupportedProtocols(request.ServiceType))}",
                ErrorCode = AuthErrorCode.InvalidServiceProtocol
            });
        }

        // Create new connection
        var connection = new ApiKey
        {
            UserId = userId.Value,
            Key = request.ApiKey,
            Description = request.Description ?? $"{request.ServiceType} Connection",
            ServiceType = request.ServiceType,
            Protocol = request.Protocol
        };

        _context.ApiKeys.Add(connection);
        await _context.SaveChangesAsync();

        return Ok(new CreateConnectionResponse
        {
            Success = true,
            ConnectionId = connection.Id
        });
    }

    [HttpDelete("{id}", Name = "ConnectionsDelete")]
    [ProducesResponseType(typeof(DeleteConnectionResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    [ProducesResponseType(typeof(ErrorResponse), 403)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<ActionResult<DeleteConnectionResponse>> DeleteConnection(int id)
    {
        // Get authenticated user ID from context
        var userId = HttpContext.Items["UserId"] as int?;
        if (userId == null)
        {
            return Unauthorized(new ErrorResponse
            {
                Error = "Authentication required",
                ErrorCode = AuthErrorCode.AuthenticationRequired
            });
        }

        var connection = await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.Id == id && k.IsActive);

        if (connection == null)
        {
            return NotFound(new ErrorResponse
            {
                Error = "Connection not found",
                ErrorCode = AuthErrorCode.ConnectionNotFound
            });
        }

        // Verify ownership
        if (connection.UserId != userId.Value)
        {
            return StatusCode(403, new ErrorResponse
            {
                Error = "You do not have permission to delete this connection",
                ErrorCode = AuthErrorCode.Forbidden
            });
        }

        // Soft delete
        connection.IsActive = false;
        await _context.SaveChangesAsync();

        return Ok(new DeleteConnectionResponse { Success = true });
    }
}

// Response Models
public class ConnectionInfo
{
    public int Id { get; set; }
    public ServiceType ServiceType { get; set; }
    public ProtocolType Protocol { get; set; }
    public string? Description { get; set; }
    public string Key { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
}

public class ConnectionsListResponse
{
    public List<ConnectionInfo> Connections { get; set; } = new();
}

public class ServiceRegistryResponse
{
    public List<ServiceDefinition> Services { get; set; } = new();
    public List<ProtocolDefinition> Protocols { get; set; } = new();
}

// Request Models
public class CreateConnectionRequest
{
    public string ApiKey { get; set; } = "";
    public string? Description { get; set; }
    public ServiceType ServiceType { get; set; }
    public ProtocolType Protocol { get; set; }
}

public class CreateConnectionResponse
{
    public bool Success { get; set; }
    public int? ConnectionId { get; set; }
}

public class DeleteConnectionResponse
{
    public bool Success { get; set; }
}
