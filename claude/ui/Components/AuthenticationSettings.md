# AuthenticationSettings Component

## Purpose
The AuthenticationSettings component allows users to manage their authentication methods (passwords and passkeys). Users must always have at least one authentication method available.

## Functional Requirements

### Core Business Rule
**Users MUST have at least one authentication method at all times.**

This means:
- Cannot remove password if no passkeys exist
- Cannot remove last passkey if no password exists
- UI enforces this by disabling delete/remove buttons appropriately

### Valid States
1. **password-only**: User has password, no passkeys
2. **single-passkey**: User has one passkey, no password
3. **multiple-passkeys**: User has 2+ passkeys, no password
4. **password-and-passkeys**: User has both password and passkeys

**INVALID STATE** (never allowed):
- ~~**no-auth**: User has neither password nor passkeys~~ ❌

## Component Structure

### Password Section
Located at top of component.

**When password exists (`hasPassword = true`):**
- Shows: "Password authentication is enabled for your account."
- Button: "Remove Password" (enabled ONLY if passkeys exist)
- Button text changes to "Add passkey first" if no passkeys (disabled)

**When no password (`hasPassword = false`):**
- Shows: "No password set. You're using passkey-only authentication."
- Button: "Create Password" (always enabled)

**Password Creation Dialog:**
- Appears when clicking "Create Password"
- Two password fields: "Password" and "Confirm Password"
- Create button enabled ONLY when both fields filled and match
- Cancel button to close dialog
- Shows error if passwords don't match

### Passkeys Section
Located below password section.

**When passkeys exist:**
- Shows list of passkeys with:
  - Device name (e.g., "iPhone 15 Pro")
  - Created date
  - Last used time (relative, e.g., "2 hours ago")
  - Delete button (enabled/disabled based on rules)

**Delete button logic:**
- Enabled if: user has password OR has multiple passkeys
- Disabled if: no password AND this is the last passkey
- Button text changes to "Last one" when disabled

**When loading:**
- Shows: "Loading passkeys..."

**When no passkeys:**
- Shows: "No passkeys registered yet."

**Add Passkey button:**
- Shown below passkey list
- Disabled if passkeys not supported by browser
- Text: "+ Add Passkey"

### Success/Error Messages
- Success messages appear at bottom (green background)
- Error messages appear at bottom (red background)
- Auto-dismiss after 5 seconds

## State Transitions

### Create Password Flow
```
single-passkey → [Click "Create Password"] → [Show Dialog] →
[Fill Fields] → [Click "Create"] → password-and-passkeys
```

**UI Changes:**
- "Create Password" button → "Remove Password" button
- Passkey delete buttons become enabled

### Remove Password Flow
```
password-and-passkeys → [Click "Remove Password"] → password-only
```

**UI Changes:**
- "Remove Password" button → "Create Password" button
- Passkey delete buttons become disabled (if only one passkey)

### Delete Passkey Flow (with password)
```
password-and-passkeys → [Click Delete on passkey] → password-only
```

**UI Changes:**
- Passkey removed from list
- Shows "No passkeys registered yet" if that was last passkey
- "Remove Password" button changes to "Add passkey first" (disabled)
- Success message: "Passkey removed successfully"

### Delete Passkey Flow (with multiple passkeys)
```
multiple-passkeys → [Click Delete] → single-passkey OR multiple-passkeys
```

**UI Changes:**
- Passkey removed from list
- If now only one passkey: Delete button becomes disabled ("Last one")
- Success message: "Passkey removed successfully"

## Test Infrastructure

### Mock States
All mocks read the `?state=` query parameter to determine initial data.

**State: `password-only`**
```csharp
_hasPassword = true;
_passkeys = new List<PasskeyInfo>();
```

**State: `single-passkey`**
```csharp
_hasPassword = false;
_passkeys = new List<PasskeyInfo>
{
    new()
    {
        Id = 1,
        DeviceName = "iPhone 15 Pro",
        CreatedAt = new DateTimeOffset(2025, 9, 1, 10, 0, 0, TimeSpan.FromHours(-7)),
        LastUsedAt = DateTimeOffset.Now.AddHours(-2)
    }
};
```

**State: `multiple-passkeys`**
```csharp
_hasPassword = false;
_passkeys = new List<PasskeyInfo>
{
    new() { Id = 1, DeviceName = "iPhone 15 Pro", ... },
    new() { Id = 2, DeviceName = "MacBook Pro", ... }
};
```

**State: `password-and-passkeys`**
```csharp
_hasPassword = true;
_passkeys = new List<PasskeyInfo>
{
    new() { Id = 1, DeviceName = "iPhone 15 Pro", ... }
};
```

### Mock Services

**MockAutoHostClient** (`/home/jeremy/auto/AutoWeb/Tests/MockServices.cs`)
- Implements `IAutoHostClient`
- Stateful: tracks `_hasPassword` and `_passkeys`
- Initializes from query string state
- Updates internal state on operations
- Logs all operations to console

