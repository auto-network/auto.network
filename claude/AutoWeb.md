# AutoWeb Documentation

AutoWeb is the Blazor WebAssembly frontend that provides a terminal-inspired chat interface for AI interactions.

## Overview

- **Framework**: Blazor WebAssembly (.NET 9.0)
- **CSS**: TailwindCSS v4.1.13
- **UI Style**: Terminal-inspired (green-on-black aesthetic)
- **API Integration**: OpenRouter (x-ai/grok-4-fast:free model)
- **Backend Integration**: AutoHost API via NSwag-generated client
- **Port**: Varies (typically 5000-5300 range)

## Key Features

- Email-first authentication flow with smooth transitions
- Real-time chat with OpenRouter API
- API key persistence via AutoHost backend
- Connection status monitoring
- Auto-generated strongly-typed API client
- Hot reload for both C# and CSS

## Project Structure

```
AutoWeb/
├── Client/
│   └── AutoHostClient.cs      # Auto-generated API client
├── Components/
│   └── AuthenticationSettings.razor  # Auth settings component
├── Layout/
│   └── MainLayout.razor       # Main layout wrapper
├── Pages/
│   ├── Auth.razor             # Authentication page
│   └── Home.razor             # Main chat interface
├── Services/
│   └── PasskeyService.cs      # WebAuthn passkey operations
├── Tests/                     # Mock infrastructure for testing
│   ├── TestPage.razor         # Component test harness
│   ├── MockServices.cs        # MockAutoHostClient, MockStates registry
│   ├── MockJSRuntime.cs       # Mock JSRuntime (sessionStorage, PasskeySupport)
│   └── PlaywrightCollection.cs # xUnit collection for browser tests
├── Properties/
│   └── launchSettings.json    # Development server config
├── wwwroot/
│   ├── css/
│   │   ├── input.css          # TailwindCSS input
│   │   └── app.css            # Compiled CSS output
│   └── index.html             # SPA host page
├── _Imports.razor             # Global using statements
├── App.razor                  # Root component
├── Program.cs                 # Application entry point & mock registration
├── autonomy.csproj            # Project file
├── package.json               # Node dependencies (TailwindCSS)
└── node_modules/              # Node packages
```

## Authentication Flow

1. **Email Step**: User enters email
2. **Check User**: API call to see if user exists
3. **Password Step**: Show appropriate UI (login vs register)
4. **Authentication**: Register and/or login via AutoHost
5. **Session Storage**: Store token, userId, and email
6. **Navigation**: Redirect to main chat interface

## Client Generation

The AutoHost API client is auto-generated on every build:

1. NSwag reads OpenAPI spec from http://localhost:5050/swagger/v1/swagger.json
2. Generates strongly-typed client to `/Client/AutoHostClient.cs`
3. Includes all request/response models
4. Full IntelliSense support

## Development Commands

```bash
# Start with hot reload (CSS + .NET)
cd /home/jeremy/auto/AutoWeb
npm run dev

# Run .NET only
dotnet watch run

# Build CSS only
npx @tailwindcss/cli -i ./wwwroot/css/input.css -o ./wwwroot/css/app.css --watch

# Build project
dotnet build

# Publish for production
dotnet publish
```

## NPM Scripts

```json
{
  "scripts": {
    "build:css": "npx @tailwindcss/cli -i ./wwwroot/css/input.css -o ./wwwroot/css/app.css --minify",
    "dev": "concurrently \"npx @tailwindcss/cli -i ./wwwroot/css/input.css -o ./wwwroot/css/app.css --watch\" \"dotnet watch run\""
  }
}
```

## Key Components

### Auth.razor
- Email-first authentication flow
- Smooth CSS transitions between steps
- Connection status monitoring
- Error handling with user feedback
- Strongly-typed API client usage

### Home.razor
- Main chat interface (4,360+ lines)
- Direct OpenRouter API integration
- Message history management
- Streaming response support
- Terminal-style UI

### Program.cs
- Service registration
- HttpClient configuration
- AutoHostClient registration with DI

## API Integration

### AutoHost Integration
```csharp
@inject IAutoHostClient AutoHostClient

// Example usage
var response = await AutoHostClient.CheckAsync(new CheckUserRequest { Username = email });
var exists = response.Exists;
```

### OpenRouter Integration
- Direct client-side API calls
- Streaming support for real-time responses
- API key retrieved from AutoHost and stored in session

## TailwindCSS Integration

