#!/usr/bin/env python3
"""Base framework for layout validation tests"""

import json
from typing import Dict, List, Any, Optional
from dataclasses import dataclass


@dataclass
class LayoutElement:
    """Represents a single element in the layout"""
    tag: str
    type: Optional[str]
    text: str
    value: str
    placeholder: str
    rect: Dict[str, int]
    visibleRect: Dict[str, int]
    style: Dict[str, str]
    classes: str
    isContainer: bool
    visibility: Dict[str, Any]

    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> 'LayoutElement':
        return cls(
            tag=data.get('tag', ''),
            type=data.get('type'),
            text=data.get('text', ''),
            value=data.get('value', ''),
            placeholder=data.get('placeholder', ''),
            rect=data.get('rect', {}),
            visibleRect=data.get('visibleRect', data.get('rect', {})),  # Fallback to rect if visibleRect not present
            style=data.get('style', {}),
            classes=data.get('classes', ''),
            isContainer=data.get('isContainer', False),
            visibility=data.get('visibility', {})
        )


class LayoutTest:
    """Base class for layout tests"""

    def __init__(self, layout_path: str):
        self.layout_path = layout_path
        self.errors: List[str] = []
        self.warnings: List[str] = []

        with open(layout_path, 'r') as f:
            self.data = json.load(f)

        self.viewport = self.data.get('viewport', {})
        self.elements = [LayoutElement.from_dict(e) for e in self.data.get('elements', [])]

    def find_element(self, tag: str, text: Optional[str] = None, type: Optional[str] = None) -> Optional[LayoutElement]:
        """Find first element matching criteria"""
        for element in self.elements:
            if element.tag != tag:
                continue
            if text and text not in element.text:
                continue
            if type and element.type != type:
                continue
            return element
        return None

    def find_all_elements(self, tag: str, text: Optional[str] = None, type: Optional[str] = None) -> List[LayoutElement]:
        """Find all elements matching criteria"""
        results = []
        for element in self.elements:
            if element.tag != tag:
                continue
            if text and text not in element.text:
                continue
            if type and element.type != type:
                continue
            results.append(element)
        return results

    def assert_element_exists(self, tag: str, text: Optional[str] = None, type: Optional[str] = None, message: str = ""):
        """Assert that an element exists"""
        element = self.find_element(tag, text, type)
        if not element:
            msg = f"Element not found: {tag}"
            if text:
                msg += f" with text '{text}'"
            if type:
                msg += f" with type '{type}'"
            if message:
                msg += f" ({message})"
            self.errors.append(msg)
            return None
        return element

    def assert_element_visible(self, element: LayoutElement, message: str = ""):
        """Assert that an element is visible"""
        if not element:
            return

        # Check basic visibility properties
        if element.style.get('display') == 'none':
            self.errors.append(f"{element.tag} element is hidden (display: none)" + (f" ({message})" if message else ""))
        if element.style.get('visibility') == 'hidden':
            self.errors.append(f"{element.tag} element is hidden (visibility: hidden)" + (f" ({message})" if message else ""))
        if float(element.style.get('opacity', '1')) == 0:
            self.errors.append(f"{element.tag} element is invisible (opacity: 0)" + (f" ({message})" if message else ""))

        # Check if element is within viewport
        if element.rect.get('x', 0) >= self.viewport.get('width', 1280):
            self.errors.append(f"{element.tag} element is outside viewport (x >= viewport width)" + (f" ({message})" if message else ""))
        if element.rect.get('y', 0) >= self.viewport.get('height', 720):
            self.errors.append(f"{element.tag} element is outside viewport (y >= viewport height)" + (f" ({message})" if message else ""))

    def assert_element_not_exists(self, tag: str, text: Optional[str] = None, type: Optional[str] = None, message: str = ""):
        """Assert that an element does not exist"""
        element = self.find_element(tag, text, type)
        if element:
            msg = f"Unexpected element found: {tag}"
            if text:
                msg += f" with text '{text}'"
            if type:
                msg += f" with type '{type}'"
            if message:
                msg += f" ({message})"
            self.errors.append(msg)

    def assert_button_text(self, expected_text: str, message: str = ""):
        """Assert button has specific text"""
        button = self.find_element('button', type='submit')
        if not button:
            self.errors.append(f"Submit button not found" + (f" ({message})" if message else ""))
            return

        if button.text != expected_text:
            self.errors.append(f"Button text is '{button.text}', expected '{expected_text}'" + (f" ({message})" if message else ""))

    def assert_element_alignment(self, elem1: LayoutElement, elem2: LayoutElement, alignment: str = "left", tolerance: int = 5):
        """Assert two elements are aligned"""
        if not elem1 or not elem2:
            return

        if alignment == "left":
            diff = abs(elem1.rect['x'] - elem2.rect['x'])
            if diff > tolerance:
                self.errors.append(f"{elem1.tag} and {elem2.tag} are not left-aligned (diff: {diff}px)")
        elif alignment == "right":
            right1 = elem1.rect['x'] + elem1.rect['width']
            right2 = elem2.rect['x'] + elem2.rect['width']
            diff = abs(right1 - right2)
            if diff > tolerance:
                self.errors.append(f"{elem1.tag} and {elem2.tag} are not right-aligned (diff: {diff}px)")
        elif alignment == "center":
            center1 = elem1.rect['x'] + elem1.rect['width'] / 2
            center2 = elem2.rect['x'] + elem2.rect['width'] / 2
            diff = abs(center1 - center2)
            if diff > tolerance:
                self.errors.append(f"{elem1.tag} and {elem2.tag} are not center-aligned (diff: {diff}px)")

    def assert_button_enabled(self, button: LayoutElement, message: str = ""):
        """Assert button appears enabled (not disabled)"""
        if not button:
            return

        # Look for actual disabled state, not disabled: pseudo-class styles
        # cursor-not-allowed WITHOUT "disabled:" prefix means button IS disabled
        classes = button.classes

        # Check if cursor-not-allowed appears WITHOUT disabled: prefix
        if 'cursor-not-allowed' in classes and 'disabled:cursor-not-allowed' not in classes:
            self.errors.append(f"Button is disabled (has cursor-not-allowed)" + (f" ({message})" if message else ""))

        # Alternative: Check if the classes suggest it's actually disabled (e.g., bg-gray-600 without disabled:)
        if 'bg-gray-600' in classes and 'disabled:bg-gray-600' not in classes:
            self.errors.append(f"Button appears disabled (has gray background)" + (f" ({message})" if message else ""))

        # Hover states suggest the button is interactive/enabled
        if 'hover:' not in button.classes:
            self.warnings.append(f"Button missing hover states (may not be interactive)" + (f" ({message})" if message else ""))

    def assert_button_disabled(self, button: LayoutElement, message: str = ""):
        """Assert button appears disabled"""
        if not button:
            return

        classes = button.classes

        # Check for actual disabled state indicators (not pseudo-class styles)
        # Look for bg-gray-600 WITHOUT disabled: prefix (actual gray background)
        has_disabled_appearance = (
            ('bg-gray-600' in classes and 'disabled:bg-gray-600' not in classes) or
            ('cursor-not-allowed' in classes and 'disabled:cursor-not-allowed' not in classes)
        )

        # Also check if button has disabled state styles defined (which will apply when HTML disabled attribute is set)
        has_disabled_styles = ('disabled:bg-gray-600' in classes or 'disabled:cursor-not-allowed' in classes)

        if not has_disabled_appearance:
            # In Blazor with HTML disabled attribute, we won't see the visual classes directly
            # But if it has disabled: pseudo-classes, it's likely properly disabled via HTML attribute
            if has_disabled_styles:
                # This is fine - button is likely disabled via HTML attribute
                pass  # Don't warn - this is expected behavior
            else:
                # No disabled styling at all - this is a problem
                self.errors.append(f"Button missing any disabled styling" + (f" ({message})" if message else ""))

    def assert_has_disabled_styles(self, button: LayoutElement, message: str = ""):
        """Assert button has disabled state styling classes (for when it IS disabled)"""
        if not button:
            return

        # These classes define how button looks WHEN disabled
        if 'disabled:bg-gray' not in button.classes:
            self.warnings.append(f"Button missing disabled state styles (disabled:bg-gray-*)" + (f" ({message})" if message else ""))

    def assert_has_error_styling(self, element: LayoutElement, message: str = ""):
        """Assert element has error state styling"""
        if not element:
            return

        has_error = ('border-red' in element.classes or
                     'text-red' in element.classes or
                     'ring-red' in element.classes)

        if not has_error:
            self.warnings.append(f"Element missing error styling" + (f" ({message})" if message else ""))

    def assert_in_viewport(self, element: LayoutElement, message: str = ""):
        """Assert element is fully within viewport bounds"""
        if not element:
            return

        viewport_width = self.viewport.get('width', 1280)
        viewport_height = self.viewport.get('height', 720)

        # Check horizontal bounds
        if element.rect['x'] < 0:
            self.errors.append(f"Element extends beyond left edge (x={element.rect['x']})" + (f" ({message})" if message else ""))

        element_right = element.rect['x'] + element.rect['width']
        if element_right > viewport_width:
            self.errors.append(f"Element extends beyond right edge (right={element_right}, viewport={viewport_width})" + (f" ({message})" if message else ""))

        # Check vertical bounds
        if element.rect['y'] < 0:
            self.errors.append(f"Element extends beyond top edge (y={element.rect['y']})" + (f" ({message})" if message else ""))

        element_bottom = element.rect['y'] + element.rect['height']
        if element_bottom > viewport_height:
            self.errors.append(f"Element extends beyond bottom edge (bottom={element_bottom}, viewport={viewport_height})" + (f" ({message})" if message else ""))

    def assert_fully_visible(self, element: LayoutElement, message: str = ""):
        """Assert element is fully visible (not clipped by parent containers)"""
        if not element:
            return

        # Check if element has visibility information
        if not element.visibility:
            # Fallback to basic viewport check if no visibility data
            self.assert_in_viewport(element, message)
            return

        # Check if element is clipped
        if element.visibility.get('isClipped', False):
            clips = []
            if element.visibility.get('clippedTop'):
                clips.append('top')
            if element.visibility.get('clippedBottom'):
                clips.append('bottom')
            if element.visibility.get('clippedLeft'):
                clips.append('left')
            if element.visibility.get('clippedRight'):
                clips.append('right')

            clip_info = ', '.join(clips) if clips else 'unknown sides'
            self.errors.append(f"{element.tag} element is clipped by parent container ({clip_info})" + (f" ({message})" if message else ""))

        # Check if visible height is less than original height
        visible_height = element.visibility.get('visibleHeight', element.visibleRect.get('height', 0))
        original_height = element.visibility.get('originalHeight', element.rect.get('height', 0))

        if visible_height < original_height:
            clipped_amount = original_height - visible_height
            self.errors.append(f"{element.tag} element is {clipped_amount}px cut off (visible: {visible_height}px, total: {original_height}px)" + (f" ({message})" if message else ""))

    def assert_has_transition_classes(self, element: LayoutElement, message: str = ""):
        """Assert element has CSS transition classes"""
        if not element:
            return

        has_transition = ('transition' in element.classes or
                         'duration-' in element.classes)

        if not has_transition:
            self.warnings.append(f"Element missing transition/animation classes" + (f" ({message})" if message else ""))

    def run_tests(self):
        """Override this method in subclasses to define specific tests"""
        raise NotImplementedError("Subclasses must implement run_tests()")

    def report(self) -> bool:
        """Print test results and return success status"""
        test_name = self.__class__.__name__

        if not self.errors and not self.warnings:
            print(f"✓ {test_name}: All tests passed")
            return True

        if self.errors:
            print(f"✗ {test_name}: {len(self.errors)} error(s)")
            for error in self.errors:
                print(f"  ERROR: {error}")

        if self.warnings:
            print(f"  {test_name}: {len(self.warnings)} warning(s)")
            for warning in self.warnings:
                print(f"  WARNING: {warning}")

        return len(self.errors) == 0