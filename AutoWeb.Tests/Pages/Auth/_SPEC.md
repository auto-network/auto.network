# Auth.razor Component Specification

## Purpose
Authentication page handling user login and registration with support for both password and passkey (WebAuthn) authentication methods. Manages API keys and sessions through the AutoHost backend.

## Auth State Machine

Auth.razor implements a multi-step state machine with the following steps:

```
AuthStep enum:
- Email            // Initial email entry
- MethodSelection  // Choose password or passkey (if both available)
- Password         // Password authentication/registration
- Passkey          // Passkey authentication/registration
```

## Valid State Transitions

### New User Flows

**New User (Passkey Supported)**:
```
Email → MethodSelection → Password (create) → Success
Email → MethodSelection → Passkey (register) → Success
```

**New User (No Passkey Support)**:
```
Email → Password (create) → Success
```

### Existing User Flows

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

## Mock States

All mocks read the `?state=` query parameter to determine initial data and behavior.

### Available States

1. **new-user-passkey-supported**
   - User doesn't exist
   - Browser supports passkeys
   - Flow: Email → MethodSelection → Password/Passkey → Success

2. **new-user-passkey-not-supported**
   - User doesn't exist
   - Browser doesn't support passkeys
   - Flow: Email → Password → Success

3. **existing-password-only**
   - User exists with password, no passkeys
   - Flow: Email → Password (login) → Success

4. **existing-passkey-only**
   - User exists with passkey, no password
   - Browser supports passkeys
   - Flow: Email → Passkey (auto-trigger) → Success

5. **existing-passkey-only-not-supported**
   - User exists with passkey, no password
   - Browser doesn't support passkeys
   - Flow: Email → Error

6. **existing-password-and-passkey**
   - User exists with both auth methods
   - Browser supports passkeys
   - Flow: Email → MethodSelection → Password/Passkey → Success

7. **existing-password-and-passkey-not-supported**
   - User exists with both auth methods
   - Browser doesn't support passkeys
   - Flow: Email → Password (login) → Success

## Component Structure

### Email Step
- Email input with validation
- Continue button (enabled only with valid email)
- Connection status indicator
- Real-time email validation

### Method Selection Step
- Password option (🔑 icon)
- Passkey option (🔐 icon)
- Both clickable buttons
- Email displayed with "Change" button

### Password Step

**New User**:
- "Create Password" label
- Password input
- Confirm Password input
- "Create Account" button (enabled when passwords match)

**Existing User**:
- "Password" label
- Single password input
- "Sign In" button (enabled when password filled)

**Both**:
- Email displayed with "Change" button to return to email step

### Passkey Step
- "Authenticating with Passkey" heading
- Waiting state during WebAuthn
- On error: Retry button + fallback to password (if available)
- On success: Navigate to home

## Test Infrastructure

### Mock Services

**MockAutoHostClient** (`AutoWeb/Tests/MockServices.cs`)
- Implements `IAutoHostClient`
- Stateful: tracks `_registeredUsers`, `_userAuthMethods`
- Initializes from query string state
- Updates internal state on operations
- Logs all operations to console

**Key Methods**:
- `PasskeyCheckUserAsync(CheckUserRequest)` → Returns user existence and auth methods
- `AuthRegisterAsync(RegisterRequest)` → Registers new user with password
- `AuthLoginAsync(LoginRequest)` → Returns success with mock token
- `PasskeyChallengeAsync()` → Returns mock challenge
- `PasskeyRegisterAsync(RegisterNewUserPasskeyRequest)` → Registers new user with passkey
- `GetVersionAsync()` → Returns mock version

**MockPasskeyServiceForAuth** (`AutoWeb/Tests/MockServices.cs`)
- Extends `PasskeyService`
- Overrides `IsSupported()` to read from query string
- Overrides `AuthenticateWithPasskeyAsync()` to return mock success
- Overrides `RegisterNewUserWithPasskeyAsync()` to return mock success

**MockJSRuntime** (`AutoWeb/Tests/MockJSRuntime.cs`)
- Implements `IJSRuntime`
- Simulates `sessionStorage.setItem/getItem`
- Returns mock passkey creation data for `PasskeySupport.createPasskey`
- No-op for `eval()` focus calls

### Test Page
**URL**: `http://localhost:6200/test?component=Auth&state={STATE}&automated=true`