- Input file: `/wwwroot/css/input.css`
- Output file: `/wwwroot/css/app.css`
- Watches all `.razor` files for class usage
- Automatic minification on publish
- Hot reload during development

## Build Targets

The project file includes custom MSBuild targets:

1. **BuildTailwind** - Runs before publish to minify CSS
2. **GenerateAutoHostClient** - Runs before build to generate API client

## Dependencies

### NuGet Packages
- Microsoft.AspNetCore.Components.WebAssembly
- Newtonsoft.Json (for NSwag)
- NSwag.MSBuild (for client generation)

### NPM Packages
- @tailwindcss/cli
- concurrently (for dev script)

## Session Storage

The app stores the following in browser session storage:
- `authToken` - Session token from AutoHost
- `userId` - User ID from AutoHost
- `userEmail` - User's email address

## Testing Infrastructure

AutoWeb includes comprehensive mock infrastructure for isolated component testing. See `/home/jeremy/auto/claude/UI.md` for complete documentation.

### Mock System Overview

**Purpose**: Enable fast, isolated component testing without backend dependencies.

**Key Components**:

1. **TestPage.razor** - Component-agnostic test harness
   - Renders any component via `?component=` parameter
   - Loads state via `?state=` parameter
   - Supports automated mode with `?automated=true`
   - Example: `/test?component=Auth&state=new-user-passkey-supported&automated=true`

2. **MockStates Registry** - Central state definitions
   ```csharp
   public static class MockStates
   {
       public static readonly Dictionary<string, string[]> ComponentStates = new()
       {
           ["AuthenticationSettings"] = new[] { "password-only", "single-passkey", ... },
           ["Auth"] = new[] { "new-user-passkey-supported", ... }
       };
   }
   ```

3. **MockAutoHostClient** - Stateful API mock
   - Reads initial state from query string
   - Tracks state changes during operations
   - Returns data based on current state
   - Supports both AuthenticationSettings and Auth components

4. **MockPasskeyServiceForAuth** - Passkey operations mock
   - Reads passkey support from query string
   - Returns mock passkey creation/authentication data
   - Extends base PasskeyService

5. **MockJSRuntime** - JSRuntime mock
   - Implements sessionStorage operations
   - Mocks PasskeySupport.createPasskey/getPasskey
   - Handles eval() calls (for focus())

### Mock Registration (Program.cs)

```csharp
var enableMocks = Environment.GetEnvironmentVariable("ENABLE_MOCKS") == "true" ||
                  builder.HostEnvironment.IsDevelopment();

if (enableMocks)
{
    // Register MockAutoHostClient
    builder.Services.AddScoped<IAutoHostClient>(sp =>
    {
        var nav = sp.GetRequiredService<NavigationManager>();
        return new MockAutoHostClient(nav);
    });

    // Register MockPasskeyServiceForAuth
    builder.Services.AddScoped<PasskeyService>(sp =>
    {
        var jsRuntime = sp.GetRequiredService<IJSRuntime>();
        var autoHostClient = sp.GetRequiredService<IAutoHostClient>();
        var logger = sp.GetRequiredService<ILogger<PasskeyService>>();
        var nav = sp.GetRequiredService<NavigationManager>();
        return new MockPasskeyServiceForAuth(jsRuntime, autoHostClient, logger, nav);
    });
}
```

**CRITICAL**: Never register `IJSRuntime` in Program.cs - causes service lifetime conflicts. Tests provide MockJSRuntime directly.

### Test Organization

Tests live in separate `AutoWeb.Tests` project:
- **Unit Tests**: bUnit tests for component logic (~10-50ms each)
- **Render Tests**: bUnit tests for HTML structure (~10-50ms each)
- **Layout Tests**: Playwright tests for visual layout (~1-3s each)
- **Interaction Tests**: Playwright tests for workflows (~3-5s each)

See `AutoWeb.Tests/Components/AuthenticationSettings/` for complete examples.

### Running Tests

```bash
# Run all tests
cd /home/jeremy/auto
dotnet test

# Run specific component tests
dotnet test --filter "FullyQualifiedName~AuthenticationSettings"

# Run with mocks enabled (for manual testing)
cd AutoWeb
ENABLE_MOCKS=true dotnet run --urls http://localhost:6200
# Navigate to /test?component=AuthenticationSettings&state=password-only
```

### Testing Benefits

- ✅ **Fast**: Milliseconds for unit/render, seconds for browser tests
- ✅ **Isolated**: No AutoHost backend required
- ✅ **Comprehensive**: 4-layer coverage (79+ tests for AuthenticationSettings)
- ✅ **Deterministic**: No flakiness from backend/network issues
- ✅ **State control**: Direct URL access to any component state
- ✅ **CI-friendly**: Runs in automated pipelines

