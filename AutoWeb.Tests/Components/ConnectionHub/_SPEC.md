# ConnectionHub Component Specification

## Overview
The ConnectionHub component provides a centralized interface for managing external service connections (LLMs, APIs, etc.). It allows users to add, view, and delete connections with support for multiple service types and protocols.

## Component Location
- **Source:** `/home/jeremy/auto/AutoWeb/Components/ConnectionHub.razor`
- **Code-behind:** `/home/jeremy/auto/AutoWeb/Components/ConnectionHub.razor.cs`
- **Tests:** `/home/jeremy/auto/AutoWeb.Tests/Components/ConnectionHub/`

## Functional Requirements

### Core Features
1. **Connection Management**
   - List all active connections
   - Add new connections with service/protocol selection
   - Delete existing connections
   - Toggle API key visibility

2. **Service Registry Integration**
   - Dynamic service/protocol dropdown population
   - Protocol validation based on selected service
   - Display service and protocol descriptions

3. **User Feedback**
   - Success messages for create/delete operations
   - Error messages with detailed error codes
   - Loading states during async operations

## Valid States

### Component States (for testing)
Defined in `AutoWeb/Tests/MockServices.cs - MockStates`:

1. **empty** - No connections configured
   - Shows empty state message
   - Add Connection button visible

2. **single-connection** - One connection exists
   - Connection list shows 1 item
   - Show/Hide and Delete buttons available

3. **multiple-connections** - Multiple connections exist
   - Connection list shows 3 items (OpenRouter, OpenAI, Anthropic)
   - All connections independently manageable

## Service Types & Protocols

### Supported Services
- **OpenRouter** → OpenAI Compatible
- **OpenAI** → OpenAI Compatible
- **Anthropic** → Anthropic API
- **Grok (xAI)** → OpenAI Compatible

### Protocol Types
- **OpenAI Compatible** - Standard OpenAI API format
- **Anthropic API** - Anthropic's native API format

## API Integration

### Endpoints Used
1. `GET /api/connections/registry` - Get service/protocol metadata
2. `GET /api/connections` - List all active connections
3. `POST /api/connections` - Create new connection
4. `DELETE /api/connections/{id}` - Delete connection

### Request/Response Models
- `ServiceRegistryResponse` - Services and protocols metadata
- `ConnectionsListResponse` - List of connections
- `CreateConnectionRequest` - New connection data
- `CreateConnectionResponse` - Creation result with ID
- `DeleteConnectionResponse` - Deletion result

## Mock Infrastructure

### Mock States Implementation
**File:** `AutoWeb/Tests/MockServices.cs`

```csharp
["ConnectionHub"] = new[]
{
    "empty",                  // No connections
    "single-connection",      // 1 connection
    "multiple-connections"    // 3 connections
}
```

### MockAutoHostClient Implementation
- `ConnectionsGetAsync()` - Returns `_connections` list based on state
- `ConnectionsGetRegistryAsync()` - Returns hardcoded service/protocol definitions
- `ConnectionsCreateAsync()` - Adds to `_connections`, returns new ID
- `ConnectionsDeleteAsync()` - Removes from `_connections`

### State Initialization
- **empty**: `_connections = new List<ConnectionInfo>()`
- **single-connection**: 1 OpenRouter connection
- **multiple-connections**: 3 connections (OpenRouter, OpenAI, Anthropic)

## Test Coverage

### Unit Tests (14 tests, ~1s runtime)
**File:** `UnitTests.cs`

**Initialization (2 tests)**
- ✅ Should load registry on initialization
- ✅ Should load connections on initialization

**UI State (3 tests)**
- ✅ Should show empty state when no connections
- ✅ Should show Add Connection button
- ✅ Should open add connection form when button clicked

**Create Connection (4 tests)**
- ✅ Should enable Save button when API key is provided
- ✅ Should call API to create connection when Save clicked
- ✅ Should show success message after connection created
- ✅ Should show error message if creation fails

**Connection List (1 test)**
- ✅ Should display list of connections

**Key Visibility (1 test)**
- ✅ Should toggle API key visibility when Show clicked

**Delete Connection (3 tests)**
- ✅ Should call API to delete connection when Delete clicked
- ✅ Should show success message after deletion
- ✅ Should show error message if deletion fails

### Render Tests (8 tests, ~900ms runtime)
**File:** `RenderTests.cs`

**Component Structure (1 test)**
- ✅ Should render main container with correct structure

**UI Elements (2 tests)**
- ✅ Should render Add Connection button with correct styling
- ✅ Should render empty state message

**Form Rendering (1 test)**
- ✅ Should render add form structure with all inputs and buttons

**Connection List (1 test)**
- ✅ Should render connection list items with correct structure

**State-based Rendering (3 tests)**
- ✅ Should apply disabled attribute correctly to Save button
- ✅ Should render success message with correct styling
- ✅ Should render error message with correct styling

### Layout Tests (3 tests, Playwright)
**File:** `LayoutTests.cs`