**Example URLs**:
- `http://localhost:6200/test?component=Auth&state=new-user-passkey-supported&automated=true`
- `http://localhost:6200/test?component=Auth&state=existing-password-only&automated=true`

### Test Files

**Unit Tests** (`UnitTests.cs`)
- Uses bUnit
- Tests component logic and state transitions
- Fast (10-50ms per test)
- 34 tests total

**Render Tests** (`RenderTests.cs`)
- Uses bUnit
- Tests HTML structure, CSS classes, element attributes
- Fast (10-50ms per test)
- 10 tests total

**Layout Tests** (`LayoutTests.cs`)
- Uses Playwright
- Tests actual browser rendering and element visibility
- ~2-3 seconds per test
- 8 tests total

**Interaction Tests** (`InteractionTests.cs`)
- Uses Playwright
- Tests complete end-to-end user workflows
- ~3-5 seconds per test
- 8 tests total

## Test Coverage

### Unit Tests (34 tests)

**Email Validation** (5 tests):
- Valid email enables Continue
- Invalid email shows error
- Empty email disables Continue
- Email validation is real-time
- Email border changes on error

**User Type Detection** (6 tests):
- New user with passkey support → method selection
- New user without passkey support → password
- Existing user password-only → password
- Existing user passkey-only → passkey
- Existing user both methods → method selection
- Existing user passkey-only but no support → error

**Password Step Logic** (8 tests):
- New user sees "Create Password" + confirm field
- Existing user sees "Password" only
- Submit disabled when passwords don't match
- Submit disabled when password empty
- Submit enabled when valid
- Back button returns to email
- Email displayed with Change button
- Button text correct ("Create Account" vs "Sign In")

**Method Selection** (4 tests):
- Shows when user has both methods
- Password button goes to password step
- Passkey button triggers passkey flow (existing)
- Passkey button triggers registration (new)

**Passkey Flow** (5 tests):
- Auto-triggers for passkey-only users
- Shows waiting state
- Shows error state with retry
- Shows fallback to password (if available)
- Successful auth navigates to home

**Error Handling** (6 tests):
- Network error handling
- Invalid credentials
- Passkey errors
- Various edge cases

### Render Tests (10 tests)

**Email Step Structure** (3 tests):
- Email input with correct attributes
- Continue button with correct classes
- Status indicator structure

**Password Step Structure** (3 tests):
- Password input(s) present
- Submit button structure
- Email display with Change button

**Method Selection Structure** (2 tests):
- Two option buttons present
- Correct icons (🔑, 🔐) and text

**Passkey Step Structure** (2 tests):
- Passkey icon and message
- Retry/fallback buttons structure

### Layout Tests (8 tests)

States tested:
1. Email step with new user (passkey supported)
2. Email step with valid email entered
3. Email step with invalid email
4. Method selection for new user
5. Password creation for new user
6. Password login for existing user
7. Method selection for existing user with both methods
8. Passkey button visibility

### Interaction Tests (8 tests)

Complete workflows:
1. **NewUser_PasswordRegistration_Success** - Email → Method Selection → Password → Create Account
2. **NewUser_PasskeyRegistration_Success** - Email → Method Selection → Passkey → Success
3. **NewUser_NoPasskeySupport_PasswordOnly** - Email → Password (no method selection) → Create Account
4. **ExistingUser_PasswordLogin_Success** - Email → Password → Sign In
5. **ExistingUser_PasskeyAutoTrigger_Success** - Email → Passkey (auto) → Success
6. **ExistingUser_MethodSelection_ChoosePassword_Success** - Email → Method Selection → Password → Sign In
7. **ExistingUser_MethodSelection_ChoosePasskey_Success** - Email → Method Selection → Passkey → Success
8. **ExistingUser_PasswordAndPasskey_NoPasskeySupport_PasswordOnly** - Email → Password (skip passkey) → Sign In

## Critical Bug Fixes

### Bug 1: SelectPasskeyMethod Routing
**Problem**: When new user clicked passkey button, component called `AuthenticateWithPasskeyAsync()` instead of `RegisterWithPasskey()`.

**Fix**: Updated `SelectPasskeyMethod()` to check `isNewUser` flag and call appropriate method:
```csharp
if (isNewUser)
{
    await RegisterWithPasskey();
}
else
{
    await AuthenticateWithPasskey();
}
```

**Location**: `AutoWeb/Pages/Auth.razor.cs:247`

### Bug 2: Passkey Registration Bypassing Mocks
**Problem**: `RegisterWithPasskey()` called JavaScript directly instead of using PasskeyService, preventing mock interception in tests.

