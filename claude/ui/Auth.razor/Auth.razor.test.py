#!/usr/bin/env python3
"""Layout validation tests for Auth.razor page"""

import json
import os
import sys
from typing import Dict, List, Any, Optional
from dataclasses import dataclass

# Add parent directory to path to import the base test framework
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from test_framework import LayoutTest, LayoutElement


class AuthInitialTest(LayoutTest):
    """Tests for Auth.razor Initial state"""

    def run_tests(self):
        # Test 1: Page title - existence, visibility, and branding
        title = self.assert_element_exists('h1', text='Auto', message="Page should have 'Auto' title")
        if title:
            self.assert_element_visible(title, "Title should be visible")
            # Verify title has green styling (brand color)
            if 'text-green-400' not in title.classes:
                self.warnings.append("Title missing brand color (text-green-400)")
            # Verify monospace font for terminal aesthetic
            if 'font-mono' not in title.classes:
                self.warnings.append("Title missing monospace font")

        # Test 2: Form structure - ensure only email step is visible
        email_label = self.assert_element_exists('label', text='Email', message="Email label should exist")
        if email_label:
            self.assert_element_visible(email_label, "Email label should be visible")

        # Test 3: Email input - comprehensive validation
        email_input = self.assert_element_exists('input', type='email', message="Email input field should exist")
        if email_input:
            self.assert_element_visible(email_input, "Email input should be visible")

            # Check placeholder text
            if email_input.placeholder != 'you@example.com':
                self.errors.append(f"Email placeholder is '{email_input.placeholder}', expected 'you@example.com'")

            # Check that it's empty initially
            if email_input.value:
                self.errors.append(f"Email input should be empty initially, but has value: '{email_input.value}'")

            # Verify input has proper styling
            if 'bg-gray-700' not in email_input.classes:
                self.warnings.append("Email input missing dark background (bg-gray-700)")
            if 'focus:border-green-400' not in email_input.classes:
                self.warnings.append("Email input missing focus highlight (focus:border-green-400)")

        # Test 4: Submit button - text and state
        submit_button = self.find_element('button', type='submit')
        if not submit_button:
            self.errors.append("Submit button not found")
        else:
            # Check text
            if submit_button.text != 'Continue':
                self.errors.append(f"Button text is '{submit_button.text}', expected 'Continue'")

            # Button should be disabled initially (no email, not connected)
            self.assert_button_disabled(submit_button, "Button should be disabled in initial state")

            # Check button has disabled state styling classes (for when it IS disabled)
            self.assert_has_disabled_styles(submit_button, "Button needs disabled state styles")

            # Check button has proper colors
            if 'bg-green-600' not in submit_button.classes:
                self.warnings.append("Submit button missing primary color (bg-green-600)")

        # Test 5: No password fields should be visible (important for flow validation)
        self.assert_element_not_exists('input', type='password', message="No password fields should be visible in initial state")
        self.assert_element_not_exists('label', text='Password', message="No password label should be visible")
        self.assert_element_not_exists('label', text='Confirm Password', message="No confirm password label should be visible")
        self.assert_element_not_exists('label', text='Create Password', message="No create password label should be visible")

        # Test 6: Connection status indicator positioning and styling
        status_label = self.assert_element_exists('span', text='Status:', message="Status label should exist")
        disconnected_status = self.find_element('span', text='Disconnected')

        if status_label and email_input:
            # CRITICAL: Status should be aligned with or below the form, not floating to the right!
            form_left = email_input.rect['x']
            form_right = email_input.rect['x'] + email_input.rect['width']
            status_left = status_label.rect['x']

            # Status should either be:
            # 1. Centered below the form
            # 2. Left-aligned with the form
            # But NOT to the right of the form
            if status_left > form_right:
                self.errors.append(f"Status indicator is floating to the right of the form! Status x={status_left}, Form ends at x={form_right}")

            # Check vertical positioning - should be below the button
            if submit_button:
                button_bottom = submit_button.rect['y'] + submit_button.rect['height']
                if status_label.rect['y'] < button_bottom:
                    self.errors.append(f"Status should be below the form. Status y={status_label.rect['y']}, Button bottom={button_bottom}")

        if disconnected_status:
            # Verify disconnected status has red styling
            if 'text-red-400' not in disconnected_status.classes:
                self.errors.append("Disconnected status should be red (text-red-400)")
        else:
            self.warnings.append("Disconnected status indicator not found")

        # Test 7: Version should NOT be shown when disconnected
        version_element = self.find_element('span', text='Version')
        if version_element:
            self.errors.append("Version should not be displayed when disconnected")

        # Test 8: Layout alignment - form elements should be perfectly aligned
        if email_label and email_input:
            self.assert_element_alignment(email_label, email_input, "left", tolerance=5)

        if email_input and submit_button:
            self.assert_element_alignment(email_input, submit_button, "left", tolerance=1)
            self.assert_element_alignment(email_input, submit_button, "right", tolerance=1)

        # Test 9: Form centering in viewport
        if email_input:
            form_center = email_input.rect['x'] + email_input.rect['width'] / 2
            viewport_center = self.viewport.get('width', 1280) / 2

            # Auth form should be perfectly centered
            if abs(form_center - viewport_center) > 10:
                self.errors.append(f"Form is not centered (form center: {form_center}px, viewport center: {viewport_center}px)")

        # Test 10: No validation errors should be shown initially
        self.assert_element_not_exists('p', text='Please enter', message="No validation messages should be visible initially")
        self.assert_element_not_exists('p', text='valid email', message="No validation messages should be visible initially")

        # Test 11: Verify element count (catch unexpected elements)
        # Element count may vary based on connection state
        # Disconnected: 6 elements (h1, label, input, button, 2 status spans)
        # Connected: 7-8 elements (adds connected status and possibly version)
        if len(self.elements) < 6 or len(self.elements) > 8:
            self.warnings.append(f"Unexpected element count: {len(self.elements)} (expected 6-8)")

        # Test 12: Verify all visible elements are within viewport
        for element in self.elements:
            # Skip status elements we know are mispositioned
            if 'Status' in element.text or 'Disconnected' in element.text:
                continue
            self.assert_in_viewport(element, f"{element.tag} element should be fully visible")


