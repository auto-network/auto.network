# UI Testing Method

This document describes the structured approach for UI testing and validation in this project.

## ⚠️ IMPORTANT: Testing Approach Evolution

**This document describes the LEGACY capture-screenshots.py approach.**

**For MODERN component testing, see `/home/jeremy/auto/claude/UI.md`.**

### When to Use Each Approach

**Modern Mock-Based Testing** (UI.md) - **USE THIS FOR NEW TESTS**
- ✅ Fast: Tests run in milliseconds to seconds
- ✅ Isolated: No backend dependencies
- ✅ Comprehensive: 4 layers (unit, render, layout, interaction)
- ✅ CI-friendly: Runs in automated pipelines
- ✅ State control: Direct URL access to any state via `?state=` parameter
- **Use for**: All component testing, regression testing, TDD workflow

**Legacy Screenshot Testing** (this document) - **USE FOR DOCUMENTATION**
- ✅ Visual: Human-readable PNG screenshots
- ✅ LayoutML: Structured JSON for validation
- ❌ Slow: 30-60 seconds per page
- ❌ Full-stack: Requires AutoHost + AutoWeb servers
- **Use for**: Visual documentation, one-time layout validation

### Migration Status

- ✅ **AuthenticationSettings**: Fully migrated to modern testing (79 tests)
- 🔄 **Auth.razor**: Migration in progress (Phase 1 complete)
- 📋 **Other components**: Use modern approach from the start

---

## Overview (Legacy Approach)

Our UI testing pipeline combines visual screenshots with structured layout data (LayoutML) to enable automated validation of UI requirements. This approach solves the fundamental problem that AI assistants cannot reliably interpret PNG screenshots directly.

**Note**: The modern mock-based approach (UI.md) achieves the same goals with better performance and isolation.

## Directory Structure

Each page under test has its own dedicated folder:

```
claude/ui/
├── capture-screenshots.py      # Main capture and test runner
├── test_framework.py           # Base testing framework classes
├── METHOD.md                   # This documentation
└── {PageName}/                 # Folder per page (e.g., Auth.razor)
    ├── {PageName}.json         # Screenshot definitions and actions
    ├── {PageName}.md           # Page-specific documentation
    ├── {PageName}.test.py      # Layout validation tests
    └── results/                # Generated artifacts (cleared on each run)
        ├── {Name}.png          # Visual screenshots
        └── {Name}.json         # LayoutML structured data
```

## Workflow

### 1. Define Screenshots (`{PageName}.json`)

Each page has a JSON file defining what screenshots to capture and any actions to perform:

```json
{
  "baseUrl": "/auth",
  "screenshots": [
    {
      "name": "Auth.razor.Initial",
      "description": "Initial auth page state - empty email field",
      "actions": []
    },
    {
      "name": "Auth.razor.EmailEntered",
      "description": "Auth page with email address entered",
      "actions": [
        {
          "type": "fill",
          "selector": "input[type=\"email\"]",
          "value": "user@example.com"
        }
      ]
    },
    {
      "name": "Auth.razor.PasswordStep",
      "description": "Auth page showing password entry step",
      "actions": [
        {
          "type": "fill",
          "selector": "input[type=\"email\"]",
          "value": "user@example.com"
        },
        {
          "type": "click",
          "selector": "button[type=\"submit\"]"
        },
        {
          "type": "wait",
          "selector": "input[type=\"password\"]"
        }
      ]
    }
  ]
}
```

Action types:
- `fill`: Enter text into an input field
- `click`: Click an element
- `wait`: Wait for an element to appear

### 2. Capture Screenshots and LayoutML

Run the capture script to generate screenshots and structured layout data:

```bash
# Capture all pages
python claude/ui/capture-screenshots.py

# Capture specific page
python claude/ui/capture-screenshots.py --page Auth.razor

# Run tests only (no capture)
python claude/ui/capture-screenshots.py --test-only --page Auth.razor
```

The capture process:
1. **Cleans old results** - Removes previous `results/` folder to avoid stale data
2. **Starts dedicated servers** - AutoHost on port 6050, AutoWeb on port 6100
3. **Captures screenshots** - Executes defined actions and saves PNG files
4. **Extracts LayoutML** - Captures DOM structure with computed styles and visibility
5. **Runs validation tests** - Automatically executes page-specific tests

### 3. Write Layout Tests (`{PageName}.test.py`)

Each page has a test file that validates the captured LayoutML data:

```python
from test_framework import LayoutTest

class AuthInitialTest(LayoutTest):
    def run_tests(self):
        # Verify expected elements exist
        title = self.assert_element_exists('h1', text='Auto')
        self.assert_element_visible(title)

        # Verify button has correct text
        self.assert_button_text('Continue')

        # Verify no password fields visible
        self.assert_element_not_exists('input', type='password')

        # Check alignment
        email_label = self.find_element('label', text='Email')
        email_input = self.find_element('input', type='email')
        self.assert_element_alignment(email_label, email_input, "left")
```

## LayoutML Format

The LayoutML JSON files contain structured representations of the page layout:

```json
{
  "viewport": {
    "width": 1280,
    "height": 720
  },
  "elements": [
    {
      "tag": "button",
      "type": "submit",
      "text": "Continue",
      "value": "",
      "placeholder": "",
      "rect": {
        "x": 373,
        "y": 415,
        "width": 384,
        "height": 48
      },
      "style": {
        "display": "inline-block",
        "opacity": "1",
        "visibility": "visible",
        "position": "static",
        "zIndex": "auto"
      },
      "classes": "w-full mt-4 p-3 bg-green-600..."
    }
  ]
}
```

