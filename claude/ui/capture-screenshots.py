#!/usr/bin/env python3
import json
import subprocess
import time
import os
import sys
import argparse
import requests

script_start = time.time()

# Parse arguments
parser = argparse.ArgumentParser(description='Capture screenshots for UI testing')
parser.add_argument('--page', help='Specific page to capture (e.g., Auth.razor)', default=None)
parser.add_argument('--test-only', action='store_true', help='Run tests only without capturing screenshots')
args = parser.parse_args()

# Get script directory
script_dir = os.path.dirname(os.path.abspath(__file__))

# If test-only mode, skip to tests
if args.test_only:
    # Determine which pages to test
    if args.page:
        page_dirs = [args.page] if os.path.exists(os.path.join(script_dir, args.page)) else []
    else:
        # Find all page directories
        page_dirs = []
        for item in os.listdir(script_dir):
            item_path = os.path.join(script_dir, item)
            if os.path.isdir(item_path):
                json_file = os.path.join(item_path, f"{item}.json")
                if os.path.exists(json_file):
                    page_dirs.append(item)

    # Run tests
    print("Running layout validation tests...")
    all_tests_passed = True

    for page_dir in page_dirs:
        test_script = os.path.join(script_dir, page_dir, f"{page_dir}.test.py")
        if os.path.exists(test_script):
            print(f"\nTesting {page_dir}:")
            test_result = subprocess.run([sys.executable, test_script], capture_output=True, text=True)
            print(test_result.stdout, end='')

            if test_result.returncode != 0:
                all_tests_passed = False
                if test_result.stderr:
                    print(test_result.stderr)

    if all_tests_passed:
        print("\n✓ All layout tests passed!")
        sys.exit(0)
    else:
        print("\n✗ Some layout tests failed")
        sys.exit(1)

# Define dedicated ports for screenshot capture
AUTOHOST_PORT = 6050
AUTOWEB_PORT = 6100

print("Starting dedicated servers for screenshot capture...")

# Kill any existing processes on our dedicated ports
subprocess.run(f"lsof -ti:{AUTOHOST_PORT} | xargs kill -9 2>/dev/null || true", shell=True)
subprocess.run(f"lsof -ti:{AUTOWEB_PORT} | xargs kill -9 2>/dev/null || true", shell=True)
time.sleep(2)

# Build CSS FIRST before starting any servers
print("Building latest CSS...")
os.chdir("/home/jeremy/auto/AutoWeb")
subprocess.run(["npx", "@tailwindcss/cli", "-i", "./wwwroot/css/input.css", "-o", "./wwwroot/css/app.css"])

# Clean up any existing test database for fresh start
test_db_path = os.path.join(script_dir, "test_database.db")
if os.path.exists(test_db_path):
    os.remove(test_db_path)
    print("Removed existing test database for fresh start")

# Start AutoHost on dedicated port with isolated test database
print(f"Starting AutoHost on port {AUTOHOST_PORT}...")
os.chdir("/home/jeremy/auto/AutoHost")
autohost_proc = subprocess.Popen(
    ["dotnet", "run", "--no-launch-profile", "--urls", f"http://localhost:{AUTOHOST_PORT}", "--", f"--DatabasePath={test_db_path}"],
    stdout=subprocess.DEVNULL,
    stderr=subprocess.DEVNULL
)

# Start AutoWeb on dedicated port with correct AutoHost URL
print(f"Starting AutoWeb on port {AUTOWEB_PORT}...")
os.chdir("/home/jeremy/auto/AutoWeb")
env = os.environ.copy()
env["AUTOHOST_URL"] = f"http://localhost:{AUTOHOST_PORT}"
autoweb_proc = subprocess.Popen(
    ["dotnet", "run", "--urls", f"http://localhost:{AUTOWEB_PORT}"],
    stdout=subprocess.DEVNULL,
    stderr=subprocess.DEVNULL,
    env=env
)

# Wait for servers to start by polling with requests
print("Waiting for servers to start...")
max_wait = 15  # Reduced from 30
start_time = time.time()
autoweb_ready = False
autohost_ready = False
last_status_time = 0

while time.time() - start_time < max_wait:
    # Check AutoWeb
    if not autoweb_ready:
        try:
            response = requests.get(f"http://localhost:{AUTOWEB_PORT}/", timeout=1)
            if response.status_code == 200:
                print(f"  AutoWeb ready")
                autoweb_ready = True
        except:
            pass

    # Check AutoHost
    if not autohost_ready:
        try:
            response = requests.get(f"http://localhost:{AUTOHOST_PORT}/", timeout=1)
            # AutoHost might return various status codes when ready
            print(f"  AutoHost ready (status: {response.status_code})")
            autohost_ready = True
        except requests.exceptions.RequestException:
            pass  # Still starting

    if autoweb_ready and autohost_ready:
        break

    # Print status every 5 seconds
    current_time = time.time()
    if current_time - last_status_time > 5:
        elapsed = int(current_time - start_time)
        print(f"  Still waiting... ({elapsed}s elapsed) - AutoWeb: {autoweb_ready}, AutoHost: {autohost_ready}")
        last_status_time = current_time

    time.sleep(0.2)

