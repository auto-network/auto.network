# AutoHost Documentation

AutoHost is the backend API service that provides authentication and data persistence for the Auto application.

## Overview

- **Framework**: ASP.NET Core Web API (.NET 9.0)
- **Database**: SQLite with Entity Framework Core (Code First)
- **Authentication**: Session-based with SHA256 token hashing
- **Documentation**: Swagger/OpenAPI available at http://localhost:5050/swagger
- **Port**: 5050

## Key Features

- User registration and login
- API key management for OpenRouter
- Session token authentication
- Multiple concurrent sessions per user
- Strongly-typed API responses

## Project Structure

```
AutoHost/
├── Controllers/
│   ├── AuthController.cs      # Authentication endpoints
│   └── VersionController.cs   # Version/status endpoint
├── Data/
│   └── AppDbContext.cs        # EF Core database context
├── Extensions/
│   └── SwaggerExtensions.cs   # Swagger configuration
├── Middleware/
│   └── TokenAuthMiddleware.cs # Custom auth middleware
├── Models/
│   ├── User.cs                # User entity
│   ├── Session.cs             # Session entity
│   ├── ApiKey.cs              # API key entity
│   └── Responses.cs           # Response DTOs
├── Services/
│   └── PasswordService.cs     # Password hashing service
├── Program.cs                 # Application entry point
└── autohost.db                # SQLite database (generated)
```

## API Endpoints

### Public Endpoints
- `POST /api/auth/register` - Register new user
- `POST /api/auth/check` - Check if username exists
- `POST /api/auth/login` - Login and receive session token
- `GET /api/version` - Get API version/status

### Protected Endpoints (Require Bearer Token)
- `GET /api/auth/apikey` - Get user's active API key
- `POST /api/auth/apikey` - Save new API key

## Authentication Flow

1. User registers with email/password
2. Password is hashed using BCrypt
3. On login, a 32-byte secure random token is generated
4. Token is SHA256 hashed before storing in database
5. Client receives the unhashed token
6. Client sends token as Bearer authentication header
7. Middleware validates by hashing and comparing

## Database Schema

### Users Table
- Id (int, PK)
- Username (string, unique)
- PasswordHash (string)
- CreatedAt (DateTime)
- LastLoginAt (DateTime?)

### Sessions Table
- Id (int, PK)
- UserId (int, FK)
- Token (string, SHA256 hash)
- CreatedAt (DateTime)
- ExpiresAt (DateTime)

### ApiKeys Table
- Id (int, PK)
- UserId (int, FK)
- Key (string, encrypted)
- Description (string?)
- IsActive (bool)
- CreatedAt (DateTime)
- LastUsedAt (DateTime?)

## Development Commands

```bash
# Run the API
cd /home/jeremy/auto/AutoHost
dotnet run --urls http://localhost:5050

# Run with watch mode
dotnet watch run --urls http://localhost:5050

# Build
dotnet build

# Run EF migrations (if needed)
dotnet ef migrations add MigrationName
dotnet ef database update
```

## Testing

Tests are in the AutoHost.Tests project using xUnit and WebApplicationFactory:

```bash
cd /home/jeremy/auto/AutoHost.Tests
dotnet test
```

## Response Models

All API responses use strongly-typed models:
- `RegisterResponse` - Success, UserId, Username
- `CheckUserResponse` - Exists
- `LoginResponse` - Success, UserId, Username, Token
- `ApiKeyResponse` - ApiKey, Description
- `SaveApiKeyResponse` - Success
- `ErrorResponse` - Error message
- `VersionResponse` - Version, Status

## Security Notes

- Passwords are hashed with BCrypt (work factor 12)
- Session tokens are 32 cryptographically secure random bytes
- Tokens are stored as SHA256 hashes in the database
- API keys are encrypted at rest
- Multiple sessions per user are supported
- No JWT - simple session tokens for simplicity