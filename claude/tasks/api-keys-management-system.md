# Connections Hub - Sprint Plan

**Created**: 2025-10-02
**Status**: In Progress
**Estimated Time**: 20-30 hours
**Goal**: Implement Connections Hub for managing multiple service integrations with comprehensive test coverage

## Overview

Transform the current single-key API system into a comprehensive **Connections Hub** that manages all external service integrations. Each connection represents an authenticated integration with an external service (OpenRouter, OpenAI, Anthropic, Grok, etc.) and defines how the system can interact with that service.

## Objectives

### Phase 1 & 2: Full Implementation
1. Backend support for multiple active connections with service type identification
2. New dedicated ConnectionsController with REST endpoints (list, create, delete)
3. Frontend Connections component supporting multiple service integrations
4. Comprehensive 4-layer test coverage (60-80 frontend tests, 20-25 backend tests)
5. Complete documentation following project standards

## Architecture Changes

### Backend Structure
- **New Controller**: `ConnectionsController.cs` at `/api/connections/*`
- **Separated from Auth**: Move API key operations out of AuthController into dedicated Connections Hub
- **Multi-Connection Support**: Remove single-active-key limitation, support unlimited active connections
- **Model Remains**: ApiKey model unchanged (represents credentials for connections)

### API Endpoints

**ConnectionsController** (`/api/connections/*`):
- `GET /api/connections` - List all active connections for user
- `GET /api/connections/registry` - Get service and protocol definitions (for UI)
- `POST /api/connections` - Create new connection (validates service/protocol mapping)
- `DELETE /api/connections/{id}` - Soft delete specific connection