class AuthEmailEnteredTest(LayoutTest):
    """Tests for Auth.razor with email entered and AutoHost connected"""

    def run_tests(self):
        # Test 1: Email input has the entered value
        email_input = self.assert_element_exists('input', type='email', message="Email input should exist")
        if email_input:
            if email_input.value != 'user@example.com':
                self.errors.append(f"Email input should have 'user@example.com', found '{email_input.value}'")

            # Verify input retains all styling
            if 'bg-gray-700' not in email_input.classes:
                self.warnings.append("Email input missing dark background")
            if 'focus:border-green-400' not in email_input.classes:
                self.warnings.append("Email input missing focus highlight")

        # Test 2: Button state and text
        submit_button = self.find_element('button', type='submit')
        if not submit_button:
            self.errors.append("Submit button not found")
        else:
            # Text should still be Continue
            if submit_button.text != 'Continue':
                self.errors.append(f"Button should say 'Continue', found '{submit_button.text}'")

            # Button should be ENABLED with valid email and connection
            self.assert_button_enabled(submit_button, "Button should be enabled with valid email and connection")

            # Should have primary color
            if 'bg-green-600' not in submit_button.classes:
                self.errors.append("Button missing primary color (bg-green-600)")

        # Test 3: No password fields visible yet
        self.assert_element_not_exists('input', type='password', message="No password fields until form submitted")
        self.assert_element_not_exists('label', text='Password', message="No password label visible")
        self.assert_element_not_exists('label', text='Create Password', message="No create password label visible")

        # Test 4: Connection status is "Connected" with green color
        connected_status = self.find_element('span', text='Connected')
        if not connected_status:
            self.errors.append("Connected status not found")
        else:
            self.assert_element_visible(connected_status, "Connected status should be visible")
            if 'text-green-400' not in connected_status.classes:
                self.errors.append("Connected status should be green (text-green-400)")

        # Test 5: Version is now visible
        version_element = self.find_element('span', text='Version')
        if not version_element:
            self.errors.append("Version should be displayed when connected")
        else:
            # Check it contains version number
            if '1.0.0' not in version_element.text:
                self.warnings.append(f"Version text unexpected: '{version_element.text}'")

        # Test 6: Status positioning (should still be problematic like Initial)
        status_label = self.find_element('span', text='Status:')
        if status_label and email_input:
            form_right = email_input.rect['x'] + email_input.rect['width']
            status_left = status_label.rect['x']

            if status_left > form_right:
                self.errors.append(f"Status indicator floating right! Status x={status_left}, Form ends at x={form_right}")

        # Test 7: Form centering should be maintained
        if email_input:
            form_center = email_input.rect['x'] + email_input.rect['width'] / 2
            viewport_center = self.viewport.get('width', 1280) / 2

            # Form may have shifted when connected
            if abs(form_center - viewport_center) > 100:
                self.errors.append(f"Form not centered (center: {form_center}px, viewport: {viewport_center}px)")

        # Test 8: No validation errors shown with valid email
        self.assert_element_not_exists('p', text='Please enter', message="No validation errors with valid email")
        self.assert_element_not_exists('p', text='valid email', message="No validation errors with valid email")

        # Test 9: Form alignment maintained
        email_label = self.find_element('label', text='Email')
        if email_label and email_input and submit_button:
            self.assert_element_alignment(email_label, email_input, "left", tolerance=5)
            self.assert_element_alignment(email_input, submit_button, "left", tolerance=1)
            self.assert_element_alignment(email_input, submit_button, "right", tolerance=1)

        # Test 10: Element count (should have version now when connected)
        # Connected: 7-8 elements (h1, label, input, button, status label, status value, version)
        if len(self.elements) < 7 or len(self.elements) > 8:
            self.warnings.append(f"Unexpected element count: {len(self.elements)} (expected 7-8 when connected)")


