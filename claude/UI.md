# UI Testing Framework

## Overview
A comprehensive, multi-layered approach to UI component testing that provides fast feedback, complete coverage, and reproducible results. This system uses **mock-based isolated testing** for speed combined with **browser-based validation** for accuracy.

## Philosophy

### The Problem with Traditional UI Testing
- Full-stack tests are slow (10-40 seconds per test)
- Hard to isolate component behavior from backend issues
- Difficult to test all possible states
- Debugging is painful (restart servers, navigate through UI, reproduce state)

### Our Solution: Multi-Layer Testing Strategy
1. **Unit Tests**: Fast, focused, test component logic in isolation
2. **Render Tests**: Validate component renders correct HTML structure
3. **Layout Tests**: Browser-based verification of visual layout with Playwright
4. **Interaction Tests**: Browser-based validation of user workflows

## Core Concepts

### 1. Test Harness Page (`TestPage.razor`)
A dedicated page in AutoWeb that:
- Renders ANY component with mock services
- Accepts component via query string (`?component=AuthenticationSettings` or `?component=Auth`)
- Accepts state via query string (`?state=single-passkey`)
- Supports automated mode (`?automated=true`) to hide test harness UI
- Provides instant component testing without backend dependencies

**Key Features:**
- **Component-agnostic**: Test any component via `?component=` parameter
- **Backward compatible**: Defaults to AuthenticationSettings if no `?component=` specified
- **State-based rendering**: Component appears in specific state instantly
- **No navigation required**: Direct URL access to any state
- **No authentication required**: Bypasses login flow
- **No backend required**: Uses mocks for all services

**URL Format:**
```
/test?component=Auth&state=new-user-passkey-supported&automated=true
/test?state=password-only&automated=true  # Defaults to AuthenticationSettings
```

### 2. Mock Service System (`MockServices.cs`)
Stateful mock implementations that:
- Return data based on query string state parameter
- Maintain internal state that changes with operations
- Support all component operations (create, read, update, delete)
- Provide console logging for debugging

**Design Principles:**
- **One mock per service**: MockAutoHostClient, MockPasskeyServiceForAuth, MockJSRuntime
- **State-driven**: Mocks read `?state=` parameter to determine initial data
- **Stateful behavior**: Mocks track changes (e.g., password created, passkey deleted)
- **Environment-based registration**: Use `ENABLE_MOCKS=true` to activate
- **IMPORTANT**: Do NOT register IJSRuntime in DI container - causes service lifetime conflicts. Tests provide MockJSRuntime directly.

### 3. Mock State Registry (`MockStates`)
Central registry defining valid states for each component:

```csharp
public static class MockStates
{
    public static readonly Dictionary<string, string[]> ComponentStates = new()
    {
        ["AuthenticationSettings"] = new[]
        {
            "password-only",
            "single-passkey",
            "multiple-passkeys",
            "password-and-passkeys"
        },
        ["Auth"] = new[]
        {
            "new-user-passkey-supported",
            "new-user-passkey-not-supported",
            "existing-password-only",
            "existing-passkey-only",
            "existing-password-and-passkey"
        }
    };

    public static string[] GetStatesForComponent(string componentName)
    {
        return ComponentStates.TryGetValue(componentName, out var states)
            ? states
            : Array.Empty<string>();
    }
}
```

**Benefits:**
- **Single source of truth**: All valid states defined in one place
- **Component-specific**: Each component has its own set of valid states
- **Type-safe**: TestPage.razor reads from registry dynamically
- **Discoverable**: Easy to see what states exist for any component

**NEVER define invalid states** (e.g., `no-auth` where user has no auth methods)

## Four-Layer Testing Strategy

### Layer 1: Unit Tests (bUnit)
**Purpose**: Test component logic, state management, event handling

**Speed**: ~10-50ms per test

**What to test:**
- Component initialization
- State changes
- Event handler logic
- Conditional rendering logic
- Parameter binding

**Example:**
```csharp
[Fact]
public void Component_InitialState_ShowsCreatePasswordButton()
{
    var cut = RenderComponent<AuthenticationSettings>();
    var btn = cut.Find("button:contains('Create Password')");
    Assert.NotNull(btn);
}
```

