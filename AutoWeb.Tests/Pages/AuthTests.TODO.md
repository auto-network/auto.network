# Auth.razor Unit Tests - TODO List

This document tracks placeholder tests that need full implementation when enhanced mock infrastructure is available.

## Summary

**Current Status**: 34 tests implemented, 3 placeholders

**Fully Implemented**: 31 tests
**Placeholders**: 3 tests

**Recent Completions** (2025-10-01):
- ✅ Passkey Error State With Retry - Implemented using `MockPasskeyServiceForAuth.OverrideAuthResult`
- ✅ Passkey Cancelled Error - Implemented using `MockPasskeyServiceForAuth.OverrideAuthResult`

## Placeholder Tests Requiring Enhanced Mocks

### 1. Passkey-Only User on Non-Supported Browser
**Location**: Line 292 (comment in `Should_Show_Error_For_PasskeyOnly_NotSupported`)

**Current**: Test verifies passkey-only user shows passkey authentication when supported
**Needed**: Verify error message when passkey-only user accesses from non-supported browser

**Requirements**:
- New mock state: `existing-passkey-only-not-supported`
- MockPasskeyServiceForAuth should return `IsSupported() = false` for this state
- Expected behavior: Show error message "Your account uses passkeys, but your browser doesn't support them. Please use a compatible browser." (Auth.razor.cs line 126)

**Enhancement**: Add to `MockStates.ComponentStates["Auth"]` and implement state logic

---

### 2. ~~Passkey Error State With Retry~~ ✅ **COMPLETED**
**Location**: Line 722-749 (`Should_Show_Error_State_With_Retry`)

**Status**: Fully implemented using `MockPasskeyServiceForAuth.OverrideAuthResult`

**Implementation**:
```csharp
GetMockPasskeyService().OverrideAuthResult = (false, null, "Passkey authentication failed");
```

**Verifies**:
- Error message displays
- "Try Again" button appears
- Component handles passkey failure gracefully

---

### 3. Passkey Navigation On Success
**Location**: Line 750-758 (`Should_Navigate_On_Success`)

**Needed**: Verify successful passkey authentication navigates to main app

**Requirements**:
- MockPasskeyService.AuthenticateWithPasskey() should return success with token
- Component should store token in session storage
- Component should navigate to `/` or main app route
- Verify FakeNavigationManager received navigation request

**Enhancement**:
- MockPasskeyService should return success response
- Track navigation calls in FakeNavigationManager
- Verify session storage (MockJSRuntime already supports this)

---

### 4. Invalid Credentials Error
**Location**: Line 786-791 (`Should_Handle_Invalid_Credentials`)

**Needed**: Verify failed login shows appropriate error message

**Requirements**:
- MockAutoHostClient.AuthLoginAsync() should return `Success = false` for specific test
- Component should show error message
- Password field should be cleared (Auth.razor.cs line 193)

**Enhancement**: Add configurable failure mode to MockAutoHostClient

---

### 5. Session Expired Error
**Location**: Line 795-800 (`Should_Handle_Session_Expired`)

**Needed**: Verify session expiration during operation is handled gracefully

**Requirements**:
- Simulate API call returning 401/403 during operation
- Component should show session expired error
- User should be redirected to email step or shown re-authentication prompt

**Enhancement**: MockAutoHostClient should support throwing session expiration errors

---

### 6. ~~Passkey Cancelled Error~~ ✅ **COMPLETED**
**Location**: Line 840-868 (`Should_Handle_Passkey_Cancelled`)

**Status**: Fully implemented using `MockPasskeyServiceForAuth.OverrideAuthResult`

**Implementation**:
```csharp
GetMockPasskeyService().OverrideAuthResult = (false, null, "User denied the request for credentials.");
```

**Verifies**:
- Error container displays
- "Try Again" button appears
- Component handles user cancellation gracefully

---

## Implementation Priority

**High Priority** (Core user flows):
1. Invalid Credentials Error - Common scenario
2. ~~Passkey Cancelled Error~~ ✅ DONE - Common scenario
3. ~~Passkey Error State With Retry~~ ✅ DONE - Core passkey flow

**Medium Priority** (Edge cases):
4. Passkey Navigation On Success - Can be tested via integration tests
5. Passkey-Only User on Non-Supported Browser - Rare edge case

**Low Priority** (Complex):
6. Session Expired Error - Complex, better suited for integration tests

## Completed Implementations

### MockPasskeyServiceForAuth Enhancements ✅

Added configurable behavior properties:
- `OverrideIsSupported` - Override passkey support detection
- `OverrideAuthResult` - Configure authentication result per-test

This allows tests to configure mock behavior without creating new mock states:

```csharp
// In test setup
SetupExistingUserPasskeyOnly();
var cut = RenderComponent<Auth>();

// Configure specific behavior
GetMockPasskeyService().OverrideAuthResult = (false, null, "User denied the request");

// Now test the UI's response to that behavior
```

**Key Insight**: Mocking passkeys doesn't require crypto - just return success/failure tuples to test UI flows!

## Mock Enhancements Needed

### MockAutoHostClient
- [ ] Add configurable failure modes for authentication methods (e.g., `OverrideLoginResult`)
- [ ] Support returning `Success = false` for login/register
- [ ] Support throwing HTTP exceptions (401, 403, 500, network errors)

### ~~MockPasskeyService / MockPasskeyServiceForAuth~~ ✅ ENHANCED
- [x] Add configurable success/failure modes - **DONE** via `OverrideAuthResult`
- [x] Support throwing user cancellation exceptions - **DONE** (return error tuple)
- [x] Support returning authentication success with valid token - **DONE**
- [x] Support returning authentication failure with error message - **DONE**

### FakeNavigationManager
- [x] Track navigation calls (already implemented via `NavigateToCore`)
- [ ] Add assertion helpers to verify navigation occurred

### MockJSRuntime
- [x] Session storage support (already implemented)
- [ ] Verify session storage was set with specific values

## Next Steps

When implementing these tests:

1. **Add Mock Configuration**: Create a way to configure mock behavior per-test
   - Example: `SetupAuthState("state", configureClient: c => c.FailNextLogin())`

2. **Implement Mock Failure Modes**: Add methods to MockAutoHostClient and MockPasskeyService
   - Example: `public bool ShouldFailNextLogin { get; set; }`

3. **Write Full Tests**: Replace `Assert.True(true, "placeholder")` with actual assertions

4. **Document in UI.md**: Add patterns for testing error states to UI.md

## Related Files

- `/home/jeremy/auto/AutoWeb.Tests/Pages/AuthTests.cs` - Main test file
- `/home/jeremy/auto/AutoWeb/Tests/MockServices.cs` - Mock implementations
- `/home/jeremy/auto/AutoWeb/Pages/Auth.razor.cs` - Component logic
- `/home/jeremy/auto/claude/UI.md` - Testing documentation