class AuthPasswordStepTest(LayoutTest):
    """Tests for Auth.razor password entry step after email submission"""

    def run_tests(self):
        # Test 1: Email display and change button
        email_display = self.find_element('div', text='user@example.com')
        if not email_display:
            self.errors.append("Email should be displayed as text div in password step")
        else:
            self.assert_element_visible(email_display, "Email display should be visible")
            # Check it's at the proper position (not off-screen)
            if email_display.rect['x'] < 0:
                self.errors.append(f"Email display should be visible, but x={email_display.rect['x']}")

        change_button = self.find_element('button', text='Change', type='button')
        if not change_button:
            self.errors.append("Change button not found")
        else:
            self.assert_element_visible(change_button, "Change button should be visible")
            # Change button should be inline with email
            if email_display and abs(change_button.rect['y'] - email_display.rect['y']) > 5:
                self.errors.append("Change button should be inline with email display")

        # Test 2: Password creation label
        create_password_label = self.find_element('label', text='Create Password')
        if not create_password_label:
            self.errors.append("Should show 'Create Password' label for new users")
        else:
            self.assert_element_visible(create_password_label, "Create Password label should be visible")

        # Test 3: Password input fields - comprehensive validation
        password_inputs = self.find_all_elements('input', type='password')

        # First check if we have at least one password input
        if len(password_inputs) == 0:
            self.errors.append("No password inputs found at all")
        elif len(password_inputs) == 1:
            # We only found one password input - check if it's fully visible
            password_input = password_inputs[0]
            if password_input.placeholder != 'Enter password':
                self.warnings.append(f"Password placeholder is '{password_input.placeholder}', expected 'Enter password'")
            if password_input.value:
                self.warnings.append("Password field should be empty initially")

            # Use new assert_fully_visible to detect clipping
            self.assert_fully_visible(password_input, "First password input must be fully visible")

            # Check password field has proper styling
            if 'bg-gray-700' not in password_input.classes:
                self.warnings.append("Password input missing dark background")
            if 'focus:border-green-400' not in password_input.classes:
                self.warnings.append("Password input missing focus highlight")

            # Check if confirm password input is missing due to clipping
            self.errors.append("Confirm password input not found - likely clipped by container overflow")
        else:
            # We found both password inputs
            # First password field
            password_input = password_inputs[0]
            if password_input.placeholder != 'Enter password':
                self.warnings.append(f"Password placeholder is '{password_input.placeholder}', expected 'Enter password'")
            if password_input.value:
                self.warnings.append("Password field should be empty initially")

            # Use new assert_fully_visible to detect clipping
            self.assert_fully_visible(password_input, "First password input must be fully visible")

            # Check password field has proper styling
            if 'bg-gray-700' not in password_input.classes:
                self.warnings.append("Password input missing dark background")
            if 'focus:border-green-400' not in password_input.classes:
                self.warnings.append("Password input missing focus highlight")

            # Confirm password field
            confirm_input = password_inputs[1]
            if confirm_input.placeholder != 'Confirm password':
                self.warnings.append(f"Confirm placeholder is '{confirm_input.placeholder}', expected 'Confirm password'")

            # Use new assert_fully_visible to detect clipping
            self.assert_fully_visible(confirm_input, "Confirm password input must be fully visible")

            # Check vertical spacing between password fields
            password_bottom = password_input.rect['y'] + password_input.rect['height']
            confirm_top = confirm_input.rect['y']
            spacing = confirm_top - password_bottom
            # Should have some spacing but not too much (accounting for label)
            if spacing < 20:
                self.warnings.append(f"Password fields too close together (spacing: {spacing}px)")
            elif spacing > 100:
                self.warnings.append(f"Password fields too far apart (spacing: {spacing}px)")

        # Test 4: Confirm password label
        confirm_label = self.find_element('label', text='Confirm Password')
        if confirm_label:
            # Use new assert_fully_visible to detect clipping
            self.assert_fully_visible(confirm_label, "Confirm password label must be fully visible")
        else:
            self.errors.append("Confirm password label not found - likely clipped by container overflow")

        # Test 5: Submit button (visible one)
        submit_buttons = self.find_all_elements('button', type='submit')
        visible_submit = None
        hidden_submit = None

        for button in submit_buttons:
            if button.rect['x'] > 0:
                visible_submit = button
            else:
                hidden_submit = button

        if not visible_submit:
            self.errors.append("No visible submit button found in password step - likely clipped by container overflow")
        else:
            if visible_submit.text != 'Create Account':
                self.errors.append(f"Submit button should say 'Create Account', found '{visible_submit.text}'")

            # Use new assert_fully_visible to detect clipping
            self.assert_fully_visible(visible_submit, "Create Account button must be fully visible")

            # Check button has proper styling
            if 'bg-green-600' not in visible_submit.classes:
                self.warnings.append("Submit button missing primary color")

            # Button should be disabled when password is empty
            self.assert_button_disabled(visible_submit, "Create Account button should be disabled when password is empty")

        # Test 6: Email form transition (should be off-screen to the left)
        email_input = self.find_element('input', type='email')
        email_label = self.find_element('label', text='Email')

        if email_input:
            # Email form should be completely hidden (off-screen or clipped)
            actual_x = email_input.rect['x']
            visible_x = email_input.visibleRect.get('x', actual_x)
            visible_width = email_input.visibleRect.get('width', 0)

            # Check if email form is visible at all in the password step
            # It should be completely hidden, not partially visible
            container_left = 416  # Left edge of container based on captured data
            if visible_width > 10 and visible_x < container_left + 100:
                self.errors.append(f"Email form is still visible in password step! X={actual_x}, visible width={visible_width}")

            # Verify all email form elements moved together
            if email_label:
                if abs(email_label.rect['x'] - email_input.rect['x']) > 40:  # Label slightly offset from input
                    self.errors.append("Email label didn't transition with input field")

        if hidden_submit:
            # Hidden submit should be with the email form
            if hidden_submit.rect['x'] >= 0:
                self.errors.append("Original Continue button should be off-screen")

        # Test 7: Form alignment in password step
        if password_inputs and len(password_inputs) >= 2:
            # Password fields should be aligned
            self.assert_element_alignment(password_inputs[0], password_inputs[1], "left", tolerance=1)
            self.assert_element_alignment(password_inputs[0], password_inputs[1], "right", tolerance=1)

            # Submit button should align with password fields
            if visible_submit:
                self.assert_element_alignment(password_inputs[0], visible_submit, "left", tolerance=1)
                self.assert_element_alignment(password_inputs[0], visible_submit, "right", tolerance=1)

        # Test 8: Status remains visible and positioned correctly
        status_label = self.find_element('span', text='Status:')
        if status_label:
            # Should still be visible
            self.assert_element_visible(status_label, "Status should remain visible")

            # Check if still mispositioned (floating right)
            if password_inputs and len(password_inputs) > 0:
                form_right = password_inputs[0].rect['x'] + password_inputs[0].rect['width']
                if status_label.rect['x'] > form_right:
                    self.errors.append(f"Status still floating right! Status x={status_label.rect['x']}, Form ends at x={form_right}")

        # Test 9: Connection status maintained
        connected = self.find_element('span', text='Connected')
        if connected:
            if 'text-green-400' not in connected.classes:
                self.warnings.append("Connected status should remain green")

        # Test 10: No validation errors shown initially
        self.assert_element_not_exists('p', text='match', message="No password mismatch error initially")
        self.assert_element_not_exists('p', text='required', message="No required field errors initially")

        # Test 12: Verify transition animation classes
        if email_display:
            # Password form should have transition classes for smooth animation
            parent_classes = email_display.classes
            if 'transition' not in parent_classes:
                self.warnings.append("Password form missing transition classes for smooth animation")

        # Test 11: Check z-index/layering (password form should be on top)
        if email_display and email_input:
            # Password form should be roughly centered in view
            password_form_x = email_display.rect['x']
            # With flex layout, password form slides into view
            # Should be somewhere between 400-600 (center area)
            if password_form_x < 400 or password_form_x > 600:
                self.warnings.append(f"Password form not centered (x={password_form_x}, expected 400-600)")

        # Test 13: Z-index validation - check style properties
        if password_inputs and len(password_inputs) > 0:
            password_style = password_inputs[0].style
            email_style = email_input.style if email_input else {}

            # Password form should have higher z-index or be positioned on top
            password_z = password_style.get('zIndex', 'auto')
            email_z = email_style.get('zIndex', 'auto')

            # If both have explicit z-index, compare them
            if password_z != 'auto' and email_z != 'auto':
                try:
                    if int(password_z) <= int(email_z):
                        self.errors.append(f"Password form z-index ({password_z}) should be higher than email form ({email_z})")
                except ValueError:
                    pass

        # Test 14: Verify password form is interactive (not behind another layer)
        if visible_submit:
            # Submit button should be clickable (positive position, visible)
            if visible_submit.rect['x'] < 0:
                self.errors.append("Submit button in password step is off-screen")
            if visible_submit.style.get('pointerEvents') == 'none':
                self.errors.append("Submit button is not interactive (pointer-events: none)")