**When to use:**
- Testing component logic without browser
- Fast feedback during development
- CI/CD pipeline (runs in milliseconds)

### Layer 2: Render Tests (bUnit)
**Purpose**: Validate HTML structure, CSS classes, element attributes

**Speed**: ~10-50ms per test

**What to test:**
- Correct HTML elements present
- CSS classes applied correctly
- Element attributes (disabled, aria-labels)
- Conditional rendering of sections

**Example:**
```csharp
[Fact]
public void PasswordDialog_WhenShown_HasCorrectStructure()
{
    var cut = RenderComponent<AuthenticationSettings>();
    // Trigger dialog
    cut.Find("button:contains('Create Password')").Click();

    // Verify structure
    var dialog = cut.Find(".bg-gray-700");
    Assert.Contains("Create New Password", dialog.InnerHtml);
    Assert.Equal(2, dialog.QuerySelectorAll("input[type='password']").Length);
}
```

**When to use:**
- Verifying component HTML structure
- Testing CSS class application
- Validating accessibility attributes

### Layer 3: Layout Tests (Playwright)
**Purpose**: Verify actual browser rendering, element positions, visibility

**Speed**: ~1-3 seconds per test (includes browser startup)

**What to test:**
- Elements are actually visible (not just in DOM)
- Layout positioning is correct
- Elements have correct computed styles
- Responsive behavior
- Browser-specific rendering

**Example:**
```csharp
[Fact]
public async Task AuthSettings_PasswordOnly_ShowsCorrectLayout()
{
    var page = await _fixture.Browser.NewPageAsync();
    await page.GotoAsync($"{BaseUrl}/test?state=password-only&automated=true");

    // Verify actual visibility
    var createBtn = page.Locator("button:has-text('Create Password')");
    await createBtn.WaitForAsync(new() { State = WaitForSelectorState.Hidden });

    var removeBtn = page.Locator("button:has-text('Remove Password')");
    await removeBtn.WaitForAsync(new() { State = WaitForSelectorState.Visible });

    await page.CloseAsync();
}
```

**When to use:**
- Verifying elements are actually visible to users
- Testing visual layout and positioning
- Validating browser-specific behavior
- Capturing LayoutML data (positions, sizes)

### Layer 4: Interaction Tests (Playwright)
**Purpose**: Validate complete user workflows end-to-end

**Speed**: ~3-5 seconds per test (includes browser startup + interactions)

**What to test:**
- Multi-step user workflows
- State transitions
- Form submissions
- Error handling
- Success/failure messages
- Complex interactions (click → fill → submit → verify)

**Example:**
```csharp
[Fact]
public async Task CreatePassword_ThenDeletePasskey_WorksCorrectly()
{
    var page = await _fixture.Browser.NewPageAsync();
    await page.GotoAsync($"{BaseUrl}/test?state=single-passkey&automated=true");

    // Step 1: Create password
    await page.Locator("button:has-text('Create Password')").ClickAsync();
    await page.GetByRole(AriaRole.Button, new() { Name = "Create", Exact = true }).ClickAsync();

    // Step 2: Verify password created
    var removeBtn = page.Locator("button:has-text('Remove Password')");
    await removeBtn.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 500 });

    // Step 3: Delete passkey
    await page.Locator("button:has-text('Delete')").First.ClickAsync();

    // Step 4: Verify final state
    var noPasskeysMsg = page.Locator("text=No passkeys registered yet");
    await noPasskeysMsg.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 200 });

    await page.CloseAsync();
}
```

**When to use:**
- Testing complete user workflows
- Verifying state transitions
- Reproducing user-reported bugs
- End-to-end validation

## Implementation Guide