- Empty state renders correctly
- Single connection renders correctly
- Multiple connections render correctly

### Interaction Tests (3 tests, Playwright)
**File:** `InteractionTests.cs`

- Can toggle API key visibility
- Can delete connection
- Can create new connection

## Event Handling

### Form Inputs
- Uses `@bind` for inputs (triggers `onchange` events)
- **Testing Note:** Use `.Change("value")` in bUnit tests, NOT `.InputAsync()`

### Button Clicks
- Add Connection - Opens form
- Save - Creates new connection
- Cancel - Closes form
- Show/Hide - Toggles key visibility
- Delete - Removes connection

## Error Handling

### Error Codes
- `AuthenticationRequired` (2003) - User not authenticated
- `ApiKeyRequired` (1002) - API key field empty
- `InvalidServiceProtocol` (5002) - Invalid service/protocol combination
- `ConnectionNotFound` (3002) - Connection doesn't exist
- `Forbidden` (3003) - User doesn't own connection

### Error Display
- Errors shown in red bordered box with red background
- Success shown in green bordered box with green background
- Messages auto-clear on next operation

## Performance Metrics

### Test Performance
- **Unit tests:** 14 tests in ~1 second
- **Render tests:** 8 tests in ~900ms
- **Total bUnit:** 22 tests in ~2 seconds
- **Playwright tests:** 6 tests (require server running)

### Component Performance
- Initial load: Fetches registry + connections in parallel
- Create: Single API call + reload connections
- Delete: Single API call + reload connections
- Toggle visibility: Pure client-side (instant)

## Known Issues

### Testing
1. **Playwright tests require server** - Layout and interaction tests need AutoWeb running on correct port
2. **Mock state is ephemeral** - Reloading page resets to initial state

### Component
1. **No connection editing** - Must delete and recreate to change
2. **No connection validation** - API key format not validated client-side
3. **No duplicate detection** - Can create multiple connections with same key

## Debugging Tips

1. **Test Failures**
   - For `@bind` elements, use `.Change()` not `.InputAsync()` in bUnit
   - Check console logs for MockAutoHostClient state changes
   - Verify state parameter in URL matches expected mock state

2. **Component Issues**
   - Check browser console for API errors
   - Verify ServiceRegistry has correct service/protocol mappings
   - Ensure ConnectionsController validates protocol compatibility

3. **State Issues**
   - Reload page to reset to initial mock state
   - Check MockAutoHostClient initialization for state handling
   - Verify TestPage.razor includes ConnectionHub component

## Related Files

### Source Files
- `/home/jeremy/auto/AutoWeb/Components/ConnectionHub.razor` - Component markup
- `/home/jeremy/auto/AutoWeb/Components/ConnectionHub.razor.cs` - Component logic
- `/home/jeremy/auto/AutoWeb/Tests/TestPage.razor` - Test harness page
- `/home/jeremy/auto/AutoWeb/Tests/MockServices.cs` - Mock implementation

### Backend Files
- `/home/jeremy/auto/AutoHost/Controllers/ConnectionsController.cs` - API endpoints
- `/home/jeremy/auto/AutoHost/Services/ServiceRegistry.cs` - Service/protocol registry
- `/home/jeremy/auto/AutoHost/Models/ServiceType.cs` - Enum definitions

### Test Files
- `/home/jeremy/auto/AutoWeb.Tests/Components/ConnectionHub/UnitTests.cs` - 14 unit tests
- `/home/jeremy/auto/AutoWeb.Tests/Components/ConnectionHub/RenderTests.cs` - 8 render tests
- `/home/jeremy/auto/AutoWeb.Tests/Components/ConnectionHub/LayoutTests.cs` - 3 layout tests
- `/home/jeremy/auto/AutoWeb.Tests/Components/ConnectionHub/InteractionTests.cs` - 3 interaction tests

## Test Execution

### Run All Tests
```bash
dotnet test --filter "FullyQualifiedName~ConnectionHub"
```

### Run Specific Layer
```bash
# Unit tests only
dotnet test --filter "FullyQualifiedName~ConnectionHubTests"

# Render tests only
dotnet test --filter "FullyQualifiedName~ConnectionHubRenderTests"

# Playwright tests (requires server)
dotnet test --filter "FullyQualifiedName~ConnectionHubLayoutTests"
dotnet test --filter "FullyQualifiedName~ConnectionHubInteractionTests"
```

### Manual Testing
```bash
# Navigate to test harness
http://localhost:6200/test?component=ConnectionHub&state=empty
http://localhost:6200/test?component=ConnectionHub&state=single-connection
http://localhost:6200/test?component=ConnectionHub&state=multiple-connections
```

## Future Enhancements

1. **Connection Editing** - Update existing connections
2. **Connection Validation** - Client-side API key format validation
3. **Bulk Operations** - Delete multiple connections
4. **Connection Testing** - Test connection before saving
5. **Connection Templates** - Quick setup for common services
6. **Import/Export** - Backup and restore connections