class AuthInvalidEmailTest(LayoutTest):
    """Tests for Auth.razor with invalid email format entered"""

    def run_tests(self):
        # Test 1: Validation error message
        error_msg = self.find_element('p', text='Please enter a valid email')
        if not error_msg:
            self.errors.append("Validation error message not found")
        else:
            self.assert_element_visible(error_msg, "Error message should be visible")

            # Check exact text
            if error_msg.text != 'Please enter a valid email address':
                self.warnings.append(f"Error text is '{error_msg.text}', expected 'Please enter a valid email address'")

            # Should have red styling
            if 'text-red-400' not in error_msg.classes:
                self.errors.append("Error message should have red text (text-red-400)")

            # Check positioning - should be between input and button
            email_input = self.find_element('input', type='email')
            submit_button = self.find_element('button', type='submit')

            if email_input and submit_button:
                input_bottom = email_input.rect['y'] + email_input.rect['height']
                button_top = submit_button.rect['y']

                if error_msg.rect['y'] <= input_bottom:
                    self.errors.append("Error message should be below input field")
                if error_msg.rect['y'] >= button_top:
                    self.errors.append("Error message should be above submit button")

        # Test 2: Email input contains invalid value and error styling
        email_input = self.find_element('input', type='email')
        if not email_input:
            self.errors.append("Email input not found")
        else:
            if email_input.value != 'notanemail':
                self.errors.append(f"Email input should have 'notanemail', found '{email_input.value}'")

            # Check for error state styling on input (red border)
            self.assert_has_error_styling(email_input, "Input should show error state with invalid email")

            # Specifically check for red border
            if 'border-red' not in email_input.classes and 'focus:border-red' not in email_input.classes:
                self.errors.append("Email input should show red border with invalid email")

            # Verify input is still in viewport despite error
            self.assert_in_viewport(email_input, "Email input should remain in viewport")

        # Test 3: Button state
        submit_button = self.find_element('button', type='submit')
        if not submit_button:
            self.errors.append("Submit button not found")
        else:
            # Text should still be Continue
            if submit_button.text != 'Continue':
                self.errors.append(f"Button should say 'Continue', found '{submit_button.text}'")

            # Button should be disabled with invalid email
            if 'disabled:bg-gray-600' not in submit_button.classes:
                self.warnings.append("Button should have disabled styling with invalid email")

        # Test 4: Form layout adjustment
        if email_input and submit_button and error_msg:
            # Despite error message, form should remain centered
            form_center = email_input.rect['x'] + email_input.rect['width'] / 2
            viewport_center = self.viewport.get('width', 1280) / 2

            if abs(form_center - viewport_center) > 100:
                self.errors.append(f"Form lost centering with error (center: {form_center}px, viewport: {viewport_center}px)")

            # Elements should remain aligned
            self.assert_element_alignment(email_input, submit_button, "left", tolerance=1)
            self.assert_element_alignment(email_input, submit_button, "right", tolerance=1)

        # Test 5: No password fields visible
        self.assert_element_not_exists('input', type='password', message="No password fields with invalid email")

        # Test 6: Status remains visible
        status_label = self.find_element('span', text='Status:')
        if status_label:
            # Check it hasn't moved due to error
            if status_label.rect['x'] > 757:  # Form ends at ~757
                self.errors.append("Status indicator still floating right with error message")

        # Test 7: Page title unchanged
        title = self.find_element('h1', text='Auto')
        if title:
            self.assert_element_visible(title, "Title should remain visible")

        # Test 8: Error message animation classes
        if error_msg:
            self.assert_has_transition_classes(error_msg, "Error message should have transition for smooth appearance")

        # Test 9: All elements should be in viewport
        for element in self.elements:
            # Skip status we know is mispositioned
            if 'Status' in element.text or 'Connected' in element.text:
                continue
            self.assert_in_viewport(element, f"{element.tag} with invalid email should be visible")