if not autoweb_ready or not autohost_ready:
    print("Warning: Servers may not be fully ready")

print(f"Server startup took {time.time() - start_time:.1f}s")

# Verify servers are actually accessible
print("Verifying servers are accessible...")
try:
    response = requests.get(f"http://localhost:{AUTOWEB_PORT}/", timeout=2)
    print(f"  AutoWeb HTTP status: {response.status_code}")
except:
    print(f"  AutoWeb HTTP status: ERROR")

try:
    response = requests.get(f"http://localhost:{AUTOHOST_PORT}/", timeout=2)
    print(f"  AutoHost HTTP status: {response.status_code}")
except:
    print(f"  AutoHost HTTP status: ERROR")

# Determine which pages to capture
if args.page:
    # Single page mode
    page_dirs = [args.page] if os.path.exists(os.path.join(script_dir, args.page)) else []
    if not page_dirs:
        print(f"Error: Page directory '{args.page}' not found")
        sys.exit(1)
else:
    # Find all page directories (those containing a .json file)
    page_dirs = []
    for item in os.listdir(script_dir):
        item_path = os.path.join(script_dir, item)
        if os.path.isdir(item_path):
            json_file = os.path.join(item_path, f"{item}.json")
            if os.path.exists(json_file):
                page_dirs.append(item)

if not page_dirs:
    # Fallback to old screenshots.json if it exists
    if os.path.exists(os.path.join(script_dir, "screenshots.json")):
        with open(os.path.join(script_dir, "screenshots.json"), "r") as f:
            config = json.load(f)
            screenshots_to_capture = config["screenshots"]
            output_dir = script_dir
    else:
        print("No page directories or screenshots.json found")
        sys.exit(1)
else:
    screenshots_to_capture = []
    for page_dir in page_dirs:
        json_file = os.path.join(script_dir, page_dir, f"{page_dir}.json")
        output_dir = os.path.join(script_dir, page_dir, "results")

        # Clean up old results first
        if os.path.exists(output_dir):
            import shutil
            shutil.rmtree(output_dir)
            print(f"Cleaned old results for {page_dir}")

        # Create fresh results directory
        os.makedirs(output_dir, exist_ok=True)

        with open(json_file, "r") as f:
            config = json.load(f)

        # Add output directory to each screenshot config
        for screenshot in config["screenshots"]:
            screenshot["output_dir"] = output_dir
            screenshots_to_capture.append(screenshot)

print("Capturing screenshots...")

