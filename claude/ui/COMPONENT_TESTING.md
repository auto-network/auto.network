# Component-Focused Layout Testing

## Purpose
Fast, isolated layout testing for individual Blazor components without requiring full application navigation or backend services.

## Problem with capture-screenshots.py
The existing `capture-screenshots.py` approach has critical limitations:
- **Too Slow**: Requires starting full AutoHost and AutoWeb servers
- **Complex State Setup**: Must navigate through entire application flows to reach component states
- **Brittle**: Depends on full application working end-to-end
- **Inefficient**: Tests entire pages when we only care about individual components

## New Approach: Direct Component Testing

### Core Concept
1. **Mock Services**: Use `MockAutoHostClient` and `MockPasskeyService` (in `/AutoWeb/Tests/MockServices.cs`)
2. **Test Routes**: `/test` route automatically uses mock services (configured in `Program.cs`)
3. **Query String State Control**: Mock services read `?state=XXX` from URL to return different data
4. **Direct Component Testing**: Navigate directly to `/test?state=XXX` to render component in specific state

### Available Mock States
Defined in `MockAutoHostClient.GetState()`:
- `no-auth` - No password, no passkeys
- `password-only` - Has password, no passkeys
- `single-passkey` - No password, one passkey
- `multiple-passkeys` - No password, multiple passkeys
- `password-and-passkeys` - Has both password and passkeys

### Architecture

```
AutoWeb/
├── Tests/
│   ├── MockServices.cs          # Mock implementations
│   └── TestPage.razor            # Test harness page at /test (for manual inspection)
├── Program.cs                    # Routes /test* to mock services
└── Components/
    └── AuthenticationSettings.razor  # Component under test

AutoWeb.Tests/
├── Components/
│   ├── AuthenticationSettingsTests.cs        # Unit tests (58 tests)
│   └── AuthenticationSettingsLayoutTests.cs  # Layout tests (this approach)
└── baseline/
    └── AuthenticationSettings/
        ├── no-auth.html          # Baseline HTML for each state
        ├── password-only.html
        ├── single-passkey.html
        ├── multiple-passkeys.html
        └── password-and-passkeys.html
```

### Test Configuration

States are defined directly in the C# test class using Theory data:

```csharp
public static IEnumerable<object[]> LayoutStates()
{
    yield return new object[] { "no-auth", "No password, no passkeys" };
    yield return new object[] { "password-only", "Password but no passkeys" };
    yield return new object[] { "single-passkey", "Single passkey only" };
    yield return new object[] { "multiple-passkeys", "Multiple passkeys" };
    yield return new object[] { "password-and-passkeys", "Both password and passkeys" };
}

[Theory]
[MemberData(nameof(LayoutStates))]
public async Task Layout_MatchesBaseline(string stateName, string description)
{
    // Setup mock for this state
    SetupMockForState(stateName);

    // Render component
    var component = RenderComponent<AuthenticationSettings>();
    await Task.Delay(50);

    // Compare with baseline
    var actual = NormalizeHtml(component.Markup);
    var baseline = LoadBaseline(stateName);

    Assert.Equal(baseline, actual);
}
```

### Workflow

#### 1. Initial Setup (One Time)
```bash
# Ensure TestPage.razor exists with component mounted
# Ensure MockServices.cs has all required states implemented
# Ensure Program.cs routes /test to mock services
```

#### 2. Capture Baseline Layouts
```bash
cd /home/jeremy/auto/AutoWeb.Tests
dotnet test --filter "FullyQualifiedName~AuthenticationSettingsLayoutTests.CaptureBaseline"
```

This test:
1. Uses bUnit to render component with mocked services
2. For each state:
   - Sets up mock to return specific state data
   - Renders component
   - Captures normalized HTML
   - Saves to `baseline/AuthenticationSettings/{state}.html`
3. **Completes in under 1 second (no browser, no servers!)**

#### 3. Run Layout Tests
```bash
cd /home/jeremy/auto/AutoWeb.Tests
dotnet test --filter "FullyQualifiedName~AuthenticationSettingsLayoutTests"
```

Or run all tests:
```bash
dotnet test  # Runs all bUnit tests including layout validation
```

### Benefits

**Speed**
- No AutoHost startup
- No AutoWeb startup
- No browser startup
- No Playwright overhead
- Pure in-memory bUnit rendering
- **All 5 states tested in under 1 second**