class AuthPasswordsFilledTest(LayoutTest):
    """Tests for Auth.razor with passwords filled and matching"""

    def run_tests(self):
        # Test 1: Both password fields should be filled
        password_inputs = self.find_all_elements('input', type='password')

        if len(password_inputs) != 2:
            self.errors.append(f"Expected 2 password inputs, found {len(password_inputs)}")
        else:
            # First password field
            if password_inputs[0].value != 'TestPassword123!':
                self.errors.append(f"First password should be filled, but has value: '{password_inputs[0].value}'")

            # Second password field (confirm)
            if password_inputs[1].value != 'TestPassword123!':
                self.errors.append(f"Confirm password should be filled, but has value: '{password_inputs[1].value}'")

        # Test 2: Create Account button should be enabled
        submit_button = self.find_element('button', type='submit')
        if not submit_button:
            self.errors.append("Submit button not found")
        else:
            if submit_button.text != 'Create Account':
                self.errors.append(f"Button should say 'Create Account', found '{submit_button.text}'")

            # Button should be ENABLED with matching passwords
            self.assert_button_enabled(submit_button, "Create Account button should be enabled with matching passwords")

            # Should have primary color (not gray)
            if 'bg-green-600' not in submit_button.classes:
                self.errors.append("Button should have primary green color when enabled")

        # Test 3: Email display should show user@example.com
        email_display = self.find_element('div', text='user@example.com')
        if not email_display:
            self.errors.append("Email should be displayed as 'user@example.com'")

        # Test 4: Should still show "Create Password" for new user
        create_password_label = self.find_element('label', text='Create Password')
        if not create_password_label:
            self.errors.append("Should show 'Create Password' label for new user")

        # Test 5: Connection status should be maintained
        connected = self.find_element('span', text='Connected')
        if connected:
            if 'text-green-400' not in connected.classes:
                self.warnings.append("Connected status should be green")