### Step 1: Create Test Harness Page
```razor
@page "/test"
@using AutoWeb.Components
@using AutoWeb.Pages
@inject NavigationManager Navigation

<PageTitle>Component Test Harness</PageTitle>

@if (AutomatedTest)
{
    <!-- Automated testing: render ONLY the component -->
    @if (ComponentName == "AuthenticationSettings")
    {
        <AuthenticationSettings />
    }
    else if (ComponentName == "Auth")
    {
        <Auth />
    }
    else
    {
        <div class="text-red-500">Unknown component: @ComponentName</div>
    }
}
else
{
    <!-- Manual testing: render with state selector UI -->
    <div class="min-h-screen bg-gray-900 text-gray-100 p-4">
        <h1>@ComponentName Test Harness</h1>
        <div class="mb-4">
            @foreach (var state in States)
            {
                <button @onclick="() => ChangeState(state)">@state</button>
            }
        </div>
        <div class="border p-4">
            @* Render component based on ComponentName *@
        </div>
    </div>
}

@code {
    private string ComponentName = "AuthenticationSettings";
    private string CurrentState = "";
    private bool AutomatedTest = false;
    private string[] States = Array.Empty<string>();

    protected override void OnInitialized()
    {
        var uri = new Uri(Navigation.Uri);
        var query = HttpUtility.ParseQueryString(uri.Query);

        // Get component name, default to AuthenticationSettings for backward compatibility
        ComponentName = query["component"] ?? "AuthenticationSettings";

        // Get valid states for this component from central registry
        States = AutoWeb.Tests.MockStates.GetStatesForComponent(ComponentName);

        // Get current state, default to first state
        CurrentState = query["state"] ?? (States.Length > 0 ? States[0] : "");
        AutomatedTest = query["automated"] == "true";
    }

    private void ChangeState(string state)
    {
        Navigation.NavigateTo($"/test?component={ComponentName}&state={state}", forceLoad: true);
    }
}
```

**Key Features:**
- **Component parameter**: `?component=` selects which component to test
- **Backward compatible**: Defaults to AuthenticationSettings
- **Dynamic state loading**: Reads valid states from MockStates registry
- **Automated mode**: `?automated=true` hides test harness UI

### Step 2: Implement Mock Services

**Pattern:**
```csharp
public class MockAutoHostClient : IAutoHostClient
{
    private readonly NavigationManager _nav;
    private bool _hasPassword;
    private List<PasskeyInfo> _passkeys;

    public MockAutoHostClient(NavigationManager nav)
    {
        _nav = nav;
        InitializeFromState();
    }

    private void InitializeFromState()
    {
        var uri = new Uri(_nav.Uri);
        var query = HttpUtility.ParseQueryString(uri.Query);
        var state = query["state"] ?? "password-only";

        switch (state)
        {
            case "password-only":
                _hasPassword = true;
                _passkeys = new List<PasskeyInfo>();
                break;
            case "single-passkey":
                _hasPassword = false;
                _passkeys = new List<PasskeyInfo> { /* ... */ };
                break;
        }
    }

    public Task<AuthCheckResponse> AuthCheckUserAsync(string email)
    {
        return Task.FromResult(new AuthCheckResponse
        {
            HasPassword = _hasPassword,
            HasPasskeys = _passkeys.Any()
        });
    }

    public Task<PasswordOperationResponse> AuthCreatePasswordAsync(CreatePasswordRequest request)
    {
        _hasPassword = true;
        Console.WriteLine($"[MockAutoHostClient] Created password, hasPassword={_hasPassword}");
        return Task.FromResult(new PasswordOperationResponse
        {
            Success = true,
            Message = "Password created successfully"
        });
    }
}
```

**Key principles:**
- Read state from NavigationManager
- Initialize internal state based on query parameter
- Update internal state when operations occur
- Log all state changes for debugging

### Step 3: Register Mocks Conditionally

```csharp
// Program.cs
var enableMocks = Environment.GetEnvironmentVariable("ENABLE_MOCKS") == "true" ||
                  builder.HostEnvironment.IsDevelopment();

if (enableMocks)
{
    builder.Services.AddScoped<IAutoHostClient>(sp =>
    {
        var nav = sp.GetRequiredService<NavigationManager>();
        return new MockAutoHostClient(nav);
    });
}
else
{
    builder.Services.AddScoped<IAutoHostClient, AutoHostClient>();
}
```

### Step 4: Create Playwright Fixture

```csharp
public class PlaywrightFixture : IAsyncLifetime
{
    public IBrowser Browser { get; private set; } = null!;
    public const string BaseUrl = "http://localhost:6200";
    private IPlaywright _playwright = null!;

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync(new()
        {
            Headless = true
        });
    }

    public async Task DisposeAsync()
    {
        await Browser.CloseAsync();
        _playwright.Dispose();
    }
}
```

