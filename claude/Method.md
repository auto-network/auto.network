# Debugging Method for Claude

## Core Principle: PROVE, Don't ASSUME

### 1. When Something Fails
**STOP** and gather evidence before theorizing:
- Read the ACTUAL error message carefully
- Check what's ACTUALLY running (`ps`, `lsof`, `curl`)
- Verify the ACTUAL state (file contents, port bindings, process output)
- Test the simplest case first

**DON'T**:
- Assume timing issues without proof
- Assume process behavior without checking
- Create elaborate theories without evidence
- Write complex fixes for simple problems

### 2. Problem-Solving Order
1. **Observe** - What exactly is happening?
2. **Verify** - Can I reproduce it?
3. **Isolate** - What's the minimal test case?
4. **Prove** - What does debugging show?
5. **Fix** - Make the smallest change that works

### 3. Before Writing Code
**ALWAYS**:
- Understand what the existing code actually does
- Test your assumptions with simple commands
- Plan the change before implementing
- Consider if there's a simpler approach

**NEVER**:
- Add delays/sleeps as a first solution
- Write code to fix an unverified assumption
- Make multiple changes at once
- Ignore existing patterns in the codebase

### 4. Common Traps to Avoid
- **"It must be a timing issue"** - Usually it's not. Check configuration first.
- **"The process is dying"** - Check if it ever started correctly.
- **"It worked before"** - Check what actually changed.
- **"This should work"** - Test that it actually works.

### 5. Debugging Checklist
When a service/server issue occurs:
- [ ] Is it actually running? (`ps aux | grep`)
- [ ] On the right port? (`lsof -i :PORT`)
- [ ] Accessible? (`curl http://localhost:PORT`)
- [ ] Using the right config? (check env vars vs config files)
- [ ] What does the actual output say? (not what I think it says)

### 6. The Right Mental Model
Think like a detective, not a fortune teller:
- Follow evidence, not hunches
- Test hypotheses, don't assume them
- Validate each step before moving forward
- When stuck, go back to what you KNOW works

### 7. Code Organization for Blazor Components
When creating or modifying Razor components:
- **ALWAYS** use code-behind files (`.razor.cs`) for C# logic
- **NEVER** mix C# code blocks with HTML in `.razor` files
- Keep `.razor` files focused on markup and binding
- Place all logic, event handlers, and properties in the code-behind

Example structure:
- `Auth.razor` - Contains only HTML markup and Blazor directives
- `Auth.razor.cs` - Contains the partial class with all C# code

**CRITICAL**: When refactoring to code-behind:
- **PRESERVE** all UI transitions, animations, and behaviors
- **CHECK** `/claude/ui/{PageName}/CRITICAL_FEATURES.md` before changing
- **NEVER** simplify or remove features during refactoring
- See `/claude/UI_PRESERVATION.md` for detailed guidelines

### 8. Common Operations

#### Regenerating NSwag Client
When you make API changes to AutoHost controllers, the client needs to be regenerated:
```bash
/home/jeremy/auto/regenerate-client.sh
```
This script:
1. Kills existing AutoHost instances
2. Builds AutoHost with latest changes
3. Starts AutoHost temporarily to serve swagger
4. Forces NSwag to regenerate the client
5. Cleans up

**ALWAYS use this script when API contracts change!**

### 9. Version Control After Each Sprint Phase

After completing any significant work (feature, refactor, test suite), create a detailed commit:

#### Step 1: Review Changes
```bash
git status        # See what changed
git diff          # Review all modifications
```

#### Step 2: Prepare Detailed Commit Message
Follow this structure:
```
Brief summary (imperative: "Add X", "Fix Y", "Refactor Z")

## Changes
- Bullet points organized by category
- Feature additions
- Bug fixes
- Refactoring work
- Test coverage

## Technical Details
- Architecture decisions made
- Implementation patterns used
- Dependencies added/changed

## Testing
- Tests added/modified
- Coverage achieved
- Verification performed
```

#### Step 3: Create and Verify Signed Commit
```bash
# Stage changes
git add .

# Create commit with detailed message
git commit -m "$(cat <<'EOF'
Your detailed commit message here

## Changes
...

## Testing
...
EOF
)"

# Verify GPG signature
git log --show-signature -1

# Push to GitHub
git push origin main
```

#### Why This Matters
- **Detailed commits** serve as project documentation
- **GPG signatures** prove authenticity and prevent tampering
- **Regular commits** create restore points and track progress
- **Comprehensive messages** help future debugging and understanding

#### Critical Rules
- ❌ NEVER include "Co-Authored-By: Claude" or AI attribution
- ✅ ALWAYS verify GPG signature before pushing
- ✅ ALWAYS write comprehensive commit messages (see initial commit db83a47)
- ✅ ALWAYS commit after completing a sprint phase (feature/refactor/tests)

### Remember
The bug is usually simpler than you think. The fix is usually smaller than you imagine. The problem is usually exactly what the error message says it is.

Every completed sprint phase deserves a detailed, signed commit to GitHub.