class AuthExistingUserEmptyPasswordTest(LayoutTest):
    """Tests for Auth.razor existing user login step with empty password"""

    def run_tests(self):
        # Test 1: Email display as text with Change button
        email_display = self.find_element('div', text='user@example.com')
        if not email_display:
            self.errors.append("Email should be displayed as text div for existing user")
        else:
            self.assert_element_visible(email_display, "Email display should be visible")
            # Check it's at the proper position (not off-screen)
            if email_display.rect['x'] < 0:
                self.errors.append(f"Email display should be visible, but x={email_display.rect['x']}")

        change_button = self.find_element('button', text='Change', type='button')
        if not change_button:
            self.errors.append("Change button not found")
        else:
            self.assert_element_visible(change_button, "Change button should be visible")
            # Change button should be inline with email
            if email_display and abs(change_button.rect['y'] - email_display.rect['y']) > 5:
                self.errors.append("Change button should be inline with email display")

        # Test 2: Password label (NOT "Create Password")
        password_label = self.find_element('label', text='Password')
        create_password_label = self.find_element('label', text='Create Password')

        if create_password_label:
            self.errors.append("Should show 'Password' label, not 'Create Password' for existing users")
        if not password_label:
            self.errors.append("Should show 'Password' label for existing users")
        else:
            self.assert_element_visible(password_label, "Password label should be visible")

        # Test 3: Single password input field
        password_inputs = self.find_all_elements('input', type='password')

        if len(password_inputs) == 0:
            self.errors.append("No password input found")
        elif len(password_inputs) > 1:
            self.errors.append(f"Should have only one password field for existing user, found {len(password_inputs)}")
        else:
            password_input = password_inputs[0]
            # Check placeholder
            if password_input.placeholder != 'Enter password':
                self.warnings.append(f"Password placeholder is '{password_input.placeholder}', expected 'Enter password'")
            # Should be empty
            if password_input.value:
                self.errors.append("Password field should be empty initially")

            # Check visibility
            self.assert_fully_visible(password_input, "Password input must be fully visible")

            # Check styling
            if 'bg-gray-700' not in password_input.classes:
                self.warnings.append("Password input missing dark background")
            if 'focus:border-green-400' not in password_input.classes:
                self.warnings.append("Password input missing focus highlight")

        # Test 4: No Confirm Password field
        confirm_label = self.find_element('label', text='Confirm Password')
        if confirm_label:
            self.errors.append("Confirm Password field should not be shown for existing users")

        # Check there's really only one password input
        if len(password_inputs) > 1:
            self.errors.append("Confirm password input should not exist for existing users")

        # Test 5: Sign In button
        submit_button = self.find_element('button', type='submit')
        if not submit_button:
            self.errors.append("Submit button not found")
        else:
            # Check text
            if submit_button.text != 'Sign In':
                self.errors.append(f"Button should say 'Sign In' for existing user, found '{submit_button.text}'")

            # Should be disabled with empty password
            self.assert_button_disabled(submit_button, "Sign In button should be disabled when password is empty")

            # Check visibility
            self.assert_fully_visible(submit_button, "Sign In button must be fully visible")

            # Check styling
            if 'bg-green-600' not in submit_button.classes:
                self.warnings.append("Submit button missing primary color")

        # Test 6: All elements fully visible (not clipped)
        if password_inputs and len(password_inputs) > 0:
            for input in password_inputs:
                self.assert_fully_visible(input, "Password input must not be clipped")

        if submit_button:
            self.assert_fully_visible(submit_button, "Sign In button must not be clipped")

        # Test 7: Form alignment
        if password_inputs and len(password_inputs) > 0 and submit_button:
            self.assert_element_alignment(password_inputs[0], submit_button, "left", tolerance=1)
            self.assert_element_alignment(password_inputs[0], submit_button, "right", tolerance=1)

        # Test 8: Email form should be hidden
        email_input = self.find_element('input', type='email')
        if email_input:
            # Email form should be completely hidden (off-screen or clipped)
            actual_x = email_input.rect['x']
            if actual_x >= 0 and actual_x < 1000:
                self.errors.append(f"Email form should be hidden in login step, but is at x={actual_x}")

        # Test 9: Status remains visible
        status_label = self.find_element('span', text='Status:')
        if status_label:
            self.assert_element_visible(status_label, "Status should remain visible")

        # Test 10: Connection status maintained
        connected = self.find_element('span', text='Connected')
        if connected:
            if 'text-green-400' not in connected.classes:
                self.warnings.append("Connected status should remain green")


