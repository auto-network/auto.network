# Auth.razor Testing Modernization Plan

## Executive Summary

Transform Auth.razor testing from the old capture-screenshots.py methodology to the modern 4-layer testing approach proven with AuthenticationSettings. This includes:
1. Creating stateful mocks for all Auth dependencies
2. Extending TestPage.razor to be component-agnostic
3. Implementing unit, render, layout, and interaction tests
4. Achieving one-shot test creation (5-10 minutes per test)

## Current State Analysis

### Existing Documentation
- **Auth.razor.md**: Comprehensive page specification with visual design, user flows, states, and validation criteria
- **Auth.razor.test.md**: Test expectations for 8 screenshot-based tests
- **Auth.razor.json**: Screenshot definitions using capture-screenshots.py methodology

### Existing Tests (capture-screenshots.py)
1. ✅ AuthInitial - Empty form
2. ✅ AuthEmailEntered - Valid email filled
3. ✅ AuthPasswordStep - New user password creation
4. ⚠️  AuthCreateAccount - Account creation flow
5. ✅ AuthInvalidEmail - Validation error
6. ✅ AuthEmptySubmit - Empty submission
7. ⚠️  AuthExistingUserEmptyPassword - Existing user login
8. ⚠️  AuthLoggedIn - Successful authentication

### Component Complexity

**Auth.razor** is significantly more complex than AuthenticationSettings:
- **5 distinct auth steps**: Email → MethodSelection → Password/Passkey → Success
- **Multiple user types**: New users vs existing users
- **Multiple auth methods**: Password, Passkey, both
- **Complex conditional logic**: Different flows based on user state
- **Real-time connection monitoring**: Backend availability checks
- **WebAuthn integration**: Browser passkey support
- **Session management**: Token storage and navigation
- **Error handling**: Network errors, validation errors, auth failures

## Auth State Machine

### Valid States

Auth.razor implements a state machine with these steps:

```
AuthStep enum:
- Email            // Initial email entry
- MethodSelection  // Choose password or passkey (if both available)
- Password         // Password authentication/registration
- Passkey          // Passkey authentication/registration
```

### State Transitions

**New User Flow (Passkey Supported)**:
```
Email → MethodSelection → Password (create) → Success
Email → MethodSelection → Passkey (register) → Success
```

**New User Flow (No Passkey Support)**:
```
Email → Password (create) → Success
```

**Existing User (Has Both Auth Methods)**:
```
Email → MethodSelection → Password (login) → Success
Email → MethodSelection → Passkey (auth) → Success
```

**Existing User (Password Only)**:
```
Email → Password (login) → Success
```

**Existing User (Passkey Only, Supported)**:
```
Email → Passkey (auto-trigger) → Success
```

**Existing User (Passkey Only, Not Supported)**:
```
Email → Error (locked out)
```

### Mock States Needed

Unlike AuthenticationSettings which has 4 simple states, Auth.razor needs complex mock states representing:

1. **User Existence**: `new-user` vs `existing-user`
2. **Auth Methods Available**: `none`, `password-only`, `passkey-only`, `both`
3. **Passkey Support**: `supported` vs `not-supported`
4. **Connection Status**: `connected` vs `disconnected`
5. **Current Step**: `email`, `method-selection`, `password`, `passkey`

**Example Mock State Names**:
- `new-user-passkey-supported` → Email → MethodSelection
- `new-user-no-passkey-support` → Email → Password
- `existing-password-only` → Email → Password (login)
- `existing-passkey-only-supported` → Email → Passkey (auto)
- `existing-passkey-only-not-supported` → Email → Error
- `existing-both-methods` → Email → MethodSelection
- `disconnected` → Email (disabled)

## Dependencies to Mock

### 1. IAutoHostClient
**Methods Used**:
- `PasskeyCheckUserAsync(CheckUserRequest)` → CheckUserPasskeyResponse
- `AuthRegisterAsync(RegisterRequest)` → void
- `AuthLoginAsync(LoginRequest)` → LoginResponse
- `PasskeyChallengeAsync()` → ChallengeResponse
- `PasskeyRegisterAsync(RegisterNewUserPasskeyRequest)` → RegisterPasskeyResponse
- `GetVersionAsync()` → VersionResponse

**State Behavior**:
- Return different user states based on query string
- Track registration (user becomes "existing" after creation)
- Simulate successful login/registration

### 2. PasskeyService
**Methods Used**:
- `IsSupported()` → bool
- `AuthenticateWithPasskeyAsync(email)` → (success, token, error)