## Error Code Pattern

AutoWeb uses machine-readable error codes from the AutoHost API for proper error handling and future i18n support.

### Backend: AuthErrorCode Enum

Located in `/home/jeremy/auto/AutoHost/Models/AuthErrorCode.cs`:

```csharp
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AuthErrorCode
{
    None = 0,

    // Validation errors (1000-1999)
    UsernameRequired = 1000,
    PasswordRequired = 1001,
    InvalidEmail = 1003,

    // Authentication errors (2000-2999)
    InvalidCredentials = 2000,
    AuthenticationCancelled = 2001,
    InvalidOrExpiredChallenge = 2002,
    PasskeyAuthenticationFailed = 2005,
    WrongPasskeySelected = 2006,

    // Authorization errors (3000-3999)
    UserNotFound = 3000,
    PasskeyNotFound = 3001,

    // Registration errors (4000-4999)
    UsernameAlreadyExists = 4000,

    // Browser/client errors (6000-6999)
    PasskeyNotSupported = 6000,
    CannotConnect = 6001,

    // Generic errors (9000-9999)
    UnknownError = 9000
}
```

**Key Feature**: Uses `JsonStringEnumConverter` so enum names (not numbers) serialize over the wire.

### Frontend: Error Code Handling

Components store both `errorMessage` (for debugging) and `errorCode` (for logic):

```csharp
private string errorMessage = "";
private AutoWeb.Client.AuthErrorCode? errorCode = null;

// Catch API errors
catch (ApiException<ErrorResponse> ex)
{
    errorMessage = ex.Result?.Error ?? "Operation failed";
    errorCode = ex.Result?.ErrorCode;
}

// Catch network errors (no error code available)
catch (HttpRequestException)
{
    errorMessage = "Cannot connect to AutoHost";
    errorCode = null;  // Network errors don't have codes
}

// Display user-friendly messages based on error code
private string GetUserFriendlyMessage(AuthErrorCode? code)
{
    if (code == null)
    {
        // Fallback for network errors
        if (errorMessage.Contains("Cannot connect"))
            return "Unable to connect. Please check your connection.";
        return errorMessage;
    }

    return code.Value switch
    {
        AuthErrorCode.InvalidCredentials => "Invalid password. Please try again.",
        AuthErrorCode.UsernameAlreadyExists => "An account already exists.",
        AuthErrorCode.PasskeyNotSupported => "Your browser doesn't support passkeys.",
        _ => "An error occurred. Please try again."
    };
}
```

### Services: Returning Error Codes

Services that wrap external APIs (like PasskeyService) map errors to codes:

```csharp
public virtual async Task<(bool Success, string? Token, AuthErrorCode? ErrorCode, string? Error)>
    AuthenticateWithPasskeyAsync(string username)
{
    try
    {
        var result = await _apiClient.PasskeyAuthenticateAsync(request);
        return (result.Success, result.Token,
                result.Success ? null : AuthErrorCode.PasskeyAuthenticationFailed,
                result.Success ? null : "Authentication failed");
    }
    catch (Exception ex)
    {
        // Map browser errors to error codes
        var errorCode = ex.Message.Contains("User denied", StringComparison.OrdinalIgnoreCase)
            ? AuthErrorCode.AuthenticationCancelled
            : AuthErrorCode.PasskeyAuthenticationFailed;

        return (false, null, errorCode, ex.Message);
    }
}
```

### Client Regeneration

When AutoHost API changes, regenerate the client:

```bash
/home/jeremy/auto/regenerate-client.sh
```

This ensures `AuthErrorCode` enum stays in sync between backend and frontend.

### Benefits

- ✅ **Type-safe**: Enum-based instead of string matching
- ✅ **i18n-ready**: Error codes can be mapped to localized messages
- ✅ **Maintainable**: Single source of truth in AutoHost
- ✅ **Testable**: Mocks can return specific error codes
- ✅ **Debuggable**: Error message still present for logging

## Important Notes

- AutoHost must be running on port 5050 (production mode)
- Client regenerates automatically on build
- TailwindCSS rebuilds on file changes
- Session storage clears when browser closes
- Multiple browser sessions supported per user
- Mock mode enabled in development by default
- Test port 6200 used by Playwright fixture
- **Error codes**: Always use `AuthErrorCode` for API errors, not string matching