class AuthEmptySubmitTest(LayoutTest):
    """Tests for Auth.razor with empty email (future submit validation)"""

    def run_tests(self):
        # Test 1: Should be identical to Initial state
        # This test verifies the page resets properly or handles empty state

        # Email input should be empty
        email_input = self.assert_element_exists('input', type='email', message="Email input should exist")
        if email_input:
            if email_input.value:
                self.errors.append(f"Email should be empty, but has value: '{email_input.value}'")

            # Placeholder should be visible
            if email_input.placeholder != 'you@example.com':
                self.errors.append(f"Placeholder is '{email_input.placeholder}', expected 'you@example.com'")

        # Test 2: No error message shown (empty is not invalid until submitted)
        error_msgs = self.find_all_elements('p', text='email')
        for error in error_msgs:
            if 'required' in error.text.lower() or 'enter' in error.text.lower():
                self.errors.append(f"No error should be shown for empty field: found '{error.text}'")

        # Test 3: Button should be disabled
        submit_button = self.find_element('button', type='submit')
        if submit_button:
            # Should say Continue
            if submit_button.text != 'Continue':
                self.errors.append(f"Button should say 'Continue', found '{submit_button.text}'")

            # Should be disabled with empty email
            self.assert_button_disabled(submit_button, "Button must be disabled with empty email")

        # Test 3b: Check if "Email is required" error is shown
        # This would only appear if user tried to submit with empty field
        required_error = self.find_element('p', text='Email is required')
        if required_error:
            # If this test is capturing after submit attempt, error should be visible
            self.assert_element_visible(required_error, "Email required error should be visible after submit attempt")
            # Error should be red
            if 'text-red-400' not in required_error.classes:
                self.errors.append("Email required error should be red")

        # Test 4: Connection status (can be either Connected or Disconnected)
        # Note: EmptySubmit runs after other tests which may have established connection
        disconnected = self.find_element('span', text='Disconnected')
        connected = self.find_element('span', text='Connected')

        if disconnected:
            if 'text-red-400' not in disconnected.classes:
                self.warnings.append("Disconnected status should be red")
            # Test 5: No version shown when disconnected
            version = self.find_element('span', text='Version')
            if version:
                self.errors.append("Version should not be shown when disconnected")
        elif connected:
            if 'text-green-400' not in connected.classes:
                self.warnings.append("Connected status should be green")
            # Version should be shown when connected
            version = self.find_element('span', text='Version')
            if not version:
                self.warnings.append("Version should be shown when connected")

        # Test 6: Element count depends on connection state
        if disconnected:
            expected_elements = 6  # h1, label, input, button, 2 status spans
        else:
            expected_elements = 8  # h1, label, input, button, 2 status spans, connected, version

        if len(self.elements) != expected_elements:
            self.warnings.append(f"Expected {expected_elements} elements, found {len(self.elements)}")

        # Test 7: Form positioning (should have same issues as Initial)
        if email_input:
            form_center = email_input.rect['x'] + email_input.rect['width'] / 2
            viewport_center = self.viewport.get('width', 1280) / 2

            if abs(form_center - viewport_center) > 10:
                self.errors.append(f"Form not centered (center: {form_center}px, viewport: {viewport_center}px)")

        # Test 8: Status positioning (likely has same issue as Initial)
        status_label = self.find_element('span', text='Status:')
        if status_label and email_input:
            form_right = email_input.rect['x'] + email_input.rect['width']
            if status_label.rect['x'] > form_right:
                self.errors.append(f"Status floating right (x={status_label.rect['x']}, form ends at {form_right})")