**Key Methods:**
- `AuthCheckUserAsync()`: Returns current password/passkey status
- `AuthCreatePasswordAsync()`: Sets `_hasPassword = true`
- `AuthRemovePasswordAsync()`: Sets `_hasPassword = false`
- `PasskeyDeleteAsync()`: Removes passkey from `_passkeys` list
- `PasskeyListAsync()`: Returns current `_passkeys` list

**MockPasskeyService** (`/home/jeremy/auto/AutoWeb/Tests/MockServices.cs`)
- Implements `PasskeyService` (inherits, overrides `IsSupported()`)
- Always returns `true` for `IsSupported()` (browser supports passkeys)

**MockJSRuntime** (`/home/jeremy/auto/AutoWeb/Tests/MockJSRuntime.cs`)
- Implements `IJSRuntime`
- Returns "test@example.com" for `sessionStorage.getItem("userEmail")`

### Test Page
**URL**: `http://localhost:6200/test?state={STATE}&automated=true`

- `state`: One of `password-only`, `single-passkey`, `multiple-passkeys`, `password-and-passkeys`
- `automated=true`: Hides test harness UI, shows only component

**Example URLs:**
- `http://localhost:6200/test?state=password-only&automated=true`
- `http://localhost:6200/test?state=single-passkey&automated=true`

### Test Files

**Unit Tests** (`AuthenticationSettingsTests.cs`)
- Uses bUnit
- Tests component logic in isolation
- Fast (10-50ms per test)
- No browser required

**Render Tests** (`AuthenticationSettingsRenderTests.cs`)
- Uses bUnit
- Tests HTML structure, CSS classes
- Fast (10-50ms per test)
- No browser required

**Layout Tests** (`AuthenticationSettingsLayoutTests.cs`)
- Uses Playwright
- Tests actual browser rendering
- Validates element visibility
- ~1-3 seconds per test

**Interaction Tests** (`AuthenticationSettingsInteractionTests.cs`)
- Uses Playwright
- Tests complete user workflows
- Validates state transitions
- ~3-5 seconds per test

## Test Coverage

### Unit Tests (10+ tests)
- ✓ Component initialization
- ✓ Password creation dialog shows/hides
- ✓ Password validation logic
- ✓ Delete button enable/disable logic
- ✓ State transitions
- ✓ Event handler invocation

### Render Tests (5+ tests)
- ✓ Password section HTML structure
- ✓ Passkey list HTML structure
- ✓ Button classes and attributes
- ✓ Conditional rendering (dialog, messages)
- ✓ Accessibility attributes

### Layout Tests (5 tests, one per state)
- ✓ `password-only` state renders correctly
- ✓ `single-passkey` state renders correctly
- ✓ `multiple-passkeys` state renders correctly
- ✓ `password-and-passkeys` state renders correctly
- ✓ Empty/loading states render correctly

### Interaction Tests (3 tests)
- ✓ Create password workflow
- ✓ Remove password workflow
- ✓ Delete passkey workflow
- ✓ **CreatePassword_ThenDeletePasskey_ShouldShowNoPasskeysMessage** (reproduces user bug)

## Key Interaction Test: CreatePassword_ThenDeletePasskey

**Purpose**: Reproduce user-reported bug where deleting last passkey after creating password shows correct "No passkeys" message and correct button state.

**Test File**: `/home/jeremy/auto/AutoWeb.Tests/Components/AuthenticationSettingsInteractionTests.cs:25`

**Workflow:**
1. Start in `single-passkey` state
2. Click "Create Password" button
3. Fill password fields with "TestPassword123!"
4. Click "Create" button (exact match to avoid "Create Password")
5. Verify "Remove Password" button appears
6. Verify Delete button is enabled
7. Click Delete on passkey
8. Verify success message "Passkey removed successfully"
9. Verify passkey removed from DOM
10. Verify "No passkeys registered yet" message
11. Verify button changed to "Add passkey first" (disabled)

**Key Learnings:**
- Use `GetByRole(AriaRole.Button, new() { Name = "Create", Exact = true })` for exact button matching
- Use `PressSequentiallyAsync()` not `FillAsync()` for Blazor inputs
- Use 200ms timeouts, not 3000ms (local Blazor is fast)
- Look at screenshot FIRST when test fails

**Runtime**: ~3 seconds

## Known Issues & Gotchas

### Playwright + Blazor Gotchas

**Issue 1: FillAsync doesn't work with @bind:event="oninput"**
```csharp
// ❌ WRONG - doesn't trigger Blazor binding
await input.FillAsync("password");

// ✓ CORRECT - types character by character
await input.PressSequentiallyAsync("password");
```

**Issue 2: has-text() does substring matching**
```csharp
// ❌ WRONG - matches both "Create" and "Create Password"
page.Locator("button:has-text('Create')")

// ✓ CORRECT - exact match
page.GetByRole(AriaRole.Button, new() { Name = "Create", Exact = true })
```