### Step 5: Write Tests

**Unit Test Example:**
```csharp
public class AuthenticationSettingsTests : TestContext
{
    [Fact]
    public void PasswordOnly_ShowsRemoveButton()
    {
        // Setup mock
        Services.AddSingleton<IAutoHostClient>(new MockAutoHostClient(/* password-only state */));

        var cut = RenderComponent<AuthenticationSettings>();

        var btn = cut.Find("button:contains('Remove Password')");
        Assert.NotNull(btn);
    }
}
```

**Layout Test Example:**
```csharp
public class AuthenticationSettingsLayoutTests : IClassFixture<PlaywrightFixture>
{
    [Fact]
    public async Task PasswordOnly_ElementsVisible()
    {
        var page = await _fixture.Browser.NewPageAsync();
        await page.GotoAsync($"{BaseUrl}/test?state=password-only&automated=true");

        var removeBtn = page.Locator("button:has-text('Remove Password')");
        await removeBtn.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        await page.CloseAsync();
    }
}
```

**Interaction Test Example:**
```csharp
public class AuthenticationSettingsInteractionTests : IClassFixture<PlaywrightFixture>
{
    [Fact]
    public async Task CreatePassword_ShowsSuccessMessage()
    {
        var page = await _fixture.Browser.NewPageAsync();
        await page.GotoAsync($"{BaseUrl}/test?state=single-passkey&automated=true");

        // Click Create Password
        await page.Locator("button:has-text('Create Password')").ClickAsync();

        // Fill form (use PressSequentiallyAsync for Blazor!)
        await page.Locator("input[type='password']").First.PressSequentiallyAsync("Test123!");
        await page.Locator("input[type='password']").Nth(1).PressSequentiallyAsync("Test123!");

        // Submit
        await page.GetByRole(AriaRole.Button, new() { Name = "Create", Exact = true }).ClickAsync();

        // Verify
        var successMsg = page.Locator("text=Password created successfully");
        await successMsg.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 200 });

        await page.CloseAsync();
    }
}
```

## Best Practices

### CRITICAL: IJSRuntime Registration

**DO NOT** register IJSRuntime in the DI container in Program.cs:

**BAD:**
```csharp
// Program.cs
if (enableMocks)
{
    builder.Services.AddScoped<Microsoft.JSInterop.IJSRuntime, MockJSRuntime>(); // ❌ CAUSES LIFETIME CONFLICT
}
```

**Error you'll see:**
```
ManagedError: Cannot consume scoped service 'Microsoft.JSInterop.IJSRuntime'
from singleton 'Microsoft.AspNetCore.Components.ResourceCollectionProvider'
```

**GOOD:**
```csharp
// Tests provide MockJSRuntime directly
public class AuthenticationSettingsTests : TestContext
{
    [Fact]
    public void Test()
    {
        Services.AddSingleton<IJSRuntime>(new MockJSRuntime()); // ✅ Test provides it
        var cut = RenderComponent<AuthenticationSettings>();
    }
}
```

**Why:** Blazor framework registers IJSRuntime internally with specific lifetime requirements. Registering a mock globally causes service lifetime conflicts. Instead, tests should provide the mock implementation directly when setting up the test context.

**Exception:** PasskeyService CAN be registered with MockJSRuntime dependency because PasskeyService is scoped, not singleton:
```csharp
// Program.cs - This is OK
if (enableMocks)
{
    builder.Services.AddScoped<PasskeyService>(sp =>
    {
        var jsRuntime = sp.GetRequiredService<IJSRuntime>(); // Gets Blazor's JSRuntime
        // ... use jsRuntime for PasskeyService
    });
}
```

### Timeouts
**BAD:**
```csharp
await element.WaitForAsync(new() { Timeout = 5000 }); // Why 5 seconds?!
await page.WaitForTimeoutAsync(1000); // Arbitrary wait
```

**GOOD:**
```csharp
await element.WaitForAsync(new() { Timeout = 200 }); // Local Blazor runs in milliseconds
// Only wait for specific state changes, not arbitrary time
```

**Rule**: Local Blazor with mocks runs in single-digit milliseconds. Use 100-200ms timeouts, not seconds.