**Fix**:
1. Added `RegisterNewUserWithPasskeyAsync()` to PasskeyService
2. Refactored `RegisterWithPasskey()` to call service method
3. Added mock override in `MockPasskeyServiceForAuth`

**Result**: Mocks now properly intercept all passkey operations.

**Location**: `AutoWeb/Services/PasskeyService.cs:81`

## Known Issues & Gotchas

### Playwright + Blazor

**Issue 1: FillAsync doesn't work with @bind:event="oninput"**
```csharp
// ❌ WRONG
await input.FillAsync("password");

// ✓ CORRECT
await input.PressSequentiallyAsync("password");
```

**Issue 2: has-text() does substring matching**
```csharp
// ❌ WRONG - matches both "Create" and "Create Account"
page.Locator("button:has-text('Create')");

// ✓ CORRECT - exact match
page.GetByRole(AriaRole.Button, new() { Name = "Create Account", Exact = true });
```

**Issue 3: Use emoji selectors for precision**
```csharp
// ✓ Best practice - unambiguous
page.Locator("button:has-text('🔑')");  // Password
page.Locator("button:has-text('🔐')");  // Passkey
```

### Component Behavior

**Issue 1: State updates are asynchronous**
- After transitions, component may re-render
- Tests need small waits (100-500ms) for state to settle
- Don't rely on instantaneous updates

**Issue 2: Passkey auto-trigger**
- Passkey-only users automatically trigger passkey auth after email
- No method selection shown
- Tests must account for automatic progression

**Issue 3: sessionStorage as success indicator**
- Successful auth sets `authToken`, `userId`, `userEmail` in sessionStorage
- Tests verify these instead of navigation (navigation is mocked)

## Debugging Tips

### Test Failing? Follow This Order:
1. Check if test is using correct mock state
2. Check console logs for mock service calls
3. Verify selectors match actual HTML
4. Check timing (wait long enough for Blazor updates)
5. Look for JavaScript errors in browser console

### Common Selector Issues:
```csharp
// Finding inputs
page.Locator("input[type='email']")
page.Locator("input[type='password']").First  // First password field
page.Locator("input[type='password']").Nth(1) // Confirm password

// Finding buttons with emojis
page.Locator("button:has-text('🔑')")
page.Locator("button:has-text('🔐')")

// Finding buttons by text
page.Locator("button[type='submit']:has-text('Continue')")
page.Locator("button[type='submit']:has-text('Create Account')")
page.Locator("button[type='submit']:has-text('Sign In')")
```

### Mock Debugging:
All mocks log to console:
```
[MockAutoHostClient] InitializeFromState() => new-user-passkey-supported
[MockAutoHostClient] CheckUser(test@example.com) => Exists=False
[MockPasskeyServiceForAuth] IsSupported() => true (state=new-user-passkey-supported)
[MockPasskeyServiceForAuth] RegisterNewUserWithPasskeyAsync(test@example.com)
```

Enable console logging in tests:
```csharp
page.Console += (_, msg) => _output.WriteLine($"CONSOLE: {msg.Text}");
```

## Performance Metrics

### Actual Performance
- **Unit tests** (34 tests): ~1 second
- **Render tests** (10 tests): ~500ms
- **Layout tests** (8 tests): ~24 seconds
- **Interaction tests** (8 tests): ~28 seconds
- **Total** (60 tests): ~53 seconds

### Comparison to Full-Stack
- Full-stack (with real backend): 3-5 minutes
- Mock-based: <1 minute
- **Speedup**: 3-5x faster

## References

**Component Source**: `AutoWeb/Pages/Auth.razor`

**Mock Source**: `AutoWeb/Tests/MockServices.cs`

**Test Files**:
- Unit: `UnitTests.cs` (this directory)
- Render: `RenderTests.cs` (this directory)
- Layout: `LayoutTests.cs` (this directory)
- Interaction: `InteractionTests.cs` (this directory)

**Test Harness**: `AutoWeb/Pages/TestPage.razor`

**Documentation**:
- Testing Framework: `/claude/UI.md`
- Debugging Guide: `/claude/Reminder.md`
- Anti-Patterns: `/claude/Stupid.md`
- Planning Doc: `/claude/tasks/Auth-Phase5-InteractionTests-Plan.md`
- Modernization Plan: `/claude/tasks/Auth-Testing-Modernization.md`
