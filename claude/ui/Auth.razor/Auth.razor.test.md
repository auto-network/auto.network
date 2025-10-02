# Auth.razor Test Expectations

## Test Flow Strategy
Tests run in sequence with an isolated test database:
1. Initial - Empty form
2. EmailEntered - Email filled but not submitted
3. PasswordStep - New user sees password creation form
4. **CreateAccount** - Actually create account for user@example.com
5. InvalidEmail - Test validation
6. EmptySubmit - Test empty submission
7. ExistingUser - Test login with user@example.com (now exists from step 4)

## AuthInitialTest
- [x] Title "Auto" is visible with green color and monospace font
- [x] Email label and input are visible
- [x] Email input is empty with placeholder "you@example.com"
- [x] Continue button is visible and shows "Continue"
- [x] Continue button is disabled when disconnected
- [x] No password fields are visible
- [x] Status shows "Disconnected" in red
- [ ] No version shown when disconnected

## AuthEmailEnteredTest
- [x] Email input contains "user@example.com"
- [x] Continue button still shows "Continue"
- [x] Continue button is enabled with valid email and connection
- [x] No password fields visible until form submitted
- [x] Status shows "Connected" in green
- [x] Version "1.0.0" is visible when connected

## AuthPasswordStepTest
- [x] Email shown as text (not input) with Change button
- [x] "Create Password" label is visible
- [x] Password input field is visible and empty
- [x] Confirm Password input field is visible and empty
- [x] Button shows "Create Account"
- [x] Create Account button is disabled when password is empty (gray styling)
- [ ] Create Account button is disabled when passwords don't match
- [x] All password elements are fully visible (not clipped)
- [x] Email form is completely hidden (not bleeding through)

## AuthCreateAccountTest
- [ ] Both password fields filled with matching passwords
- [ ] Create Account button is enabled (green, not gray)
- [ ] Clicking Create Account successfully creates the user
- [ ] User is redirected to main chat interface
- [ ] Account is persisted in test database for later tests

## AuthExistingUserEmptyPasswordTest
- [ ] Email shown as text with Change button
- [ ] "Password" label is visible (not "Create Password")
- [ ] Single password input field is visible
- [ ] No Confirm Password field shown
- [ ] Button shows "Sign In"
- [ ] Sign In button is disabled when password is empty
- [ ] All elements are fully visible (not clipped)

## AuthInvalidEmailTest
- [x] Email input contains invalid text "notanemail"
- [x] Error message "Please enter a valid email address" is visible
- [x] Error message has red color
- [x] Continue button is disabled
- [ ] Email input shows error styling (red border)

## AuthEmptySubmitTest
- [x] Email input is empty
- [x] Continue button shows "Continue"
- [x] Continue button is disabled
- [ ] Clicking Continue shows "Email is required" error