### Playwright + Blazor Gotchas

**BAD:**
```csharp
await input.FillAsync("password"); // Doesn't trigger @bind:event="oninput"!
```

**GOOD:**
```csharp
await input.PressSequentiallyAsync("password"); // Types character by character
```

**Rule**: Use `PressSequentiallyAsync()` for Blazor inputs with `@bind:event="oninput"`, not `FillAsync()`.

### Triggering Input Events (bUnit)

**CRITICAL: bUnit input event handling for Blazor `@bind:event="oninput"`**

**BAD:**
```csharp
var input = cut.Find("input[type='email']");
input.Change("test@example.com");  // ❌ Triggers onchange, not oninput!
input.Input("test@example.com");   // ❌ Still triggers onchange internally!
```

**GOOD:**
```csharp
var input = cut.Find("input[type='email']");
await input.InputAsync(new ChangeEventArgs { Value = "test@example.com" });  // ✅ Triggers oninput
```

**Why**: Blazor inputs with `@bind:event="oninput"` bind to the `oninput` event, not `onchange`. bUnit's `.Change()` and `.Input()` methods trigger `onchange` by default. You must use `.InputAsync(new ChangeEventArgs { Value = ... })` to trigger `oninput` correctly.

**Rule**: For Blazor inputs with `@bind:event="oninput"`, always use `InputAsync(new ChangeEventArgs { Value = "..." })`.

### Button Selection

**BAD:**
```csharp
var btn = page.Locator("button:has-text('Create')"); // Matches "Create Password" too!
```

**GOOD:**
```csharp
var btn = page.GetByRole(AriaRole.Button, new() { Name = "Create", Exact = true });
```

**Rule**: Use exact text matching or role-based selectors to avoid ambiguity.

### Test Output

**BAD:**
```csharp
// Run test multiple times to grep for different output
dotnet test > output.txt
grep "error" output.txt
dotnet test > output2.txt  // Running again!
grep "success" output2.txt
```

**GOOD:**
```csharp
// Log everything once, grep multiple times
dotnet test > output.txt 2>&1
grep "error" output.txt
grep "success" output.txt
grep "warning" output.txt
```

**Rule**: Log comprehensively upfront, save to file, grep for different pieces. Don't re-run tests just to see different output.

### Debugging

**CRITICAL: Look at screenshots FIRST when tests fail!**

The test logs show what you THINK happened. The screenshot shows what ACTUALLY happened.

**BAD debugging order:**
1. Test fails
2. Read logs
3. Add more logging
4. Run test again
5. Modify code
6. Run test again
7. Finally look at screenshot

**GOOD debugging order:**
1. Test fails
2. **IMMEDIATELY** open screenshot (`/tmp/test-failure.png`)
3. Compare screenshot to expected state
4. Now you know what's actually wrong
5. Look at logs to understand why

### Screenshot Naming
```csharp
await page.ScreenshotAsync(new() { Path = "/tmp/test-failure.png" });
```

Use descriptive names. Include test name or scenario.

## Directory Structure
```
AutoWeb/
├── Pages/
│   └── Auth.razor                     # Auth component (page-level)
├── Components/
│   └── AuthenticationSettings.razor   # AuthenticationSettings component
├── Tests/
│   ├── TestPage.razor                 # Component-agnostic test harness
│   ├── MockServices.cs                # MockAutoHostClient, MockStates registry
│   ├── MockJSRuntime.cs               # Mock JSRuntime (sessionStorage, PasskeySupport)
│   └── PlaywrightCollection.cs        # xUnit collection definition

AutoWeb.Tests/
├── Components/
│   ├── AuthenticationSettings/
│   │   ├── AuthenticationSettingsTests.cs           # Unit tests (bUnit)
│   │   ├── AuthenticationSettingsRenderTests.cs     # Render tests (bUnit)
│   │   ├── AuthenticationSettingsLayoutTests.cs     # Layout tests (Playwright)
│   │   └── AuthenticationSettingsInteractionTests.cs # Interaction tests (Playwright)
│   └── Auth/
│       └── (Future: Auth tests following same pattern)
├── PlaywrightFixture.cs               # Shared Playwright setup (browser lifecycle)
└── AutoWeb.Tests.csproj               # Includes Playwright, bUnit, xUnit

claude/
├── UI.md                              # This document
├── ui/
│   ├── COMPONENT_TESTING.md           # Component testing methodology
│   ├── METHOD.md                      # UI testing approach
│   ├── AuthenticationSettings/
│   │   ├── AuthenticationSettings.md      # Component specification
│   │   └── *.png                          # Screenshots for documentation
│   ├── Auth.razor/
│   │   ├── Auth.razor.md                  # Auth page specification
│   │   ├── Auth.razor.test.md             # Auth test expectations
│   │   └── Auth.razor.json                # Legacy screenshot definitions
│   └── capture-screenshots.py         # Legacy screenshot capture (optional)
└── tasks/
    ├── Auth-Testing-Modernization.md  # Auth testing modernization plan
    └── Auth-Testing-WTB.md            # Auth testing work task breakdown
```