# Process each screenshot
for screenshot in screenshots_to_capture:
    name = screenshot["name"]
    url = screenshot.get("url", config.get("baseUrl", "/"))
    description = screenshot["description"]
    actions = screenshot.get("actions", [])
    output_dir = screenshot.get("output_dir", script_dir)

    print(f"Capturing {name}: {description}")

    # Always use playwright script for consistency (even without actions)
    # This ensures we capture LayoutML for all pages
    if True:  # Always run this block
        # Use playwright codegen to perform actions then screenshot
        cmd = [
            "npx", "playwright", "codegen",
            "--target", "javascript",
            "-o", f"/tmp/{name}.js",
            f"http://localhost:{AUTOWEB_PORT}{url}"
        ]

        # Build the script with actions
        script = f"await page.goto('http://localhost:{AUTOWEB_PORT}{url}');\n"

        # Wait for Blazor to be ready
        script += f"await page.waitForFunction(() => window.Blazor !== undefined, {{ timeout: 10000 }});\n"
        script += f"await page.waitForTimeout(1000); // Extra wait for Blazor to fully initialize\n"

        # Process any actions
        for i, action in enumerate(actions):
            if action["type"] == "fill":
                script += f"console.log('[{name}] Filling {action['selector']}');\n"
                script += f"await page.fill('{action['selector']}', '{action['value']}');\n"
            elif action["type"] == "click":
                script += f"console.log('[{name}] Clicking {action['selector']}');\n"
                # Wait for button to be enabled if it's a submit button
                if action['selector'] == 'button[type="submit"]':
                    script += f"await page.waitForTimeout(200); // Wait for Blazor state update\n"
                script += f"await page.click('{action['selector']}');\n"
                # Wait for transitions and state changes after clicks
                script += f"await page.waitForTimeout(500); // Wait for transitions to complete\n"
            elif action["type"] == "wait":
                if "selector" in action:
                    script += f"console.log('[{name}] Waiting for {action['selector']}');\n"
                    timeout = action.get('timeout', 5000)
                    script += f"await page.waitForSelector('{action['selector']}', {{ timeout: {timeout} }});\n"
                elif "ms" in action:
                    script += f"console.log('[{name}] Waiting for {action['ms']}ms');\n"
                    script += f"await page.waitForTimeout({action['ms']});\n"

        # ALWAYS save LayoutML data alongside screenshot (even with no actions)
        layoutml_path = os.path.join(output_dir, f"{name}.json")
        script += f"""
// Capture LayoutML data with overflow clipping detection
const layoutML = await page.evaluate(() => {{
    const viewport = {{
        width: window.innerWidth,
        height: window.innerHeight
    }};

    // Calculate actual visible bounds considering overflow clipping
    function getVisibleBounds(element) {{
        let bounds = element.getBoundingClientRect();
        let originalBounds = {{
            top: bounds.top,
            bottom: bounds.bottom,
            left: bounds.left,
            right: bounds.right,
            width: bounds.width,
            height: bounds.height
        }};

        let parent = element.parentElement;
        let hasClipping = false;

        // Walk up the DOM tree to find clipping containers
        while (parent) {{
            const parentBounds = parent.getBoundingClientRect();
            const parentStyle = window.getComputedStyle(parent);

            if (parentStyle.overflow === 'hidden' ||
                parentStyle.overflowX === 'hidden' ||
                parentStyle.overflowY === 'hidden' ||
                parentStyle.overflow === 'clip') {{

                // Clip bounds to parent
                const newBounds = {{
                    top: Math.max(bounds.top, parentBounds.top),
                    bottom: Math.min(bounds.bottom, parentBounds.bottom),
                    left: Math.max(bounds.left, parentBounds.left),
                    right: Math.min(bounds.right, parentBounds.right)
                }};

                if (newBounds.top !== bounds.top || newBounds.bottom !== bounds.bottom ||
                    newBounds.left !== bounds.left || newBounds.right !== bounds.right) {{
                    hasClipping = true;
                }}

                bounds = newBounds;
            }}
            parent = parent.parentElement;
        }}

        const visibleWidth = Math.max(0, bounds.right - bounds.left);
        const visibleHeight = Math.max(0, bounds.bottom - bounds.top);
        const isVisible = visibleHeight > 0 && visibleWidth > 0;

        return {{
            isVisible: isVisible,
            visibleBounds: bounds,
            originalBounds: originalBounds,
            visibleWidth: visibleWidth,
            visibleHeight: visibleHeight,
            isClipped: hasClipping ||
                       originalBounds.bottom !== bounds.bottom ||
                       originalBounds.top !== bounds.top ||
                       originalBounds.left !== bounds.left ||
                       originalBounds.right !== bounds.right,
            clippedTop: originalBounds.top < bounds.top,
            clippedBottom: originalBounds.bottom > bounds.bottom,
            clippedLeft: originalBounds.left < bounds.left,
            clippedRight: originalBounds.right > bounds.right
        }};
    }}

    // Helper function to check if element is actually visible on screen
    function isElementVisible(el) {{
        const style = window.getComputedStyle(el);

        // Check basic visibility
        if (style.display === 'none' || style.visibility === 'hidden') return false;

        // Check opacity through parent chain
        let currentEl = el;
        while (currentEl) {{
            const currentStyle = window.getComputedStyle(currentEl);
            if (parseFloat(currentStyle.opacity) === 0) return false;
            currentEl = currentEl.parentElement;
        }}

        return true;
    }}

    // Collect ALL elements (including containers without text)
    const elements = [];
    const allElements = document.querySelectorAll('*');

    allElements.forEach(el => {{
        // Skip script, style, and meta elements
        const tag = el.tagName.toLowerCase();
        if (tag === 'script' || tag === 'style' || tag === 'meta' ||
            tag === 'head' || tag === 'html' || tag === 'body') return;

        if (!isElementVisible(el)) return;

        const rect = el.getBoundingClientRect();
        const style = window.getComputedStyle(el);
        const visibilityInfo = getVisibleBounds(el);

        // Skip elements with no visible area
        if (!visibilityInfo.isVisible) return;

        // Get direct text content
        let text = '';
        for (let node of el.childNodes) {{
            if (node.nodeType === 3) {{
                text += node.textContent.trim() + ' ';
            }}
        }}
        text = text.trim();

        // For inputs, get value or placeholder
        if (tag === 'input' || tag === 'textarea') {{
            text = el.value || el.placeholder || text;
        }}

        // Include containers even without text (to track clipping boundaries)
        const isContainer = style.overflow === 'hidden' ||
                          style.overflow === 'clip' ||
                          style.overflowX === 'hidden' ||
                          style.overflowY === 'hidden';

        // Include element if it has text, is an input, or is a clipping container
        if (!text && !el.value && !el.placeholder && !isContainer) {{
            // Skip elements with no content and not containers
            return;
        }}

        elements.push({{
            tag: tag,
            type: el.type || null,
            text: text.substring(0, 100),
            value: el.value || '',
            placeholder: el.placeholder || '',
            rect: {{
                x: Math.round(rect.x),
                y: Math.round(rect.y),
                width: Math.round(rect.width),
                height: Math.round(rect.height)
            }},
            visibleRect: {{
                x: Math.round(visibilityInfo.visibleBounds.left),
                y: Math.round(visibilityInfo.visibleBounds.top),
                width: Math.round(visibilityInfo.visibleWidth),
                height: Math.round(visibilityInfo.visibleHeight)
            }},
            style: {{
                display: style.display,
                opacity: style.opacity,
                visibility: style.visibility,
                position: style.position,
                zIndex: style.zIndex,
                overflow: style.overflow,
                overflowX: style.overflowX,
                overflowY: style.overflowY
            }},
            classes: el.className || '',
            isContainer: isContainer,
            visibility: {{
                isFullyVisible: !visibilityInfo.isClipped,
                isClipped: visibilityInfo.isClipped,
                clippedTop: visibilityInfo.clippedTop,
                clippedBottom: visibilityInfo.clippedBottom,
                clippedLeft: visibilityInfo.clippedLeft,
                clippedRight: visibilityInfo.clippedRight,
                visibleHeight: visibilityInfo.visibleHeight,
                originalHeight: visibilityInfo.originalBounds.height
            }}
        }});
    }});

    return {{
        viewport: viewport,
        elements: elements.filter(e => e.visibleRect.width > 0 && e.visibleRect.height > 0)
    }};
}});

// Save LayoutML to file
const fs = require('fs');
fs.writeFileSync('{layoutml_path}', JSON.stringify(layoutML, null, 2));
console.log('[{name}] LayoutML saved to {layoutml_path}');

// Print summary of visible elements
console.log('[{name}] Visible elements:');
layoutML.elements
    .filter(el => el.text || el.value || el.placeholder)
    .forEach(el => {{
        const content = el.text || el.value || el.placeholder;
        console.log(`  [${{el.rect.x}}, ${{el.rect.y}}] ${{el.tag}} "${{content.substring(0, 30)}}"`);
    }});
"""

        # Take screenshot
        script += f"await page.screenshot({{ path: '{os.path.join(output_dir, name)}.png', fullPage: true }});\n"
        script += f"console.log('[{name}] Screenshot saved');\n"

        # Write script and run it
        with open(f"/tmp/{name}.js", "w") as f:
            f.write(f"""const {{ chromium }} = require('playwright');
(async () => {{
  const browser = await chromium.launch();
  const page = await browser.newPage();
  {script}
  await browser.close();
}})();""")

        # Run the script with NODE_PATH set to find global playwright
        env = os.environ.copy()
        env["NODE_PATH"] = "/home/jeremy/.nvm/versions/node/v22.18.0/lib/node_modules"
        result = subprocess.run(["node", f"/tmp/{name}.js"], cwd=script_dir, env=env, capture_output=True, text=True)

        if result.returncode != 0:
            print(f"  ERROR running {name}.js:")
            print(result.stderr)
        else:
            # Show stdout if there's any (contains our debug logs)
            if result.stdout and result.stdout.strip():
                for line in result.stdout.strip().split('\n'):
                    print(f"    {line}")
            print(f"  Saved to {os.path.join(output_dir, name)}.png")

# Wait a moment for all scripts to complete
time.sleep(0.5)

# Clean up servers
print("Cleaning up servers...")
autohost_proc.terminate()
autoweb_proc.terminate()

# Run page-specific layout tests
print("\nRunning layout validation tests...")
all_tests_passed = True

for page_dir in page_dirs:
    test_script = os.path.join(script_dir, page_dir, f"{page_dir}.test.py")
    if os.path.exists(test_script):
        print(f"\nTesting {page_dir}:")
        test_result = subprocess.run([sys.executable, test_script], capture_output=True, text=True)
        print(test_result.stdout, end='')

        if test_result.returncode != 0:
            all_tests_passed = False
            if test_result.stderr:
                print(test_result.stderr)

if all_tests_passed:
    print("\n✓ All layout tests passed!")
    sys.exit(0)
else:
    print("\n✗ Some layout tests failed")
    sys.exit(1)