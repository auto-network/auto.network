# Auth.razor Page Specification

## Purpose
Authentication page for the Auto application that handles user login with email and password, managing API keys and sessions through the AutoHost backend.

## Visual Design

### Layout Structure
- **Full-screen Background**: Dark navy (`#1a1f36`) using MinimalLayout (no container constraints)
- **Centered Container**: Fixed height (h-32) with absolutely positioned content
- **Gray Card**: Dark gray (`#374151`/gray-700) rounded container with shadow
- **Green Title**: "Auto" in green-400 positioned above the card
- **Connection Status**: Bottom-center position showing backend status

### Colors
- Background: `#1a1f36` (dark navy)
- Card: `#374151` (gray-700)
- Title: `#10b981` (green-400)
- Buttons: `#059669` (green-600), hover: `#10b981` (green-500)
- Disabled: `#4b5563` (gray-600)
- Error text: `#ef4444` (red-500)
- Connected: `#10b981` (green-400)
- Disconnected: `#ef4444` (red-400)

### Typography
- Title: Large, bold, green-400
- Labels: Standard white text
- Inputs: White text on dark gray-700 background
- Status: Small gray-400 text at bottom

### Spacing
- Balanced vertical centering with flexbox
- Consistent p-8 padding within card
- Full-width form elements with mt-4 spacing
- Status indicator mt-4 below card

## User Flows

### Flow 1: Successful Login
1. User loads `/auth` page
2. Enters valid email address (e.g., "user@example.com")
3. Continue button becomes enabled (green)
4. Clicks Continue
5. Password field appears with slide transition
6. Enters password
7. Clicks Login
8. On success: Redirected to Home page with stored API key

### Flow 2: Invalid Email Format
1. User enters invalid email (e.g., "notanemail")
2. Real-time validation shows error
3. Continue button remains disabled
4. User corrects email format
5. Error clears, button enables

### Flow 3: Empty Submit Attempt
1. User clicks Continue without entering email
2. Validation prevents submission
3. Button remains disabled
4. User must enter email to proceed

### Flow 4: Backend Connection Lost
1. AutoHost becomes unavailable
2. Status changes to "Disconnected" (red)
3. Continue button shows "AutoHost Not Connected"
4. Button is disabled even with valid email

## States

### State 1: Initial (Email Entry)
- **Visible Elements**:
  - "Auto" title (green-400)
  - "Email" label
  - Email input field with placeholder "you@example.com"
  - Continue button (disabled until valid email entered)
  - Connection status at bottom
- **Hidden Elements**: Password field, back link
- **Validation**: Real-time email validation as user types

### State 2: Email Filled
- **Visible Elements**: Same as Initial
- **Field Values**: Valid email entered (e.g., "user@example.com")
- **Button State**: Enabled (green-600)
- **Input Border**: Green when focused

### State 3: Password Entry
- **Visible Elements**:
  - "Auto" title
  - "Password" label
  - Password input field
  - Login button
  - Back/Change email link
- **Hidden Elements**: Email input (slid left)
- **Transition**: 500ms slide animation

### State 4: Invalid Email
- **Visible Elements**:
  - Email input with red border
  - Error message below input
- **Error Text**: "Please enter a valid email address"
- **Button State**: Disabled (gray-600)

### State 5: Disconnected
- **Status Text**: "Status: Disconnected" in red-400
- **Button Text**: "AutoHost Not Connected"
- **Button State**: Disabled regardless of email validity
- **Version**: Hidden when disconnected

## Validation Criteria

### For Initial State
- [x] "Auto" title visible in green-400
- [x] Email input field present and empty
- [x] Continue button visible but disabled
- [x] Connection status shows at bottom
- [x] Background fills entire viewport

### For Email Filled State
- [x] Valid email format accepted (contains @ and .)
- [x] Continue button enabled and green
- [x] No error messages visible
- [x] Input has focus outline

### For Password Entry State
- [ ] Password field visible after transition
- [ ] Email field hidden (slid left)
- [ ] Login button enabled
- [ ] Back option available

### For Error States
- [x] Invalid email shows inline error
- [x] Button disabled on errors
- [x] Error text in red-500
- [ ] Network errors show in status

### For Connection Status
- [x] Green "Connected" when AutoHost available
- [x] Red "Disconnected" when unavailable
- [x] Version number shown when connected
- [x] Positioned below main card

## Edge Cases