## Component Testing Checklist

When testing a new component, follow this checklist:

### 1. Define Valid States
- [ ] List all valid real-world states
- [ ] Confirm each state is actually possible
- [ ] Add states to `MockStates.ComponentStates` dictionary
- [ ] Document state transitions

### 2. Create Mock System
- [ ] Implement stateful mocks for all services
- [ ] Add state-based initialization
- [ ] Add console logging for debugging
- [ ] Register mocks conditionally in Program.cs

### 3. Create Test Harness
- [ ] Add TestPage.razor (if not exists)
- [ ] Support state query parameter
- [ ] Support automated mode
- [ ] Test harness renders component correctly

### 4. Write Unit Tests (bUnit)
- [ ] Test initialization
- [ ] Test state changes
- [ ] Test event handlers
- [ ] Test conditional rendering
- Target: 10+ tests, ~1 second total runtime

### 5. Write Render Tests (bUnit)
- [ ] Test HTML structure
- [ ] Test CSS classes
- [ ] Test element attributes
- [ ] Test accessibility attributes
- Target: 5+ tests, ~500ms total runtime

### 6. Write Layout Tests (Playwright)
- [ ] Test each valid state renders correctly
- [ ] Test element visibility
- [ ] Test element positioning
- [ ] Capture LayoutML data
- Target: 5+ tests, ~10 seconds total runtime

### 7. Write Interaction Tests (Playwright)
- [ ] Test key user workflows
- [ ] Test state transitions
- [ ] Test error handling
- [ ] Test success messages
- Target: 3-5 tests, ~15 seconds total runtime

### 8. Document Component
- [ ] Create `claude/ui/{ComponentName}/{ComponentName}.md`
- [ ] Document functional requirements
- [ ] Document test infrastructure
- [ ] Document known issues
- [ ] Include screenshots

## Performance Targets

### Per-Test Performance
- **Unit test**: 10-50ms
- **Render test**: 10-50ms
- **Layout test**: 1-3 seconds
- **Interaction test**: 3-5 seconds

### Total Suite Performance
For a component with comprehensive coverage:
- **Unit tests** (10 tests): ~500ms
- **Render tests** (5 tests): ~250ms
- **Layout tests** (5 tests): ~10 seconds
- **Interaction tests** (3 tests): ~12 seconds
- **Total**: ~23 seconds for complete component validation

## Benefits of This System

### Speed
- **Immediate feedback**: Unit/render tests run in milliseconds
- **Fast iteration**: Change code, run tests, see results in <1 second
- **Parallel execution**: Layout/interaction tests can run in parallel

### Isolation
- **No backend required**: Mocks provide all data
- **No authentication**: Direct state access via URL
- **No navigation**: Jump directly to any state
- **No database**: Everything in memory

### Coverage
- **Four layers**: Unit, render, layout, interaction
- **All states**: Every valid state tested
- **All workflows**: Every user journey validated
- **All edge cases**: Error states, empty states, loading states

### Debugging
- **Console logging**: Mocks log all operations
- **Screenshots**: Visual proof of what actually happened
- **State inspection**: Browser DevTools available
- **Fast reproduction**: Jump directly to failing state