**AuthController** (`/api/auth/*`):
- Keep existing: register, login, check, password/*, passkey/*
- Remove: apikey endpoints (moved to ConnectionsController)

## Data Model Changes

### ApiKey Model (Represents Connection Credentials)
**File**: `/home/jeremy/auto/AutoHost/Models/ApiKey.cs`

**Concept**: Each ApiKey represents a connection's authentication credentials. The model stores how to authenticate with a specific external service.

**New Fields**:
```csharp
[Required]
public ServiceType ServiceType { get; set; } = ServiceType.OpenRouter;

[Required]
public ProtocolType Protocol { get; set; } = ProtocolType.OpenAICompatible;
```

**Migration Required**: Yes - Add two new enum columns (stored as int)

### Service Type Enums
**File**: `/home/jeremy/auto/AutoHost/Models/ServiceType.cs` (NEW)

**Phase 1 & 2: LLM Services Only** (extensible for Storage/Compute later)
```csharp
public enum ServiceType
{
    OpenRouter,
    OpenAI,
    Anthropic,
    Grok
}

public enum ProtocolType
{
    OpenAICompatible,
    AnthropicAPI
}
```

### Service Registry
**File**: `/home/jeremy/auto/AutoHost/Services/ServiceRegistry.cs` (NEW)

**Purpose**: Define valid ServiceType → ProtocolType mappings and metadata

```csharp
public class ServiceDefinition
{
    public ServiceType Type { get; init; }
    public string DisplayName { get; init; } = "";
    public string Description { get; init; } = "";
    public ProtocolType[] SupportedProtocols { get; init; } = Array.Empty<ProtocolType>();
    public ProtocolType DefaultProtocol { get; init; }
}

public class ProtocolDefinition
{
    public ProtocolType Type { get; init; }
    public string DisplayName { get; init; } = "";
    public string Description { get; init; } = "";
}

public static class ServiceRegistry
{
    public static readonly Dictionary<ServiceType, ServiceDefinition> Services;
    public static readonly Dictionary<ProtocolType, ProtocolDefinition> Protocols;

    public static ProtocolType[] GetSupportedProtocols(ServiceType service);
    public static ProtocolType GetDefaultProtocol(ServiceType service);
    public static bool IsValidMapping(ServiceType service, ProtocolType protocol);
}
```

**Future Extensibility**: Architecture supports adding Storage (S3-Compatible), Compute, etc. by simply adding to enums and registry.

### Response Models
**File**: `/home/jeremy/auto/AutoHost/Controllers/ConnectionsController.cs` or separate Models file

**New Models**:
```csharp
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
```

**Updated Models**:
```csharp
public class CreateConnectionRequest
{
    [Required]
    public string ApiKey { get; set; } = "";

    [MaxLength(200)]
    public string? Description { get; set; }

    [Required]
    public ServiceType ServiceType { get; set; }

    [Required]
    public ProtocolType Protocol { get; set; }
}

public class CreateConnectionResponse
{
    public bool Success { get; set; }
    public int? ConnectionId { get; set; }
}
```

## Frontend Changes

### Component Updates
**File**: `/home/jeremy/auto/AutoWeb/Components/ConnectionHub.razor` (renamed from ApiKeysSettings)

**Key Changes**:
1. Rename component: `ApiKeysSettings` → `ConnectionHub`
2. Create code-behind: `ConnectionHub.razor.cs`
3. Call connections endpoint: `ConnectionsGetAsync()`
4. Add ServiceType dropdown with options: OpenRouter, OpenAI, Anthropic, Grok
5. Display multiple connections with service badges
6. Delete specific connection by ID: `ConnectionsDeleteAsync(id)`
7. Update UI terminology: "API Keys" → "Connections"
8. Remove single-connection limitation logic

### Mock Infrastructure
**File**: `/home/jeremy/auto/AutoWeb/Tests/MockServices.cs`

**MockStates** (Starting States Only):
```csharp
["ConnectionHub"] = new[]
{
    "no-connections",
    "single-connection-openrouter",
    "multiple-connections-same-service",
    "multiple-connections-different-services",
    "many-connections-mixed"
}
```

**Mock Methods to Implement**:
- `ConnectionsGetAsync()` - Return connections based on state
- `ConnectionsCreateAsync(request)` - Add connection to internal list
- `ConnectionsDeleteAsync(id)` - Remove connection from list

## Test Strategy

### Backend Tests (AutoHost.Tests)
**File**: `/home/jeremy/auto/AutoHost.Tests/ConnectionsControllerTests.cs` (NEW)

**Target**: 20-25 tests, ~5-10 seconds runtime

**Categories**:
1. **List Connections** (5 tests)
   - Empty list when no connections
   - Single connection with metadata
   - Multiple connections ordered by CreatedAt
   - Only active connections (not deleted)
   - 401 when not authenticated

2. **Create Connection** (6 tests)
   - Creates with ServiceType
   - Allows multiple active connections
   - Returns ConnectionId
   - 400 when ApiKey empty
   - 400 when ServiceType empty
   - 401 when not authenticated

3. **Delete Connection** (5 tests)
   - Soft deletes (IsActive=false)
   - 404 when not found
   - 403 when wrong owner
   - 401 when not authenticated
   - Deleted connection not in list

4. **Integration** (4 tests)
   - Create multiple → List all
   - Create → Delete → Empty list
   - Multiple services persist correctly
   - ServiceType roundtrip

### Frontend Tests (AutoWeb.Tests)
**Directory**: `/home/jeremy/auto/AutoWeb.Tests/Components/ConnectionHub/`

**Target**: 60-80 tests, ~1 minute runtime

#### Unit Tests (bUnit) - 40-50 tests
**File**: `UnitTests.cs`

1. Initialization (6 tests)
2. Add Connection Form (10 tests)
3. Save Connection (8 tests)
4. View Connection Key (4 tests)
5. Delete Connection (8 tests)
6. Service Type Display (4 tests)
7. Conditional Rendering (6 tests)

#### Render Tests (bUnit) - 8-10 tests
**File**: `RenderTests.cs`

1. HTML Structure (3 tests)
2. CSS Classes (3 tests)
3. Accessibility (2 tests)
4. Service Type Rendering (2 tests)

#### Layout Tests (Playwright) - 5-6 tests
**File**: `LayoutTests.cs`

1. State Rendering (5 tests - one per mock state)
2. Element Visibility (1 test)

#### Interaction Tests (Playwright) - 10-12 tests
**File**: `InteractionTests.cs`

1. Add Connection Workflows (4 tests)
2. View Connection Key Workflows (2 tests)
3. Delete Connection Workflows (3 tests)
4. Complex Workflows (3 tests)

### Documentation
**File**: `/home/jeremy/auto/AutoWeb.Tests/Components/ConnectionHub/_SPEC.md`

**Contents**:
- Component purpose and requirements
- Valid states (5 starting states)
- Mock infrastructure details
- Test coverage breakdown (4 layers)
- Backend API integration
- Service type extensibility
- Known limitations
- Performance metrics
- Debugging tips

## Task Breakdown

### Backend Tasks (10-14 hours)

1. **Create Service Type Enums** (30 min)
   - New file: `/home/jeremy/auto/AutoHost/Models/ServiceType.cs`
   - Define ServiceType enum (OpenRouter, OpenAI, Anthropic, Grok)
   - Define ProtocolType enum (OpenAICompatible, AnthropicAPI)

2. **Create Service Registry** (2 hours)
   - New file: `/home/jeremy/auto/AutoHost/Services/ServiceRegistry.cs`
   - Define ServiceDefinition and ProtocolDefinition classes
   - Implement service/protocol mappings for LLM services
   - Validation methods (IsValidMapping, GetDefaultProtocol, etc.)

3. **Update ApiKey Model** (30 min)
   - Change fields to enums: ServiceType and ProtocolType
   - File: `/home/jeremy/auto/AutoHost/Models/ApiKey.cs`

4. **Create EF Migration** (30 min)
   ```bash
   cd AutoHost
   dotnet ef migrations add AddServiceTypeAndProtocolToApiKey
   dotnet ef database update
   ```

5. **Create ConnectionsController** (3 hours)
   - New file: `/home/jeremy/auto/AutoHost/Controllers/ConnectionsController.cs`
   - Implement GET /api/connections (list all)
   - Implement GET /api/connections/registry (service/protocol definitions)
   - Implement POST /api/connections (with registry validation)
   - Implement DELETE /api/connections/{id}
   - Create response/request models
   - Add authentication middleware requirement

6. **Remove API Key Endpoints from AuthController** (30 min)
   - Remove `GetApiKey()` method
   - Remove `SaveApiKey()` method
   - Keep file clean for auth operations only

7. **Write ConnectionsController Tests** (4-5 hours)
   - New file: `/home/jeremy/auto/AutoHost.Tests/ConnectionsControllerTests.cs`
   - 25-30 tests including registry validation tests
   - WebApplicationFactory pattern with FluentAssertions

8. **Verify Backend Tests Pass** (30 min)
   ```bash
   cd /home/jeremy/auto/AutoHost.Tests
   dotnet test
   ```

### Frontend Tasks (12-18 hours)

7. **Regenerate NSwag Client** (10 min)
   ```bash
   /home/jeremy/auto/regenerate-client.sh
   ```

8. **Define MockStates** (30 min)
   - File: `/home/jeremy/auto/AutoWeb/Tests/MockServices.cs`
   - Add ConnectionHub states to registry

9. **Implement Mock Methods** (2 hours)
   - File: `/home/jeremy/auto/AutoWeb/Tests/MockServices.cs`
   - ConnectionsGetAsync(), ConnectionsCreateAsync(), ConnectionsDeleteAsync()
   - State-based data return
   - Console logging

10. **Rename and Update Component Files** (30 min)
    - Rename: `ApiKeysSettings.razor` → `ConnectionHub.razor`
    - Update Settings.razor to use ConnectionHub component
    - Update tab text from "API Keys" to "Connections"

11. **Update TestPage.razor** (15 min)
    - Add ConnectionHub to component switch

12. **Refactor to Code-Behind** (2 hours)
    - Create: `/home/jeremy/auto/AutoWeb/Components/ConnectionHub.razor.cs`
    - Move all logic from @code block
    - Keep .razor with only markup

13. **Update Component Logic** (3 hours)
    - Use new connections API endpoints
    - Update terminology: "API Keys" → "Connections" throughout UI
    - Add ServiceType dropdown
    - Handle multiple connections
    - Service type badges/colors
    - Delete by specific ID

14. **Write Unit Tests** (3 hours)
    - File: `/home/jeremy/auto/AutoWeb.Tests/Components/ConnectionHub/UnitTests.cs`
    - 40-50 tests with bUnit

15. **Write Render Tests** (1 hour)
    - File: `/home/jeremy/auto/AutoWeb.Tests/Components/ConnectionHub/RenderTests.cs`
    - 8-10 tests with bUnit

16. **Write Layout Tests** (1.5 hours)
    - File: `/home/jeremy/auto/AutoWeb.Tests/Components/ConnectionHub/LayoutTests.cs`
    - 5-6 tests with Playwright

17. **Write Interaction Tests** (2.5 hours)
    - File: `/home/jeremy/auto/AutoWeb.Tests/Components/ConnectionHub/InteractionTests.cs`
    - 10-12 tests with Playwright

18. **Create Documentation** (1.5 hours)
    - File: `/home/jeremy/auto/AutoWeb.Tests/Components/ConnectionHub/_SPEC.md`
    - Complete component specification

### Verification & Commit (2 hours)

18. **Run Full Test Suite** (30 min)
    ```bash
    cd /home/jeremy/auto
    dotnet test
    ```
    - Verify all ~220+ tests pass (139 existing + ~80 new)

19. **Manual Verification** (1 hour)
    - Start AutoHost and AutoWeb
    - Test all workflows manually
    - Verify database persistence
    - Check console for errors

20. **Create Detailed Commit** (30 min)
    - Stage all changes
    - Detailed commit message
    - GPG signed commit
    - Push to GitHub
    - Verify "Verified" badge

## Success Criteria

- ✅ Backend supports multiple active keys simultaneously
- ✅ ServiceType and Protocol fields in database with migration
- ✅ ApiKeysController with 3 endpoints (GET, POST, DELETE)
- ✅ 20-25 backend tests passing
- ✅ Frontend displays multiple keys with service types
- ✅ ServiceType dropdown with 4 options (OpenRouter, OpenAI, Anthropic, Grok)
- ✅ 60-80 frontend tests passing (4-layer strategy)
- ✅ Mock infrastructure supports all 5 starting states
- ✅ Complete _SPEC.md documentation
- ✅ All 220+ tests in suite pass
- ✅ Manual verification successful
- ✅ Detailed commit pushed to GitHub with GPG signature

## Future Enhancements (Not in Phase 1 & 2)

- Protocol registry system
- Usage tracking (tokens in/out/cached, costs)
- Key analytics dashboard
- Default/primary key designation
- Key validation per service type
- Key masking (show only last 4 chars)
- Search/filter keys
- Edit key metadata
- Key expiration dates
- Key rotation workflows
- Extend beyond LLM keys (storage, messaging, etc.)

## Notes

- No backward compatibility needed (no active users)
- Database migration required but no data migration
- Clean separation: Auth vs API Keys
- Extensible for future service types
- Mock states are starting states only (not transient states)
- Follow project debugging methodology (claude/Method.md)
- Follow UI testing framework (claude/UI.md)

## References

- Project overview: `/home/jeremy/auto/CLAUDE.md`
- Backend docs: `/home/jeremy/auto/claude/AutoHost.md`
- Frontend docs: `/home/jeremy/auto/claude/AutoWeb.md`
- Testing framework: `/home/jeremy/auto/claude/UI.md`
- Debugging methodology: `/home/jeremy/auto/claude/Method.md`
- Anti-patterns: `/home/jeremy/auto/claude/Stupid.md`