**State Behavior**:
- Return true/false based on query string `?passkeySupported=true/false`
- Simulate successful/failed passkey authentication

### 3. IJSRuntime
**Methods Used**:
- `sessionStorage.setItem(key, value)`
- `sessionStorage.getItem(key)`
- `PasskeySupport.createPasskey(email, challenge, rpId)` → PasskeyCreationResult
- `eval("document.getElementById('X')?.focus()")`

**State Behavior**:
- Simulate sessionStorage
- Return mock passkey creation results
- No-op for focus() calls

### 4. NavigationManager
**Methods Used**:
- `NavigateTo(url)` - Redirect after successful auth

**State Behavior**:
- Track navigation calls (don't actually navigate in tests)

## TestPage.razor Generalization

### Current Implementation
```razor
@page "/test"
<AuthenticationSettings />
```

**Problem**: Hardcoded to AuthenticationSettings component only.

### Proposed Implementation
```razor
@page "/test"
@using Microsoft.AspNetCore.Components.Web.Virtualization

@if (componentName == "AuthenticationSettings")
{
    <AuthenticationSettings />
}
else if (componentName == "Auth")
{
    <Auth />
}
else
{
    <div class="p-8 text-white">
        <h1>Component Test Harness</h1>
        <p>Unknown component: @componentName</p>
        <p>Available: AuthenticationSettings, Auth</p>
    </div>
}

@code {
    [SupplyParameterFromQuery(Name = "component")]
    private string componentName { get; set; } = "AuthenticationSettings";

    [SupplyParameterFromQuery(Name = "state")]
    private string state { get; set; } = "";

    [SupplyParameterFromQuery(Name = "automated")]
    private bool automated { get; set; } = false;
}
```

**URL Format**:
- `/test?component=Auth&state=new-user-passkey-supported&automated=true`
- `/test?component=AuthenticationSettings&state=password-only&automated=true`

**Benefits**:
- Single test harness for all components
- Consistent URL pattern
- Easy to extend for future components

## Mock Implementation Strategy

### MockAutoHostClient for Auth

```csharp
public class MockAutoHostClient : IAutoHostClient
{
    private readonly NavigationManager _nav;
    private Dictionary<string, bool> _registeredUsers = new();
    private Dictionary<string, UserAuthMethods> _userAuthMethods = new();

    public MockAutoHostClient(NavigationManager nav)
    {
        _nav = nav;
        InitializeFromState();
    }

    private void InitializeFromState()
    {
        var uri = new Uri(_nav.Uri);
        var query = HttpUtility.ParseQueryString(uri.Query);
        var state = query["state"] ?? "new-user-passkey-supported";

        Console.WriteLine($"[MockAutoHostClient] InitializeFromState() => {state}");

        switch (state)
        {
            case "new-user-passkey-supported":
            case "new-user-no-passkey-support":
                // No users registered
                break;

            case "existing-password-only":
                _registeredUsers["test@example.com"] = true;
                _userAuthMethods["test@example.com"] = new UserAuthMethods
                {
                    HasPassword = true,
                    HasPasskeys = false
                };
                break;

            case "existing-passkey-only-supported":
            case "existing-passkey-only-not-supported":
                _registeredUsers["test@example.com"] = true;
                _userAuthMethods["test@example.com"] = new UserAuthMethods
                {
                    HasPassword = false,
                    HasPasskeys = true
                };
                break;

            case "existing-both-methods":
                _registeredUsers["test@example.com"] = true;
                _userAuthMethods["test@example.com"] = new UserAuthMethods
                {
                    HasPassword = true,
                    HasPasskeys = true
                };
                break;
        }
    }

    public Task<CheckUserPasskeyResponse> PasskeyCheckUserAsync(CheckUserRequest request)
    {
        var exists = _registeredUsers.ContainsKey(request.Username);
        var methods = exists ? _userAuthMethods[request.Username] : new UserAuthMethods();

        Console.WriteLine($"[MockAutoHostClient] CheckUser({request.Username}) => Exists={exists}, HasPassword={methods.HasPassword}, HasPasskeys={methods.HasPasskeys}");

        return Task.FromResult(new CheckUserPasskeyResponse
        {
            Exists = exists,
            HasPassword = methods.HasPassword,
            HasPasskeys = methods.HasPasskeys
        });
    }

    public Task AuthRegisterAsync(RegisterRequest request)
    {
        Console.WriteLine($"[MockAutoHostClient] Register({request.Username})");
        _registeredUsers[request.Username] = true;
        _userAuthMethods[request.Username] = new UserAuthMethods { HasPassword = true, HasPasskeys = false };
        return Task.CompletedTask;
    }

    public Task<LoginResponse> AuthLoginAsync(LoginRequest request)
    {
        Console.WriteLine($"[MockAutoHostClient] Login({request.Username})");
        return Task.FromResult(new LoginResponse
        {
            Success = true,
            Token = "mock-token-12345",
            UserId = 1
        });
    }

    // ... other methods
}

private class UserAuthMethods
{
    public bool HasPassword { get; set; }
    public bool HasPasskeys { get; set; }
}
```

### MockPasskeyService for Auth

```csharp
public class MockPasskeyService : PasskeyService
{
    private readonly NavigationManager _nav;

    public MockPasskeyService(
        IJSRuntime jsRuntime,
        IAutoHostClient autoHostClient,
        ILogger<PasskeyService> logger,
        NavigationManager nav)
        : base(jsRuntime, autoHostClient, logger)
    {
        _nav = nav;
    }

    public override Task<bool> IsSupported()
    {
        var uri = new Uri(_nav.Uri);
        var query = HttpUtility.ParseQueryString(uri.Query);
        var state = query["state"] ?? "new-user-passkey-supported";

        // Determine passkey support from state
        var supported = !state.Contains("not-supported") && !state.Contains("no-passkey");

        Console.WriteLine($"[MockPasskeyService] IsSupported() => {supported} (state={state})");
        return Task.FromResult(supported);
    }

    public override Task<(bool success, string? token, string? error)> AuthenticateWithPasskeyAsync(string email)
    {
        Console.WriteLine($"[MockPasskeyService] AuthenticateWithPasskey({email})");

        // Simulate successful passkey authentication
        return Task.FromResult((true, "mock-passkey-token", (string?)null));
    }
}
```

### MockJSRuntime for Auth

```csharp
public class MockJSRuntime : IJSRuntime
{
    private Dictionary<string, string> _sessionStorage = new();

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        Console.WriteLine($"[MockJSRuntime] InvokeAsync({identifier})");

        if (identifier == "sessionStorage.setItem" && args?.Length == 2)
        {
            var key = args[0]?.ToString() ?? "";
            var value = args[1]?.ToString() ?? "";
            _sessionStorage[key] = value;
            Console.WriteLine($"  Set: {key} = {value}");
            return new ValueTask<TValue>(default(TValue)!);
        }

        if (identifier == "sessionStorage.getItem" && args?.Length == 1)
        {
            var key = args[0]?.ToString() ?? "";
            var value = _sessionStorage.GetValueOrDefault(key, "");
            Console.WriteLine($"  Get: {key} => {value}");
            return new ValueTask<TValue>((TValue)(object)value);
        }

        if (identifier == "PasskeySupport.createPasskey")
        {
            // Return mock passkey creation result
            var result = new Auth.PasskeyCreationResult
            {
                CredentialId = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                PublicKey = "mock-public-key",
                AttestationObject = Convert.ToBase64String(new byte[] { 1, 2, 3 }),
                ClientDataJSON = Convert.ToBase64String(new byte[] { 4, 5, 6 }),
                UserHandle = "mock-user-handle"
            };
            return new ValueTask<TValue>((TValue)(object)result);
        }

        if (identifier == "eval")
        {
            // No-op for focus() calls
            return new ValueTask<TValue>(default(TValue)!);
        }

        return new ValueTask<TValue>(default(TValue)!);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        return InvokeAsync<TValue>(identifier, args);
    }
}
```

## Four-Layer Test Strategy for Auth

### Layer 1: Unit Tests (bUnit) - 30+ tests
**Purpose**: Test component logic and state transitions

**Test Categories**:
1. **Email Validation** (5 tests)
   - Valid email enables Continue
   - Invalid email shows error
   - Empty email disables Continue
   - Email validation is real-time
   - Email border changes color on error

2. **User Type Detection** (6 tests)
   - New user goes to method selection (if passkey supported)
   - New user goes to password (if no passkey support)
   - Existing user with password goes to password step
   - Existing user with passkey goes to passkey step
   - Existing user with both goes to method selection
   - Existing user with passkey but no support shows error

3. **Password Step Logic** (8 tests)
   - New user sees "Create Password" and confirm field
   - Existing user sees "Password" only
   - Submit disabled when passwords don't match
   - Submit disabled when password empty
   - Submit enabled when valid
   - Back button returns to email
   - Email displayed with Change button
   - Button text correct ("Create Account" vs "Sign In")

4. **Method Selection** (4 tests)
   - Shows when user has both methods
   - Password button goes to password step
   - Passkey button triggers passkey flow (existing)
   - Passkey button triggers registration (new)

5. **Passkey Flow** (5 tests)
   - Auto-triggers for passkey-only users
   - Shows waiting state
   - Shows error state with retry option
   - Shows fallback to password (if available)
   - Successful auth navigates to home

6. **Error Handling** (4 tests)
   - Network error shows friendly message
   - Invalid credentials handled
   - Session expired handled
   - Passkey cancelled handled

**Runtime Target**: ~2 seconds total

### Layer 2: Render Tests (bUnit) - 10+ tests
**Purpose**: Validate HTML structure and CSS classes

**Test Categories**:
1. **Email Step Structure** (3 tests)
   - Email input with correct attributes
   - Continue button with correct classes
   - Status indicator structure

2. **Password Step Structure** (3 tests)
   - Password input(s) present
   - Submit button structure
   - Email display with Change button

3. **Method Selection Structure** (2 tests)
   - Two option buttons present
   - Correct icons and text

4. **Passkey Step Structure** (2 tests)
   - Passkey icon and message
   - Retry/fallback buttons (when applicable)

**Runtime Target**: ~500ms total

### Layer 3: Layout Tests (Playwright) - 10+ tests
**Purpose**: Verify actual browser rendering and element visibility

**Test States**:
1. ✅ `initial-disconnected` - Empty email, disconnected status
2. ✅ `initial-connected` - Empty email, connected status
3. ✅ `email-entered` - Valid email filled
4. ✅ `email-invalid` - Invalid email with error
5. ✅ `new-user-password-step` - Password creation form
6. ✅ `new-user-method-selection` - Choose password or passkey
7. ✅ `existing-user-password-step` - Password login form
8. ✅ `existing-user-method-selection` - Choose auth method
9. ✅ `passkey-waiting` - Passkey authentication in progress
10. ✅ `passkey-error` - Passkey failed with retry option

**Runtime Target**: ~20 seconds total (2s per state)

### Layer 4: Interaction Tests (Playwright) - 8+ tests
**Purpose**: Test complete user workflows end-to-end

**Critical Workflows**:
1. **New User - Password Registration** (new-user → email → password → create → success)
2. **New User - Passkey Registration** (new-user → email → method → passkey → success)
3. **Existing User - Password Login** (existing → email → password → login → success)
4. **Existing User - Passkey Login** (existing → email → passkey → success)
5. **Passkey Auto-Trigger** (existing-passkey-only → email → passkey-auto → success)
6. **Method Selection to Password** (existing-both → email → method → password → success)
7. **Method Selection to Passkey** (existing-both → email → method → passkey → success)
8. **Passkey Failure Fallback** (existing-both → email → passkey → fail → password → success)

**Runtime Target**: ~30 seconds total (~4s per test)

## Implementation Phases

### Phase 1: Mock Infrastructure (Day 1)
**Goal**: Create all stateful mocks and update TestPage.razor

**Tasks**:
1. ✅ Create `MockAutoHostClient` with Auth-specific state logic
2. ✅ Create `MockPasskeyService` extending base PasskeyService
3. ✅ Create `MockJSRuntime` with sessionStorage simulation
4. ✅ Update `Program.cs` to register Auth mocks conditionally
5. ✅ Generalize `TestPage.razor` to support component parameter
6. ✅ Test mock system manually in browser

**Deliverables**:
- `/home/jeremy/auto/AutoWeb/Tests/MockServices.cs` (extended with Auth mocks)
- `/home/jeremy/auto/AutoWeb/Pages/TestPage.razor` (generalized)
- `/home/jeremy/auto/AutoWeb/Program.cs` (updated mock registration)

**Validation**:
- Can navigate to `/test?component=Auth&state=new-user-passkey-supported&automated=true`
- Console logs show correct mock state initialization
- Each mock state renders the correct Auth step

### Phase 2: Unit Tests (Day 2-3)
**Goal**: Create 30+ bUnit tests for Auth component logic

**Tasks**:
1. ✅ Create `AuthTests.cs` with test infrastructure
2. ✅ Implement email validation tests (5 tests)
3. ✅ Implement user type detection tests (6 tests)
4. ✅ Implement password step logic tests (8 tests)
5. ✅ Implement method selection tests (4 tests)
6. ✅ Implement passkey flow tests (5 tests)
7. ✅ Implement error handling tests (4 tests)

**Deliverables**:
- `/home/jeremy/auto/AutoWeb.Tests/Pages/AuthTests.cs`

**Validation**:
- All 30+ tests passing
- Runtime < 2 seconds
- Coverage of all major logic branches

### Phase 3: Render Tests (Day 3)
**Goal**: Create 10+ bUnit tests for HTML structure

**Tasks**:
1. ✅ Create `AuthRenderTests.cs`
2. ✅ Implement email step structure tests (3 tests)
3. ✅ Implement password step structure tests (3 tests)
4. ✅ Implement method selection structure tests (2 tests)
5. ✅ Implement passkey step structure tests (2 tests)

**Deliverables**:
- `/home/jeremy/auto/AutoWeb.Tests/Pages/AuthRenderTests.cs`

**Validation**:
- All 10+ tests passing
- Runtime < 500ms
- Validates all HTML elements and CSS classes

### Phase 4: Layout Tests (Day 4)
**Goal**: Create 10+ Playwright tests for visual rendering

**Tasks**:
1. ✅ Create `AuthLayoutTests.cs`
2. ✅ Implement test for each Auth state (10 tests)
3. ✅ Add LayoutML capture for each state
4. ✅ Create baseline comparison infrastructure

**Deliverables**:
- `/home/jeremy/auto/AutoWeb.Tests/Pages/AuthLayoutTests.cs`
- `/home/jeremy/auto/AutoWeb.Tests/baseline/Auth/layout/*.json` (LayoutML baselines)

**Validation**:
- All 10 tests passing
- Runtime ~20 seconds total
- LayoutML captured for all states
- Visual regression detection working

### Phase 5: Interaction Tests (Day 5)
**Goal**: Create 8+ Playwright tests for complete workflows

**Tasks**:
1. ✅ Create `AuthInteractionTests.cs`
2. ✅ Implement new user password registration test
3. ✅ Implement new user passkey registration test
4. ✅ Implement existing user password login test
5. ✅ Implement existing user passkey login test
6. ✅ Implement passkey auto-trigger test
7. ✅ Implement method selection tests (2 tests)
8. ✅ Implement passkey failure fallback test

**Deliverables**:
- `/home/jeremy/auto/AutoWeb.Tests/Pages/AuthInteractionTests.cs`

**Validation**:
- All 8 tests passing
- Runtime ~30 seconds total
- All workflows validated end-to-end

### Phase 6: Documentation (Day 5)
**Goal**: Document the Auth testing system

**Tasks**:
1. ✅ Create `/home/jeremy/auto/claude/ui/Auth/Auth.md`
2. ✅ Document functional requirements
3. ✅ Document mock states and behavior
4. ✅ Document test coverage by layer
5. ✅ Document known issues and gotchas
6. ✅ Create test implementation guide

**Deliverables**:
- `/home/jeremy/auto/claude/ui/Auth/Auth.md` (comprehensive component doc)

**Validation**:
- Document includes all mock states
- Document includes test examples
- Document includes debugging tips

## Success Criteria

### Functional Requirements
- ✅ All 4 test layers implemented (unit, render, layout, interaction)
- ✅ 60+ total tests covering all Auth functionality
- ✅ All tests passing reliably
- ✅ Total test suite runtime < 60 seconds
- ✅ Mocks are stateful and track state changes
- ✅ TestPage.razor is component-agnostic

### Quality Requirements
- ✅ Tests follow AuthenticationSettings pattern
- ✅ Mock behavior matches real services
- ✅ No test flakiness (deterministic results)
- ✅ Clear test failure messages
- ✅ Screenshot capture on failures

### Documentation Requirements
- ✅ Comprehensive Auth component documentation
- ✅ All mock states documented
- ✅ Test examples for each layer
- ✅ Debugging guide with common issues

### Developer Experience
- ✅ Can add new Auth test in 5-10 minutes
- ✅ Tests run in CI/CD pipeline
- ✅ Easy to reproduce test failures locally
- ✅ Clear separation between test layers

## Risks and Mitigations

### Risk 1: Auth Complexity
**Risk**: Auth.razor is more complex than AuthenticationSettings, may take longer than expected

**Mitigation**:
- Break work into small phases
- Focus on one test layer at a time
- Use existing AuthenticationSettings tests as templates
- Don't try to achieve 100% coverage initially

### Risk 2: Passkey Mocking
**Risk**: WebAuthn/passkey interaction is complex to mock

**Mitigation**:
- Mock at JSRuntime level (return fake passkey data)
- Don't try to mock actual browser WebAuthn APIs
- Focus on happy path first, errors later
- Use simplified passkey data structures

### Risk 3: State Machine Complexity
**Risk**: Many possible state combinations, hard to test all paths

**Mitigation**:
- Document all valid states clearly
- Create test matrix showing state → expected behavior
- Test most common paths first
- Use parameterized tests for similar scenarios

### Risk 4: TestPage Generalization
**Risk**: Making TestPage component-agnostic may break existing AuthenticationSettings tests

**Mitigation**:
- Default to AuthenticationSettings for backward compatibility
- Test AuthenticationSettings thoroughly after changes
- Keep URL format consistent
- Add component parameter as optional

## Open Questions

### Q1: Should we keep capture-screenshots.py?
**Options**:
1. Deprecate entirely in favor of Playwright tests
2. Keep for documentation generation only
3. Keep for visual regression baseline capture

**Recommendation**: Keep for documentation generation (manual run), but all automated testing uses Playwright.

### Q2: How to handle Navigation.NavigateTo() in tests?
**Options**:
1. Mock NavigationManager to no-op
2. Mock to track navigation calls
3. Let it navigate and test the target page

**Recommendation**: Mock to track calls (verify navigation happened) but don't actually navigate.

### Q3: Should we test with real AutoHost backend?
**Options**:
1. All tests use mocks (current approach)
2. Add integration test layer with real backend
3. Mix: unit/render use mocks, interaction uses real backend

**Recommendation**: All tests use mocks for speed/reliability. Add separate integration test suite later if needed.

### Q4: How to test connection status polling?
**Options**:
1. Mock timer to control polling
2. Test polling logic separately
3. Disable polling in tests

**Recommendation**: Disable polling in test environment (add `[Parameter] bool EnablePolling { get; set; } = true`), test logic separately.

## Timeline Estimate

**Aggressive (Focused Work)**:
- Phase 1: 4 hours (mock infrastructure)
- Phase 2: 8 hours (unit tests)
- Phase 3: 3 hours (render tests)
- Phase 4: 4 hours (layout tests)
- Phase 5: 6 hours (interaction tests)
- Phase 6: 3 hours (documentation)
- **Total**: ~28 hours (~3.5 days)

**Conservative (With Discovery)**:
- Phase 1: 6 hours
- Phase 2: 12 hours
- Phase 3: 4 hours
- Phase 4: 6 hours
- Phase 5: 8 hours
- Phase 6: 4 hours
- **Total**: ~40 hours (~5 days)

## References

### Key Documentation
- `/home/jeremy/auto/claude/UI.md` - Testing framework overview
- `/home/jeremy/auto/claude/ui/AuthenticationSettings/AuthenticationSettings.md` - Reference implementation
- `/home/jeremy/auto/claude/ui/Auth.razor/Auth.razor.md` - Current Auth specification
- `/home/jeremy/auto/claude/Reminder.md` - Debugging methodology

### Existing Auth Tests (to Replace)
- `/home/jeremy/auto/claude/ui/Auth.razor/Auth.razor.json` - Screenshot definitions
- `/home/jeremy/auto/claude/ui/Auth.razor/Auth.razor.test.md` - Test expectations

### Related Components
- `/home/jeremy/auto/AutoWeb/Pages/Auth.razor` - Component to test
- `/home/jeremy/auto/AutoWeb/Tests/MockServices.cs` - Mock infrastructure
- `/home/jeremy/auto/AutoWeb.Tests/PlaywrightCollection.cs` - Playwright fixture sharing

## Next Steps

1. **Review this plan** - Ensure all stakeholders agree on approach
2. **Prioritize phases** - Decide if all phases needed or subset
3. **Allocate time** - Schedule focused work blocks
4. **Start Phase 1** - Create mock infrastructure
5. **Iterate** - Build one layer at a time, validate each step

## Success Metrics

After completion, we should achieve:
- ✅ **60+ Auth tests** across 4 layers
- ✅ **<60 second** total test suite runtime
- ✅ **100% reliable** test results (no flakiness)
- ✅ **5-10 minute** time to add new test
- ✅ **Zero dependency** on capture-screenshots.py for testing
- ✅ **Clear documentation** for all Auth states and tests
- ✅ **Reusable pattern** for testing other complex components