This structured format enables:
- Precise validation of element existence and properties
- Verification of text content and values
- Layout alignment checking
- Visibility state validation
- Style and class verification

## Test Framework API

The `test_framework.py` provides base classes with these key methods:

### Finding Elements
- `find_element(tag, text=None, type=None)` - Find first matching element
- `find_all_elements(tag, text=None, type=None)` - Find all matching elements

### Assertions
- `assert_element_exists(tag, text=None, type=None, message="")` - Element must exist
- `assert_element_not_exists(tag, text=None, type=None, message="")` - Element must not exist
- `assert_element_visible(element, message="")` - Element must be visible
- `assert_button_text(expected_text, message="")` - Verify button text
- `assert_element_alignment(elem1, elem2, alignment="left", tolerance=5)` - Check alignment

### Test Results
- `self.errors` - List of test failures
- `self.warnings` - List of non-critical issues
- `report()` - Print results and return pass/fail status

## Key Design Decisions

1. **Folder per page** - Keeps tests, definitions, and results organized and colocated
2. **LayoutML over OCR** - Structured data is more reliable than image recognition
3. **Clean before capture** - Prevents confusion from stale results
4. **Dedicated test ports** - Avoids conflicts with development servers
5. **Automatic test execution** - Tests run immediately after capture for fast feedback
6. **Separate test-only mode** - Enables rapid test iteration without regeneration

## Benefits

1. **AI-Friendly** - Structured JSON data that AI assistants can reliably parse
2. **Deterministic** - Tests produce consistent results
3. **Fast Feedback** - Immediate validation after screenshots
4. **Version Control** - Text-based test definitions and results
5. **Debugging** - Clear error messages with specific element details
6. **Maintainable** - Tests live alongside their page definitions

## Example Test Output

```
Testing Auth.razor:
  AuthInitialTest: 1 warning(s)
  WARNING: Title appears off-center (center at 565.0px, viewport center at 640.0px)
✓ AuthEmailEnteredTest: All tests passed
✓ AuthPasswordStepTest: All tests passed
✓ AuthInvalidEmailTest: All tests passed

✓ All 4 Auth.razor layout tests passed!
```

## Writing Effective Layout Tests

### Test Organization
Each test class should focus on a specific UI state and validate:
1. **Element existence** - Critical elements are present
2. **Element visibility** - Elements are actually visible (not just in DOM)
3. **Content validation** - Text, values, and placeholders are correct
4. **Positioning** - Elements are properly aligned and positioned
5. **Styling** - Important classes for state indication
6. **State transitions** - Elements move to expected positions
7. **Error conditions** - Proper error display and handling

### Common Test Patterns

#### Testing Element Positioning
```python
# Check absolute positioning
if element.rect['x'] > boundary:
    self.errors.append(f"Element outside bounds")

# Check relative alignment
self.assert_element_alignment(elem1, elem2, "left", tolerance=5)

# Check centering
center = elem.rect['x'] + elem.rect['width'] / 2
if abs(center - viewport_center) > tolerance:
    self.errors.append("Not centered")
```

#### Testing Visibility States
```python
# Check if truly visible
if element.rect['x'] < 0:  # Off-screen left
    self.errors.append("Element should be visible")

# Check style-based visibility
if 'opacity-0' in element.classes:
    self.errors.append("Element is invisible")
```

#### Testing Form States
```python
# Button state based on input
if valid_input and 'disabled' in button.classes:
    self.errors.append("Button should be enabled")

# Error message presence
if invalid_input and not error_element:
    self.errors.append("Error message missing")
```

### Best Practices

1. **Use errors vs warnings appropriately**
   - `self.errors` - Critical failures that break functionality
   - `self.warnings` - Style issues or minor problems

2. **Test both positive and negative cases**
   - Verify expected elements exist
   - Verify unexpected elements don't exist

3. **Check complete state**
   - Don't just test the changed element
   - Verify the entire page state is correct

4. **Use meaningful messages**
   ```python
   self.errors.append(f"Status at x={actual}, expected x<={boundary}")
   # Better than: self.errors.append("Wrong position")
   ```

5. **Test interactions between elements**
   - Alignment between related elements
   - Spacing and layout relationships
   - Z-index and layering

6. **Consider responsive behavior**
   - Elements should adapt to viewport
   - Test at standard viewport size (1280x720)

## Real-World Example: Auth.razor Status Bug

The Auth.razor tests successfully caught a layout bug where the status indicator was floating to the right of the form instead of being positioned below it:

```python
# This test caught the bug:
if status_left > form_right:
    self.errors.append(f"Status indicator is floating to the right!
                       Status x={status_left}, Form ends at x={form_right}")
```

Result: `ERROR: Status indicator is floating to the right! Status x=789, Form ends at x=757`

This demonstrates how LayoutML enables precise position validation that would be impossible with visual screenshot comparison alone.

## Adding Tests for a New Page

1. Create folder: `claude/ui/{PageName}/`
2. Define screenshots: `{PageName}.json`
3. Create test documentation: `{PageName}.test.md`
4. Write tests: `{PageName}.test.py`
5. Run capture: `python capture-screenshots.py --page {PageName}`
6. Iterate: `python capture-screenshots.py --test-only --page {PageName}`
7. Track coverage in test documentation

This method provides a robust, maintainable approach to UI testing that works effectively with both human developers and AI assistants.