**Issue 3: Button class changes after state transition**
- Initial "Create Password" button has `bg-green-600`
- After password created, "Remove Password" button has `bg-red-600`
- Don't rely on CSS classes for selectors, use text/role

### Component Behavior

**Issue 1: Button text changes based on state**
- "Remove Password" → "Add passkey first" when no passkeys
- "Delete" → "Last one" when last passkey and no password
- Tests must verify CORRECT button text for each state

**Issue 2: State updates are asynchronous**
- After creating password, component calls `LoadAuthenticationMethods()`
- After deleting passkey, component calls `LoadAuthenticationMethods()`
- Tests must wait for state updates, but only 100-200ms needed

**Issue 3: Success messages auto-dismiss**
- Messages stay visible for 5 seconds
- Tests must verify quickly (within 200ms of action)
- Don't rely on message for state verification, check actual button state

## Debugging Tips

### Test Failing? Follow This Order:
1. **Look at screenshot**: `/tmp/test-failure.png`
2. Compare to expected state
3. Check browser console logs (enabled in test)
4. Check mock console logs
5. Verify selector is correct
6. Verify timeout is reasonable (100-200ms)

### Common Selector Issues:
```csharp
// Finding password inputs
page.Locator("input[type='password']").First         // First field
page.Locator("input[type='password']").Nth(1)        // Second field

// Finding buttons
page.GetByRole(AriaRole.Button, new() { Name = "Create", Exact = true })
page.Locator("button:has-text('Remove Password')")  // OK - "Remove Password" is unique

// Finding delete button in passkey item
page.Locator("button:has-text('Delete')").First      // First passkey's delete button

// Finding messages
page.Locator("text=Password created successfully")
page.Locator("text=Passkey removed successfully")
page.Locator("text=No passkeys registered yet")
```

### Mock Debugging:
All mocks log to browser console:
```
[MockAutoHostClient] GetInitialState() => single-passkey
[MockAutoHostClient] InitializeFromState() => single-passkey
[AuthSettings] CheckUser response: Has Password=False, Has Passkeys=True
[MockAutoHostClient] Created password, hasPassword=True
[MockAutoHostClient] Deleted passkey ID=1, remaining count=0
```

Enable console logging in tests:
```csharp
page.Console += (_, msg) => _output.WriteLine($"[Browser {msg.Type}] {msg.Text}");
```

## Performance Metrics

### Actual Performance (as measured)
- **Single interaction test**: 3 seconds
- **Complete test suite** (all layers): ~23 seconds

### Time Breakdown (CreatePassword_ThenDeletePasskey test)
- Browser startup: ~1 second (shared across test class)
- Navigate to state: ~500ms
- Fill password fields: ~100ms
- Click Create: ~50ms
- Verify state: ~50ms
- Click Delete: ~50ms
- Verify final state: ~50ms
- **Total**: ~3 seconds

### Comparison to Full-Stack Test
- Full-stack (with backend): 30-40 seconds
- Mock-based: 3 seconds
- **Speedup**: 10x faster

## Future Improvements

### Test Coverage Gaps
- [ ] Test password validation (min length, requirements)
- [ ] Test error handling (API failures)
- [ ] Test concurrent operations
- [ ] Test browser compatibility (Safari, Firefox)
- [ ] Test passkey creation flow (requires WebAuthn mock)

### Infrastructure Improvements
- [ ] Parallelize test execution
- [ ] Add visual regression testing
- [ ] Generate LayoutML for all states
- [ ] Auto-generate component documentation from tests

### Component Improvements
- [ ] Add loading state during operations
- [ ] Add confirmation dialog for delete operations
- [ ] Add password strength indicator
- [ ] Support editing passkey device names
- [ ] Add last login information

## References

**Component Source**: `/home/jeremy/auto/AutoWeb/Components/AuthenticationSettings.razor`

**Mock Source**: `/home/jeremy/auto/AutoWeb/Tests/MockServices.cs`

**Test Files**:
- Unit: `/home/jeremy/auto/AutoWeb.Tests/Components/AuthenticationSettingsTests.cs`
- Render: `/home/jeremy/auto/AutoWeb.Tests/Components/AuthenticationSettingsRenderTests.cs`
- Layout: `/home/jeremy/auto/AutoWeb.Tests/Components/AuthenticationSettingsLayoutTests.cs`
- Interaction: `/home/jeremy/auto/AutoWeb.Tests/Components/AuthenticationSettingsInteractionTests.cs`

**Test Harness**: `/home/jeremy/auto/AutoWeb/Pages/TestPage.razor`

**Documentation**:
- Testing Framework: `/home/jeremy/auto/claude/UI.md`
- Debugging Guide: `/home/jeremy/auto/claude/Reminder.md`
- Anti-Patterns: `/home/jeremy/auto/claude/Stupid.md`