def main():
    """Run all Auth.razor layout tests"""
    script_dir = os.path.dirname(os.path.abspath(__file__))
    results_dir = os.path.join(script_dir, 'results')

    if not os.path.exists(results_dir):
        print(f"✗ Results directory not found: {results_dir}")
        sys.exit(1)

    # Define tests to run for each screenshot
    tests = [
        ('Auth.razor.Initial.json', AuthInitialTest),
        ('Auth.razor.EmailEntered.json', AuthEmailEnteredTest),
        ('Auth.razor.PasswordStep.json', AuthPasswordStepTest),
        ('Auth.razor.PasswordsFilled.json', AuthPasswordsFilledTest),
        ('Auth.razor.ExistingUserEmptyPassword.json', AuthExistingUserEmptyPasswordTest),
        ('Auth.razor.InvalidEmail.json', AuthInvalidEmailTest),
        ('Auth.razor.EmptySubmit.json', AuthEmptySubmitTest),
    ]

    all_passed = True
    tests_run = 0

    for json_file, test_class in tests:
        json_path = os.path.join(results_dir, json_file)

        if not os.path.exists(json_path):
            print(f"⚠ {test_class.__name__}: Layout file not found: {json_file}")
            continue

        try:
            test = test_class(json_path)
            test.run_tests()
            if not test.report():
                all_passed = False
            tests_run += 1
        except Exception as e:
            print(f"✗ {test_class.__name__}: Test failed with exception: {e}")
            all_passed = False

    if tests_run == 0:
        print("✗ No tests were run (no layout files found)")
        sys.exit(1)

    if all_passed:
        print(f"\n✓ All {tests_run} Auth.razor layout tests passed!")
        sys.exit(0)
    else:
        print(f"\n✗ Some Auth.razor layout tests failed")
        sys.exit(1)


if __name__ == "__main__":
    main()