### Maintainability
- **Clear separation**: Each test layer has specific purpose
- **Self-documenting**: Tests serve as specification
- **Easy updates**: Change mock data, re-run tests
- **No flakiness**: Deterministic, no backend dependencies

## System Architecture & Lifecycle

### How capture-screenshots.py Works
The capture system has a specific lifecycle that MUST be understood:

1. **Startup Phase**
   - Kills any existing processes on ports 6050 (AutoHost) and 6100 (AutoWeb)
   - Removes existing test database for fresh start
   - Builds latest CSS with TailwindCSS
   - Starts AutoHost on port 6050 with isolated test database
   - Starts AutoWeb on port 6100
   - Waits for both servers to be ready

2. **Capture Phase**
   - For each screenshot definition in the JSON files:
     - Generates a Playwright script in `/tmp/{name}.js`
     - Executes the script with `node`
     - Captures screenshot and LayoutML data
     - Saves to `results/` directory

3. **Cleanup Phase**
   - Kills both servers
   - Servers are NO LONGER RUNNING after this point

### CRITICAL: Understanding Script Dependencies

**The `/tmp/*.js` scripts are NOT standalone!**
- They REQUIRE AutoHost running on port 6050
- They REQUIRE AutoWeb running on port 6100
- They are raw Playwright automation scripts, NOT test files
- They have no test() blocks, no assertions, no test runner
- They ONLY work during the capture-screenshots.py execution

**NEVER attempt to:**
- Run these scripts with `node` after capture-screenshots.py completes
- Run these scripts with `npx playwright test` (they're not test files!)
- Debug these scripts in isolation without the servers running

**To debug capture issues:**
- Add console.log statements to the generated scripts
- Run the full capture-screenshots.py and observe output
- Use the `--page` flag to run just one page's captures
- Check the actual error messages during capture execution

The scripts are meaningless without the full system context - like trying to test a car's transmission by removing it from the car.

## Evolution from Screenshot Testing

This system evolved from the original `capture-screenshots.py` approach:

**Old approach (capture-screenshots.py):**
- Start full backend server
- Start frontend server
- Navigate through UI to reach state
- Capture screenshot
- Repeat for each state
- Total time: 30-60 seconds per component

**New approach (mock-based testing):**
- Use mocks, no backend needed
- Direct URL to any state
- Browser starts once per test class
- Tests run in parallel
- Total time: ~20 seconds for complete component

**When to use each:**
- **Mock-based tests**: Default for all component testing
- **capture-screenshots.py**: Documentation generation, visual regression testing

## One-Shot Test Implementation

**Goal**: Once the test harness and mocks are set up, implementing a new interaction test should take 5-10 minutes, not 1 hour.

**Keys to speed:**
1. **Clear state definition**: Know exactly what state you're testing
2. **Mock already configured**: State returns correct data
3. **TestPage already exists**: Jump directly to state
4. **Debugging methodology**: Screenshot first, then logs
5. **Fast timeouts**: 100-200ms, not seconds
6. **Exact selectors**: Use role-based or exact text matching

**Template for new test:**
```csharp
[Fact]
public async Task WorkflowName_DescribeExpectedBehavior()
{
    var page = await _fixture.Browser.NewPageAsync();
    try
    {
        // Navigate to state
        await page.GotoAsync($"{BaseUrl}/test?state=STATE_NAME&automated=true");

        // Perform actions
        await page.Locator("selector").ClickAsync();

        // Verify outcome
        var element = page.Locator("expected-selector");
        await element.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 200 });

        // Assert final state
        Assert.True(await element.IsVisibleAsync());
    }
    catch (Exception ex)
    {
        await page.ScreenshotAsync(new() { Path = $"/tmp/{nameof(WorkflowName)}-failure.png" });
        throw;
    }
    finally
    {
        await page.CloseAsync();
    }
}
```

**Time breakdown:**
- Write test structure: 2 minutes
- Add actions/verifications: 3 minutes
- Run test: 5 seconds
- Fix selector if wrong: 2 minutes
- **Total**: ~7 minutes

Compare to 1 hour when:
- Fighting with wrong button selector: 20 minutes
- Using FillAsync instead of PressSequentiallyAsync: 15 minutes
- Debugging with 5-second timeouts: 10 minutes
- Not looking at screenshot: 15 minutes