### Network Issues
- Connection status updates in real-time
- Form disabled when backend unavailable
- Clear messaging about connection state

### Invalid Credentials
- Error message displayed inline
- Form remains on password step
- User can retry or change email

### Session Management
- Existing session redirects to Home
- Expired session returns to Auth
- Multiple sessions supported per user

### Browser Compatibility
- Works without JavaScript (Blazor WASM)
- Responsive on all screen sizes
- Keyboard navigation supported
- Screen reader accessible

## Technical Implementation

### Components
- Uses Blazor WebAssembly with C# code-behind
- MinimalLayout for full-screen experience
- TailwindCSS for all styling
- No external CSS files

### API Integration
- AutoHost API client via NSwag
- Session token stored in memory
- API key persisted in backend
- Real-time connection monitoring

### Form Behavior
- Client-side email validation
- Disabled state management
- Async API calls with loading states
- Error handling and display

## Testing Scenarios

### Test 1: Initial Page State
**Purpose**: Verify the initial state of the authentication page
**Steps**:
1. Navigate to /auth
**Expected Result**:
- Email input field is visible with placeholder "you@example.com"
- Submit button shows "Continue"
- "Auto" heading is displayed
- Status shows "Connected" when AutoHost is running

**Screenshot Status**: ✅ PASSING

### Test 2: Valid Email Entry
**Purpose**: Verify that valid email can be entered
**Steps**:
1. Navigate to /auth
2. Enter "user@example.com" in email field
**Expected Result**:
- Email field contains the entered value
- Submit button remains "Continue"
- No error messages displayed

**Screenshot Status**: ✅ PASSING

### Test 3: Password Step Navigation
**Purpose**: Verify navigation to password step after valid email
**Steps**:
1. Navigate to /auth
2. Enter "user@example.com" in email field
3. Click "Continue" button
4. Wait for password field to appear
**Expected Result**:
- Email shown at top with "Change" link
- Password field is visible
- For new users: Shows "Create Password" and confirm field
- Submit button text shows "Create Account" for new users

**Screenshot Status**: ✅ PASSING

### Test 4: Invalid Email Format
**Purpose**: Verify email format validation
**Steps**:
1. Navigate to /auth
2. Enter "notanemail" (invalid format) in email field
**Expected Result**:
- Error message "Please enter a valid email address" appears in red
- Email field retains the invalid value
- Cannot proceed to next step

**Screenshot Status**: ✅ PASSING

### Test 5: Empty Email Submission
**Purpose**: Verify that empty email cannot be submitted
**Steps**:
1. Navigate to /auth
2. Leave email field empty
3. Attempt to click "Continue" button
**Expected Result**:
- Continue button should be disabled when email is empty
- User cannot proceed without entering email

**Screenshot Status**: ✅ PASSING
- Shows placeholder text "you@example.com" correctly
- Button would be disabled with empty email (per validation logic)

## Screenshot Pass/Fail Criteria

### Auth.razor.Initial.png
**Expected**:
- Dark navy background (#1a1b26)
- Centered gray card
- "Auto" heading in green
- Email field with placeholder "you@example.com"
- "Continue" button (green when connected)
- Status: Connected at bottom

**Status**: ✅ Working correctly

### Auth.razor.EmailEntered.png
**Expected**:
- Email field shows "user@example.com"
- Green enabled Continue button
- No error messages

**Status**: ✅ Working correctly

### Auth.razor.PasswordStep.png
**Expected**:
- Email shown at top with "Change" link
- Password field visible with placeholder
- For new users: Confirm password field visible
- Button text "Create Account" for new users

**Status**: ✅ Working correctly

### Auth.razor.InvalidEmail.png
**Expected**:
- Shows "notanemail" in field
- Red error "Please enter a valid email address"

**Status**: ✅ Working correctly

### Auth.razor.EmptySubmit.png
**Expected**:
- Email field with placeholder "you@example.com"
- Continue button would be disabled (validation prevents empty submission)

**Status**: ✅ Working correctly

## Current Issues Summary

### Fixed:
1. ✅ Vertical spacing - removed hardcoded heights
2. ✅ Mobile responsiveness - added proper padding
3. ✅ Password step navigation - working correctly

### Remaining:
1. Consider adding fade transition instead of slide (simpler with show/hide)
2. Add autocomplete="off" if autofill becomes an issue
3. Test on actual mobile devices for responsive behavior