**Reliability**
- No auth dependencies
- No database setup
- No multi-step flows
- Single point of failure (component itself)

**Maintainability**
- State definition in one place (MockServices.cs)
- Easy to add new states (just add to switch statement)
- Clear separation: unit tests (bUnit) vs layout tests (Playwright)

**Developer Experience**
- Test component in isolation during development
- Visual harness at `/test` for manual testing
- Fast feedback loop

### Implementation Details

#### MockAutoHostClient State Management
```csharp
private string GetState()
{
    var uri = new Uri(_nav.Uri);
    var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
    return query["state"] ?? "no-auth";
}

public Task<PasskeyListResponse> PasskeyListAsync()
{
    var state = GetState();
    var passkeys = state switch
    {
        "single-passkey" => new List<PasskeyInfo> { /* mock data */ },
        "multiple-passkeys" => new List<PasskeyInfo> { /* mock data */ },
        _ => new List<PasskeyInfo>()
    };
    return Task.FromResult(new PasskeyListResponse { Passkeys = passkeys });
}
```

#### Program.cs Service Registration
```csharp
builder.Services.AddScoped<IAutoHostClient>(sp =>
{
    var nav = sp.GetRequiredService<NavigationManager>();
    if (nav.Uri.Contains("/test"))
    {
        return new MockAutoHostClient(nav);
    }
    // Regular client for production
    return new AutoHostClient(httpClient);
});
```

#### TestPage.razor Structure
```razor
@page "/test"
@using AutoWeb.Components

<div class="test-harness">
    <div class="state-selector">
        @foreach (var state in States)
        {
            <button @onclick="() => ChangeState(state)">@state</button>
        }
    </div>

    <div class="component-container">
        <AuthenticationSettings />
    </div>
</div>

@code {
    private string[] States = { "no-auth", "password-only", "single-passkey", ... };

    private void ChangeState(string state)
    {
        Navigation.NavigateTo($"/test?state={state}", forceLoad: true);
    }
}
```

### Layout Capture Format

**Normalized HTML** (using bUnit's Markup):
- Component rendered HTML
- Whitespace normalized
- Dynamic attributes removed (Blazor internal IDs)
- Semantic structure preserved
- Easy to diff with standard text comparison
- Human-readable for debugging

```csharp
private string NormalizeHtml(string html)
{
    // Remove Blazor internal attributes
    html = Regex.Replace(html, @"\s*b-[a-z0-9]+=""[^""]*""", "");

    // Normalize whitespace
    html = Regex.Replace(html, @"\s+", " ");
    html = Regex.Replace(html, @">\s+<", "><");

    // Sort attributes for consistency
    // ...

    return html.Trim();
}
```

### Adding New Components

1. Create test harness in TestPage.razor (or new test page)
2. Add mock states to MockServices.cs if needed
3. Create `claude/ui/components/{ComponentName}/config.json`
4. Run capture script to generate baselines
5. Add to CI pipeline

### Adding New States to Existing Component

1. Add case to `MockAutoHostClient.GetState()` switch
2. Add state to component's `config.json`
3. Re-run baseline capture for new state only
4. Existing states remain unchanged

### Relationship to Other Testing Approaches

**Unit Tests** (`AuthenticationSettingsTests.cs` - 58 tests)
- Test individual behaviors and interactions
- Mock API calls and verify state changes
- Fast, focused on logic

**Layout Tests** (`AuthenticationSettingsLayoutTests.cs` - this approach)
- Test visual structure and element presence
- Verify correct UI for each state
- Baseline comparison for regression detection
- Fast, focused on rendering

**Integration Tests** (capture-screenshots.py - if still needed)
- Full user flows through real application
- E2E testing with real browsers
- Slow, run less frequently
- Used for critical user journeys only

## Key Principles

1. **Speed First**: Tests should run in seconds
2. **Isolation**: Components test independently
3. **Mock Everything**: No real services, no auth, no DB
4. **Query String Control**: State via URL parameters
5. **Baseline Comparison**: Capture once, compare many times
6. **Visual Harness**: `/test` page for manual inspection

## Success Criteria

- [ ] AuthenticationSettings component has layout tests for all 5 states
- [ ] Tests run in under 10 seconds total
- [ ] No AutoHost dependency
- [ ] Baseline captures committed to repo
- [ ] CI pipeline integrated
- [ ] Documentation complete
- [ ] Easy to add new components/states
