# Auth.razor Testing Modernization - Work Task Breakdown (WTB)

## Overview

This document breaks down the Auth testing modernization into discrete, verifiable tasks. Each task must be completed and validated before moving to the next.

**Principle**: Change one thing, verify everything still works, then proceed.

---

## Phase 1: Mock Infrastructure Setup ✅ COMPLETE

**Completion Date**: 2025-01-10
**Status**: All 8 tasks completed successfully
**Verification**: All 79 AuthenticationSettings tests still passing

**Key Achievements**:
- ✅ Extended MockAutoHostClient with 5 Auth states
- ✅ Created MockPasskeyServiceForAuth (reads passkey support from query string)
- ✅ Enhanced MockJSRuntime (consolidated, no duplicates)
- ✅ Updated Program.cs (learned: don't register IJSRuntime globally)
- ✅ Generalized TestPage.razor with `?component=` parameter
- ✅ Created MockStates registry for component-specific states
- ✅ Maintained backward compatibility
- ✅ Documented IJSRuntime lifetime conflict in Stupid.md

**Critical Learning**: Never register IJSRuntime in DI container - causes service lifetime conflicts. Tests provide MockJSRuntime directly.

---

## Phase 1 Tasks (COMPLETED)

### Task 1.1: Extend MockAutoHostClient for Auth
**File**: `/home/jeremy/auto/AutoWeb/Tests/MockServices.cs`

**Changes**:
- Add Auth-specific state handling to existing `MockAutoHostClient`
- Add methods: `AuthRegisterAsync`, `AuthLoginAsync`, `PasskeyChallengeAsync`, `PasskeyRegisterAsync`, `GetVersionAsync`
- Add internal state tracking: `_registeredUsers`, `_userAuthMethods`
- Add Auth state initialization logic

**Verification**:
```bash
# Build should succeed
dotnet build /home/jeremy/auto/AutoWeb/AutoWeb.csproj

# Existing AuthenticationSettings tests should still pass
dotnet test --filter "FullyQualifiedName~AuthenticationSettings"
```

**Success Criteria**:
- ✅ Build succeeds with no errors
- ✅ All 79 AuthenticationSettings tests still pass
- ✅ No new warnings introduced

**Estimated Time**: 1 hour

---

### Task 1.2: Create MockPasskeyService for Auth
**File**: `/home/jeremy/auto/AutoWeb/Tests/MockServices.cs`

**Changes**:
- Create `MockPasskeyServiceForAuth` class extending `PasskeyService`
- Override `IsSupported()` to read from query string
- Override `AuthenticateWithPasskeyAsync()` to return mock success

**Verification**:
```bash
# Build should succeed
dotnet build /home/jeremy/auto/AutoWeb/AutoWeb.csproj

# Check for compilation errors
echo "✓ Build successful"
```

**Success Criteria**:
- ✅ Build succeeds
- ✅ No impact on existing tests (not registered yet)

**Estimated Time**: 30 minutes

---

### Task 1.3: Create MockJSRuntimeForAuth
**File**: `/home/jeremy/auto/AutoWeb/Tests/MockJSRuntimeForAuth.cs` (new file)

**Changes**:
- Create mock implementing `IJSRuntime`
- Implement `sessionStorage.setItem/getItem`
- Implement `PasskeySupport.createPasskey` returning mock data
- Implement `eval` as no-op for focus() calls

**Verification**:
```bash
# Build should succeed
dotnet build /home/jeremy/auto/AutoWeb/AutoWeb.csproj

# Verify new file compiles
ls -la /home/jeremy/auto/AutoWeb/Tests/MockJSRuntimeForAuth.cs
```

**Success Criteria**:
- ✅ Build succeeds
- ✅ New file exists and compiles
- ✅ No impact on existing tests

**Estimated Time**: 45 minutes

---

### Task 1.4: Update Program.cs Mock Registration
**File**: `/home/jeremy/auto/AutoWeb/Program.cs`

**Changes**:
- Update mock registration to include Auth-specific services
- Conditionally register `MockJSRuntimeForAuth` when mocks enabled
- Keep existing AuthenticationSettings mock registration working

**Verification**:
```bash
# Build should succeed
dotnet build /home/jeremy/auto/AutoWeb/AutoWeb.csproj

# All existing tests should still pass
dotnet test --filter "FullyQualifiedName~AuthenticationSettings"

# Manually verify mocks work: Start AutoWeb and check console logs
cd /home/jeremy/auto/AutoWeb
ENABLE_MOCKS=true dotnet run --urls http://localhost:6200
# Navigate to /test?component=AuthenticationSettings&state=password-only&automated=true
# Verify: Console logs show "[MockAutoHostClient]" messages
# Ctrl+C to stop
```

**Success Criteria**:
- ✅ Build succeeds
- ✅ All 79 AuthenticationSettings tests pass
- ✅ Console logs confirm mocks are active
- ✅ AuthenticationSettings component renders correctly with mocks

**Estimated Time**: 1 hour

---

### Task 1.5: Generalize TestPage.razor (Component Parameter)
**File**: `/home/jeremy/auto/AutoWeb/Pages/TestPage.razor`

**Changes**:
- Add `[SupplyParameterFromQuery] string component` parameter
- Add conditional rendering: `@if (component == "AuthenticationSettings")` / `@else if (component == "Auth")`
- Default to "AuthenticationSettings" for backward compatibility
- Add error message for unknown components

**Verification**:
```bash
# Build should succeed
dotnet build /home/jeremy/auto/AutoWeb/AutoWeb.csproj

# ALL existing tests should still pass (critical validation)
dotnet test --filter "FullyQualifiedName~AuthenticationSettings"

# Manually verify backward compatibility
cd /home/jeremy/auto/AutoWeb
ENABLE_MOCKS=true dotnet run --urls http://localhost:6200
# Test OLD URL (should still work): /test?state=password-only&automated=true
# Test NEW URL: /test?component=AuthenticationSettings&state=password-only&automated=true
# Verify both URLs render AuthenticationSettings correctly
# Ctrl+C to stop
```

**Success Criteria**:
- ✅ Build succeeds
- ✅ All 79 AuthenticationSettings tests pass
- ✅ Old URL format still works (backward compatible)
- ✅ New URL format works
- ✅ Default component is AuthenticationSettings

**Estimated Time**: 45 minutes

---

### Task 1.6: Add Auth Component to TestPage.razor
**File**: `/home/jeremy/auto/AutoWeb/Pages/TestPage.razor`

**Changes**:
- Add `@else if (component == "Auth")` block rendering `<Auth />`
- Keep all existing code unchanged

**Verification**:
```bash
# Build should succeed
dotnet build /home/jeremy/auto/AutoWeb/AutoWeb.csproj

# ALL existing tests should still pass
dotnet test --filter "FullyQualifiedName~AuthenticationSettings"

# Manually verify Auth renders
cd /home/jeremy/auto/AutoWeb
ENABLE_MOCKS=true dotnet run --urls http://localhost:6200
# Navigate to: /test?component=Auth&state=new-user-passkey-supported&automated=true
# Verify: Auth component renders with email input
# Check console: Should see "[MockAutoHostClient]" logs
# Navigate to: /test?component=AuthenticationSettings&state=password-only&automated=true
# Verify: AuthenticationSettings still works
# Ctrl+C to stop
```

**Success Criteria**:
- ✅ Build succeeds
- ✅ All 79 AuthenticationSettings tests pass
- ✅ Auth component renders at `/test?component=Auth`
- ✅ Console logs show Auth mock initialization
- ✅ AuthenticationSettings still works

**Estimated Time**: 30 minutes

---

### Task 1.7: Implement Auth Mock States
**File**: `/home/jeremy/auto/AutoWeb/Tests/MockServices.cs`

**Changes**:
- Implement state initialization for all Auth states:
  - `new-user-passkey-supported`
  - `new-user-no-passkey-support`
  - `existing-password-only`
  - `existing-passkey-only-supported`
  - `existing-passkey-only-not-supported`
  - `existing-both-methods`
  - `disconnected`

**Verification**:
```bash
# Build should succeed
dotnet build /home/jeremy/auto/AutoWeb/AutoWeb.csproj

# ALL existing tests should still pass
dotnet test --filter "FullyQualifiedName~AuthenticationSettings"

# Manually verify each Auth state
cd /home/jeremy/auto/AutoWeb
ENABLE_MOCKS=true dotnet run --urls http://localhost:6200

# Test each state (navigate in browser):
# 1. /test?component=Auth&state=new-user-passkey-supported&automated=true
#    Expect: Email input, no error
# 2. /test?component=Auth&state=existing-password-only&automated=true
#    Expect: Email input, no error
# 3. /test?component=Auth&state=existing-both-methods&automated=true
#    Expect: Email input, no error

# Check console logs for each state:
# Should see: "[MockAutoHostClient] InitializeFromState() => {state}"

# Ctrl+C to stop
```

**Success Criteria**:
- ✅ Build succeeds
- ✅ All 79 AuthenticationSettings tests pass
- ✅ Each Auth state initializes correctly (console logs confirm)
- ✅ Auth component renders for each state without errors

**Estimated Time**: 1.5 hours

---

### Task 1.8: Test Mock State Transitions
**Goal**: Manually verify mock state changes work correctly

**Verification**:
```bash
cd /home/jeremy/auto/AutoWeb
ENABLE_MOCKS=true dotnet run --urls http://localhost:6200

# Test new user registration flow:
# 1. Navigate to: /test?component=Auth&state=new-user-passkey-supported&automated=true
# 2. Enter email: test@example.com
# 3. Click Continue
# 4. Check console: Should see "CheckUser(test@example.com) => Exists=False"
# 5. Verify: Password creation form appears
# 6. Fill passwords: "TestPassword123!" in both fields
# 7. Click "Create Account"
# 8. Check console: Should see "Register(test@example.com)" and "Login(test@example.com)"

# Ctrl+C to stop
```

**Success Criteria**:
- ✅ User registration flow works with mocks
- ✅ Console logs show correct method calls
- ✅ State transitions happen correctly
- ✅ No JavaScript errors in browser console

**Estimated Time**: 1 hour

---

## Phase 2: Unit Tests (bUnit)

### Task 2.1: Create AuthTests.cs Infrastructure
**File**: `/home/jeremy/auto/AutoWeb.Tests/Pages/AuthTests.cs` (new file)

**Changes**:
- Create test class extending `TestContext`
- Add setup methods for mock services
- Add helper methods: `SetupNewUser()`, `SetupExistingUser()`, etc.
- Create first simple test: `Should_Show_Email_Input_On_Initial_Load`

**Verification**:
```bash
# Build should succeed
dotnet build /home/jeremy/auto/AutoWeb.Tests/AutoWeb.Tests.csproj

# Run just the new test
dotnet test --filter "FullyQualifiedName~AuthTests.Should_Show_Email_Input"

# All existing tests should still pass
dotnet test --filter "FullyQualifiedName~AuthenticationSettings"
```

**Success Criteria**:
- ✅ Build succeeds
- ✅ New test passes
- ✅ All 79 existing tests still pass

**Estimated Time**: 1.5 hours

---

### Task 2.2: Implement Email Validation Tests (5 tests)
**File**: `/home/jeremy/auto/AutoWeb.Tests/Pages/AuthTests.cs`

**Changes**:
- Add 5 tests for email validation:
  1. `Should_Enable_Continue_With_Valid_Email`
  2. `Should_Show_Error_With_Invalid_Email`
  3. `Should_Disable_Continue_With_Empty_Email`
  4. `Should_Validate_Email_Realtime`
  5. `Should_Show_Red_Border_On_Invalid_Email`

**Verification**:
```bash
# Build should succeed
dotnet build /home/jeremy/auto/AutoWeb.Tests/AutoWeb.Tests.csproj

# Run new tests
dotnet test --filter "FullyQualifiedName~AuthTests" --verbosity normal

# All tests should pass (5 new + existing)
dotnet test --filter "FullyQualifiedName~Authentication"
```

**Success Criteria**:
- ✅ All 5 new tests pass
- ✅ Runtime < 100ms total for these 5
- ✅ All existing tests still pass

**Estimated Time**: 2 hours

---

### Task 2.3: Implement User Type Detection Tests (6 tests)
**File**: `/home/jeremy/auto/AutoWeb.Tests/Pages/AuthTests.cs`

**Changes**:
- Add 6 tests for user type detection after clicking Continue:
  1. `Should_Show_MethodSelection_For_NewUser_With_PasskeySupport`
  2. `Should_Show_Password_For_NewUser_Without_PasskeySupport`
  3. `Should_Show_Password_For_ExistingUser_PasswordOnly`
  4. `Should_Show_Passkey_For_ExistingUser_PasskeyOnly`
  5. `Should_Show_MethodSelection_For_ExistingUser_Both`
  6. `Should_Show_Error_For_PasskeyOnly_NotSupported`

**Verification**:
```bash
# Run new tests
dotnet test --filter "FullyQualifiedName~AuthTests" --verbosity normal

# Verify runtime
# Expected: ~200ms total for all 11 tests so far
```

**Success Criteria**:
- ✅ All 6 new tests pass
- ✅ Total runtime < 200ms for 11 tests
- ✅ All existing tests pass

**Estimated Time**: 2.5 hours

---

### Task 2.4: Implement Password Step Logic Tests (8 tests)
**File**: `/home/jeremy/auto/AutoWeb.Tests/Pages/AuthTests.cs`

**Changes**:
- Add 8 tests for password step behavior:
  1. `Should_Show_CreatePassword_For_NewUser`
  2. `Should_Show_ConfirmPassword_For_NewUser`
  3. `Should_Show_Password_Only_For_ExistingUser`
  4. `Should_Disable_Submit_When_Passwords_Mismatch`
  5. `Should_Disable_Submit_When_Password_Empty`
  6. `Should_Enable_Submit_When_Valid`
  7. `Should_Show_Change_Button`
  8. `Should_Show_Correct_Button_Text`

**Verification**:
```bash
dotnet test --filter "FullyQualifiedName~AuthTests" --verbosity normal

# Expected: 19 tests total, runtime < 350ms
```

**Success Criteria**:
- ✅ All 8 new tests pass
- ✅ Total 19 tests, runtime < 350ms
- ✅ All existing tests pass

**Estimated Time**: 3 hours

---

### Task 2.5: Implement Method Selection Tests (4 tests)
**File**: `/home/jeremy/auto/AutoWeb.Tests/Pages/AuthTests.cs`

**Changes**:
- Add 4 tests for method selection screen:
  1. `Should_Show_Both_Options_When_User_Has_Both`
  2. `Should_Navigate_To_Password_When_Password_Selected`
  3. `Should_Trigger_Passkey_When_Passkey_Selected_Existing`
  4. `Should_Trigger_Registration_When_Passkey_Selected_New`

**Verification**:
```bash
dotnet test --filter "FullyQualifiedName~AuthTests" --verbosity normal

# Expected: 23 tests total, runtime < 450ms
```

**Success Criteria**:
- ✅ All 4 new tests pass
- ✅ Total 23 tests, runtime < 450ms
- ✅ All existing tests pass

**Estimated Time**: 2 hours

---

### Task 2.6: Implement Passkey Flow Tests (5 tests)
**File**: `/home/jeremy/auto/AutoWeb.Tests/Pages/AuthTests.cs`

**Changes**:
- Add 5 tests for passkey authentication:
  1. `Should_Auto_Trigger_For_PasskeyOnly_User`
  2. `Should_Show_Waiting_State`
  3. `Should_Show_Error_State_With_Retry`
  4. `Should_Show_Password_Fallback_When_Available`
  5. `Should_Navigate_On_Success`

**Verification**:
```bash
dotnet test --filter "FullyQualifiedName~AuthTests" --verbosity normal

# Expected: 28 tests total, runtime < 550ms
```

**Success Criteria**:
- ✅ All 5 new tests pass
- ✅ Total 28 tests, runtime < 550ms
- ✅ All existing tests pass

**Estimated Time**: 2.5 hours

---

### Task 2.7: Implement Error Handling Tests (4 tests)
**File**: `/home/jeremy/auto/AutoWeb.Tests/Pages/AuthTests.cs`

**Changes**:
- Add 4 tests for error scenarios:
  1. `Should_Show_Network_Error_Message`
  2. `Should_Handle_Invalid_Credentials`
  3. `Should_Handle_Session_Expired`
  4. `Should_Handle_Passkey_Cancelled`

**Verification**:
```bash
dotnet test --filter "FullyQualifiedName~AuthTests" --verbosity normal

# Expected: 32 tests total, runtime < 650ms
dotnet test --filter "FullyQualifiedName~Authentication"
```

**Success Criteria**:
- ✅ All 4 new tests pass
- ✅ Total 32 Auth unit tests, runtime < 1 second
- ✅ All 111+ tests pass (32 Auth + 79 AuthenticationSettings)

**Estimated Time**: 2 hours

---

## Phase 3: Render Tests (bUnit)

### Task 3.1: Create AuthRenderTests.cs
**File**: `/home/jeremy/auto/AutoWeb.Tests/Pages/AuthRenderTests.cs` (new file)

**Changes**:
- Create test class extending `TestContext`
- Add setup infrastructure
- Create first test: `Should_Render_Email_Input_With_Correct_Attributes`

**Verification**:
```bash
dotnet build /home/jeremy/auto/AutoWeb.Tests/AutoWeb.Tests.csproj

dotnet test --filter "FullyQualifiedName~AuthRenderTests"

# All tests should still pass
dotnet test
```

**Success Criteria**:
- ✅ Build succeeds
- ✅ First render test passes
- ✅ All existing tests pass

**Estimated Time**: 1 hour

---

### Task 3.2: Implement Email Step Structure Tests (3 tests)
**File**: `/home/jeremy/auto/AutoWeb.Tests/Pages/AuthRenderTests.cs`

**Changes**:
- Add 3 tests validating HTML structure:
  1. `Should_Render_Email_Input_Structure`
  2. `Should_Render_Continue_Button_Structure`
  3. `Should_Render_Status_Indicator_Structure`

**Verification**:
```bash
dotnet test --filter "FullyQualifiedName~AuthRenderTests" --verbosity normal

# Expected: 4 tests, runtime < 100ms
```

**Success Criteria**:
- ✅ All 3 new tests pass
- ✅ Runtime < 100ms for 4 tests
- ✅ All existing tests pass

**Estimated Time**: 1.5 hours

---

### Task 3.3: Implement Password Step Structure Tests (3 tests)
**File**: `/home/jeremy/auto/AutoWeb.Tests/Pages/AuthRenderTests.cs`

**Changes**:
- Add 3 tests:
  1. `Should_Render_Password_Inputs_Structure`
  2. `Should_Render_Submit_Button_Structure`
  3. `Should_Render_Email_Display_With_Change_Button`

**Verification**:
```bash
dotnet test --filter "FullyQualifiedName~AuthRenderTests" --verbosity normal

# Expected: 7 tests, runtime < 150ms
```

**Success Criteria**:
- ✅ All 3 new tests pass
- ✅ Total 7 tests, runtime < 150ms
- ✅ All existing tests pass

**Estimated Time**: 1.5 hours

---

### Task 3.4: Implement Method Selection Structure Tests (2 tests)
**File**: `/home/jeremy/auto/AutoWeb.Tests/Pages/AuthRenderTests.cs`

**Changes**:
- Add 2 tests:
  1. `Should_Render_Two_Option_Buttons`
  2. `Should_Render_Correct_Icons_And_Text`

**Verification**:
```bash
dotnet test --filter "FullyQualifiedName~AuthRenderTests" --verbosity normal

# Expected: 9 tests, runtime < 200ms
```

**Success Criteria**:
- ✅ All 2 new tests pass
- ✅ Total 9 tests, runtime < 200ms
- ✅ All existing tests pass

**Estimated Time**: 1 hour

---

### Task 3.5: Implement Passkey Step Structure Tests (2 tests)
**File**: `/home/jeremy/auto/AutoWeb.Tests/Pages/AuthRenderTests.cs`

**Changes**:
- Add 2 tests:
  1. `Should_Render_Passkey_Icon_And_Message`
  2. `Should_Render_Retry_And_Fallback_Buttons`

**Verification**:
```bash
dotnet test --filter "FullyQualifiedName~AuthRenderTests" --verbosity normal

# Expected: 11 tests, runtime < 250ms

# Run all tests to ensure nothing broke
dotnet test
```

**Success Criteria**:
- ✅ All 2 new tests pass
- ✅ Total 11 Auth render tests, runtime < 250ms
- ✅ All tests pass (32 unit + 11 render + 79 AuthSettings = 122 tests)

**Estimated Time**: 1 hour

---

## Phase 4: Layout Tests (Playwright)

### Task 4.1: Create AuthLayoutTests.cs
**File**: `/home/jeremy/auto/AutoWeb.Tests/Pages/AuthLayoutTests.cs` (new file)

**Changes**:
- Create test class with `[Collection("Playwright")]`
- Add constructor taking `PlaywrightFixture`
- Create helper method `NavigateToState(stateName)`
- Add first test: `Layout_InitialConnected_ShowsEmailInput`

**Verification**:
```bash
dotnet build /home/jeremy/auto/AutoWeb.Tests/AutoWeb.Tests.csproj

# Run just the new test
dotnet test --filter "FullyQualifiedName~AuthLayoutTests.Layout_InitialConnected"

# All existing tests should still pass (critical!)
dotnet test --filter "FullyQualifiedName~AuthenticationSettings"
```

**Success Criteria**:
- ✅ Build succeeds
- ✅ First layout test passes (~2-3 seconds)
- ✅ All existing tests pass
- ✅ PlaywrightFixture correctly shared with AuthenticationSettings tests

**Estimated Time**: 1.5 hours

---

### Task 4.2: Implement Core Layout Tests (5 tests)
**File**: `/home/jeremy/auto/AutoWeb.Tests/Pages/AuthLayoutTests.cs`

**Changes**:
- Add 5 tests for core Auth states:
  1. `Layout_InitialDisconnected_ShowsDisabledState`
  2. `Layout_EmailEntered_ShowsValidEmail`
  3. `Layout_EmailInvalid_ShowsErrorMessage`
  4. `Layout_NewUserPasswordStep_ShowsCreateForm`
  5. `Layout_ExistingUserPasswordStep_ShowsLoginForm`

**Verification**:
```bash
dotnet test --filter "FullyQualifiedName~AuthLayoutTests" --verbosity normal

# Expected: 6 tests, runtime ~12 seconds

# All existing tests must still pass
dotnet test
```

**Success Criteria**:
- ✅ All 5 new tests pass
- ✅ Total 6 Auth layout tests, runtime ~12 seconds
- ✅ All existing tests pass

**Estimated Time**: 2 hours

---

### Task 4.3: Implement Method Selection & Passkey Tests (5 tests)
**File**: `/home/jeremy/auto/AutoWeb.Tests/Pages/AuthLayoutTests.cs`

**Changes**:
- Add 5 more tests:
  1. `Layout_NewUserMethodSelection_ShowsBothOptions`
  2. `Layout_ExistingUserMethodSelection_ShowsBothOptions`
  3. `Layout_PasskeyWaiting_ShowsWaitingState`
  4. `Layout_PasskeyError_ShowsRetryOption`
  5. `Layout_PasskeyOnlyNoSupport_ShowsError`

**Verification**:
```bash
dotnet test --filter "FullyQualifiedName~AuthLayoutTests" --verbosity normal

# Expected: 11 tests, runtime ~22 seconds

dotnet test
```

**Success Criteria**:
- ✅ All 5 new tests pass
- ✅ Total 11 Auth layout tests, runtime ~22 seconds
- ✅ All tests pass (43 Auth + 79 AuthSettings = 122 tests)

**Estimated Time**: 2.5 hours

---

## Phase 5: Interaction Tests (Playwright)

### Task 5.1: Create AuthInteractionTests.cs
**File**: `/home/jeremy/auto/AutoWeb.Tests/Pages/AuthInteractionTests.cs` (new file)

**Changes**:
- Create test class with `[Collection("Playwright")]`
- Add constructor and helpers
- Create first test: `NewUser_PasswordRegistration_Success`

**Verification**:
```bash
dotnet build /home/jeremy/auto/AutoWeb.Tests/AutoWeb.Tests.csproj

dotnet test --filter "FullyQualifiedName~AuthInteractionTests.NewUser_PasswordRegistration"

# Critical: All existing tests must pass
dotnet test --filter "FullyQualifiedName~AuthenticationSettings"
```

**Success Criteria**:
- ✅ Build succeeds
- ✅ First interaction test passes (~3-4 seconds)
- ✅ All existing tests pass
- ✅ Screenshot saved on failure

**Estimated Time**: 2 hours

---

### Task 5.2: Implement New User Workflows (2 tests)
**File**: `/home/jeremy/auto/AutoWeb.Tests/Pages/AuthInteractionTests.cs`

**Changes**:
- Add 2 tests:
  1. `NewUser_PasskeyRegistration_Success` (already have password from 5.1)
  2. `NewUser_MethodSelection_ToPassword_Success`

**Verification**:
```bash
dotnet test --filter "FullyQualifiedName~AuthInteractionTests" --verbosity normal

# Expected: 3 tests, runtime ~10 seconds

dotnet test
```

**Success Criteria**:
- ✅ All 2 new tests pass
- ✅ Total 3 interaction tests, runtime ~10 seconds
- ✅ All tests pass

**Estimated Time**: 2 hours

---

### Task 5.3: Implement Existing User Workflows (3 tests)
**File**: `/home/jeremy/auto/AutoWeb.Tests/Pages/AuthInteractionTests.cs`

**Changes**:
- Add 3 tests:
  1. `ExistingUser_PasswordLogin_Success`
  2. `ExistingUser_PasskeyAuth_Success`
  3. `ExistingUser_PasskeyAutoTrigger_Success`

**Verification**:
```bash
dotnet test --filter "FullyQualifiedName~AuthInteractionTests" --verbosity normal

# Expected: 6 tests, runtime ~18 seconds

dotnet test
```

**Success Criteria**:
- ✅ All 3 new tests pass
- ✅ Total 6 interaction tests, runtime ~18 seconds
- ✅ All tests pass

**Estimated Time**: 2.5 hours

---

### Task 5.4: Implement Method Selection Workflows (2 tests)
**File**: `/home/jeremy/auto/AutoWeb.Tests/Pages/AuthInteractionTests.cs`

**Changes**:
- Add 2 tests:
  1. `ExistingUser_MethodSelection_ToPassword_Success`
  2. `ExistingUser_MethodSelection_ToPasskey_Success`

**Verification**:
```bash
dotnet test --filter "FullyQualifiedName~AuthInteractionTests" --verbosity normal

# Expected: 8 tests, runtime ~24 seconds

dotnet test
```

**Success Criteria**:
- ✅ All 2 new tests pass
- ✅ Total 8 interaction tests, runtime ~24 seconds
- ✅ All tests pass

**Estimated Time**: 2 hours

---

### Task 5.5: Implement Error Recovery Workflow (1 test)
**File**: `/home/jeremy/auto/AutoWeb.Tests/Pages/AuthInteractionTests.cs`

**Changes**:
- Add 1 comprehensive test:
  1. `ExistingUser_PasskeyFailure_FallbackToPassword_Success`

**Verification**:
```bash
dotnet test --filter "FullyQualifiedName~AuthInteractionTests" --verbosity normal

# Expected: 9 tests, runtime ~27 seconds

# Run complete test suite
dotnet test --verbosity minimal

# Expected final counts:
# - Auth Unit: 32 tests (~1s)
# - Auth Render: 11 tests (~250ms)
# - Auth Layout: 11 tests (~22s)
# - Auth Interaction: 9 tests (~27s)
# - AuthenticationSettings: 79 tests (~17s)
# TOTAL: 142 tests in ~67 seconds
```

**Success Criteria**:
- ✅ Final test passes
- ✅ Total 9 Auth interaction tests, runtime ~27 seconds
- ✅ All 142 tests pass
- ✅ Total test suite runtime < 70 seconds

**Estimated Time**: 2 hours

---

## Phase 6: Documentation

### Task 6.1: Create Auth Component Documentation
**File**: `/home/jeremy/auto/claude/ui/Auth/Auth.md` (new file)

**Changes**:
- Document all Auth mock states
- Document Auth state machine
- Document test coverage by layer
- Include test examples
- Document known issues and gotchas
- Add debugging tips

**Verification**:
```bash
# Verify file exists and is well-formed
cat /home/jeremy/auto/claude/ui/Auth/Auth.md | head -20

# Document should include:
grep "Mock States" /home/jeremy/auto/claude/ui/Auth/Auth.md
grep "State Machine" /home/jeremy/auto/claude/ui/Auth/Auth.md
grep "Test Coverage" /home/jeremy/auto/claude/ui/Auth/Auth.md
```

**Success Criteria**:
- ✅ Documentation file created
- ✅ All mock states documented
- ✅ State machine diagram included
- ✅ Test examples provided
- ✅ Debugging section present

**Estimated Time**: 2 hours

---

### Task 6.2: Update UI.md with Auth Example
**File**: `/home/jeremy/auto/claude/UI.md`

**Changes**:
- Add Auth as second example component (after AuthenticationSettings)
- Reference Auth.md for details
- Update component testing checklist

**Verification**:
```bash
grep "Auth.razor" /home/jeremy/auto/claude/UI.md
```

**Success Criteria**:
- ✅ Auth mentioned in UI.md
- ✅ Link to Auth.md included
- ✅ Checklist updated

**Estimated Time**: 30 minutes

---

### Task 6.3: Update Auth-Testing-Modernization.md (This Document)
**File**: `/home/jeremy/auto/claude/tasks/Auth-Testing-Modernization.md`

**Changes**:
- Mark all phases as complete
- Add "Completed" section with metrics
- Document actual vs estimated time
- Add lessons learned

**Verification**:
```bash
# Check completion status
grep "✅" /home/jeremy/auto/claude/tasks/Auth-Testing-Modernization.md | wc -l
```

**Success Criteria**:
- ✅ All checkboxes marked complete
- ✅ Metrics documented
- ✅ Lessons learned added

**Estimated Time**: 30 minutes

---

## Validation Checkpoints

### After Phase 1 (Mock Infrastructure)
```bash
# Must pass all these checks:
dotnet build /home/jeremy/auto/AutoWeb/AutoWeb.csproj
dotnet test --filter "FullyQualifiedName~AuthenticationSettings"
cd /home/jeremy/auto/AutoWeb && ENABLE_MOCKS=true dotnet run --urls http://localhost:6200
# Manually verify: /test?component=Auth&state=new-user-passkey-supported&automated=true
# Stop server: Ctrl+C
```

**Expected Results**:
- ✅ Build succeeds
- ✅ 79 AuthenticationSettings tests pass
- ✅ Auth renders with mocks
- ✅ Console logs show mock initialization

---

### After Phase 2 (Unit Tests)
```bash
dotnet test --filter "FullyQualifiedName~AuthTests" --verbosity normal
dotnet test
```

**Expected Results**:
- ✅ 32 Auth unit tests pass in < 1 second
- ✅ All 111 tests pass (32 Auth + 79 AuthenticationSettings)

---

### After Phase 3 (Render Tests)
```bash
dotnet test --filter "FullyQualifiedName~AuthRenderTests" --verbosity normal
dotnet test
```

**Expected Results**:
- ✅ 11 Auth render tests pass in < 250ms
- ✅ All 122 tests pass (43 Auth + 79 AuthenticationSettings)

---

### After Phase 4 (Layout Tests)
```bash
dotnet test --filter "FullyQualifiedName~AuthLayoutTests" --verbosity normal
dotnet test --verbosity minimal
```

**Expected Results**:
- ✅ 11 Auth layout tests pass in ~22 seconds
- ✅ All 133 tests pass (54 Auth + 79 AuthenticationSettings)
- ✅ Total runtime < 45 seconds

---

### After Phase 5 (Interaction Tests)
```bash
dotnet test --verbosity minimal
```

**Expected Results**:
- ✅ 9 Auth interaction tests pass in ~27 seconds
- ✅ All 142 tests pass (63 Auth + 79 AuthenticationSettings)
- ✅ Total runtime < 70 seconds
- ✅ No test flakiness (run 3 times to verify)

---

### Final Validation
```bash
# Run complete suite 3 times to verify stability
for i in 1 2 3; do
  echo "Run $i:"
  dotnet test --verbosity minimal | grep "Passed:"
  sleep 5
done

# All runs should show: "Passed: 142"
```

**Expected Results**:
- ✅ All 3 runs: 142 tests pass
- ✅ No failures, no flakiness
- ✅ Runtime consistent (~60-70 seconds)

---

## Critical Success Factors

### 1. Test Existing Tests After Every Change
**Rule**: After modifying shared infrastructure (TestPage.razor, MockServices.cs, Program.cs), ALWAYS run:
```bash
dotnet test --filter "FullyQualifiedName~AuthenticationSettings"
```

### 2. Verify Manually After Mock Changes
**Rule**: After modifying mocks, ALWAYS manually test in browser:
```bash
cd /home/jeremy/auto/AutoWeb
ENABLE_MOCKS=true dotnet run --urls http://localhost:6200
# Navigate and verify, then Ctrl+C
```

### 3. Use Playwright Collection Fixture
**Rule**: All Playwright tests MUST use `[Collection("Playwright")]` to share server instance

### 4. Keep Timeouts Short
**Rule**: Use 100-200ms timeouts for local Blazor with mocks, not seconds

### 5. Look at Screenshots First
**Rule**: When test fails, open `/tmp/test-failure.png` BEFORE debugging code

---

## Time Tracking

### Estimated Total Time
| Phase | Estimated |
|-------|-----------|
| Phase 1: Mock Infrastructure | 6.5 hours |
| Phase 2: Unit Tests | 14 hours |
| Phase 3: Render Tests | 6 hours |
| Phase 4: Layout Tests | 4 hours |
| Phase 5: Interaction Tests | 10.5 hours |
| Phase 6: Documentation | 3 hours |
| **TOTAL** | **44 hours** |

### Actual Time (to be filled in)
| Phase | Actual | Notes |
|-------|--------|-------|
| Phase 1 | ___ hours | |
| Phase 2 | ___ hours | |
| Phase 3 | ___ hours | |
| Phase 4 | ___ hours | |
| Phase 5 | ___ hours | |
| Phase 6 | ___ hours | |
| **TOTAL** | ___ hours | |

---

## Next Steps

1. ✅ **Review this WTB** - Ensure all tasks are clear and verifiable
2. ⏳ **Start with Task 1.1** - Extend MockAutoHostClient
3. ⏳ **Track progress** - Check off tasks as completed
4. ⏳ **Update actual time** - Record time spent on each task
5. ⏳ **Document issues** - Note any blockers or surprises

---

## Notes Section

Use this space to track issues, discoveries, or deviations from the plan:

### Phase 1 Notes
-

### Phase 2 Notes
-

### Phase 3 Notes
-

### Phase 4 Notes
-

### Phase 5 Notes
-

### Phase 6 Notes
-

---

## Completion Checklist

- [ ] Phase 1: All 8 tasks complete and verified
- [ ] Phase 2: All 7 tasks complete and verified
- [ ] Phase 3: All 5 tasks complete and verified
- [ ] Phase 4: All 3 tasks complete and verified
- [ ] Phase 5: All 5 tasks complete and verified
- [ ] Phase 6: All 3 tasks complete and verified
- [ ] Final Validation: All 142 tests passing consistently
- [ ] Documentation: All docs updated and accurate
- [ ] Time Tracking: Actual times recorded
- [ ] Lessons Learned: Documented in Auth-Testing-Modernization.md
