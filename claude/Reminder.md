# CRITICAL DEBUGGING REMINDER

## 🛑 STOP FLAILING. START THINKING.

### The Problem
When something isn't working and there are multiple possible causes, the WRONG approach is to:
- Randomly try fixes
- Make assumptions about the cause
- Change multiple things at once
- Claim success without verification
- Jump to conclusions

### The Right Approach: SYSTEMATIC EVIDENCE GATHERING

When faced with a long list of potential causes, **ALWAYS** follow this approach:

## 1. List ALL Possible Causes
Before doing ANYTHING, write down every possible reason for the observed behavior. Don't assume connections between symptoms and causes.

## 2. Gather Evidence Systematically

### Order of Investigation (Easiest → Hardest):

1. **Check existing output/logs first**
   - Look at error messages
   - Check console output
   - Review test execution logs
   - **Cost: 0 seconds, often reveals the answer**

2. **Verify assumptions**
   - Is the service running?
   - Are prerequisites met?
   - What does the actual output show?
   - **Cost: Seconds, eliminates basic issues**

3. **Test manually**
   - Reproduce the exact scenario by hand
   - If it works manually → problem is in automation
   - If it fails manually → problem is in implementation
   - **Cost: 1-2 minutes, cuts problem space in half**

4. **Add diagnostic output**
   - Log at key decision points
   - Print variable values
   - Show which code path was taken
   - **Cost: Minutes, provides definitive answers**

5. **Binary search the problem**
   - Start at the beginning: "Did X happen?"
   - Then: "Did Y happen after X?"
   - Each answer eliminates half the possibilities
   - **Cost: Systematic, guarantees finding the issue**

## 3. The Golden Rules

### ✅ DO:
- **One hypothesis at a time**
- **Verify each step before moving on**
- **Look at actual output, not expected output**
- **Read error messages completely**
- **Test the simplest case first**

### ❌ DON'T:
- **Change code before understanding the problem**
- **Assume you know the cause**
- **Skip steps because they seem "obvious"**
- **Make multiple changes at once**
- **Claim success without verification**

## 4. Evidence Before Action

**NEVER** start fixing until you know:
- What actually happened (not what should have happened)
- Where in the flow it went wrong
- Why it went wrong at that specific point
- How to verify the fix worked

## 5. Common Traps

### The "It Must Be X" Trap
**Symptom**: "The password page doesn't show, so transitions are broken"
**Reality**: Could be 8+ different causes, most unrelated to transitions

### The "I Fixed It" Trap
**Symptom**: "I added the transition classes back"
**Reality**: Never verified it actually works now

### The "Complex Solution" Trap
**Symptom**: "Let me rewrite this whole system"
**Reality**: Problem was a typo or missing service

## 6. The Verification Chain

After ANY change:
1. Did the change compile/build? ✓
2. Does the manual test pass? ✓
3. Does the automated test pass? ✓
4. Do ALL related features still work? ✓

If any ✓ is missing, YOU ARE NOT DONE.

## 7. When Stuck

If you find yourself:
- Making the same change repeatedly
- Changing random things
- Getting frustrated
- Making assumptions

**STOP** and return to Step 1: List all possible causes, then systematically gather evidence.

## Remember

> "The bug is usually simpler than you think. The fix is usually smaller than you imagine. The problem is usually exactly what the error message says it is."

But you won't know which simple thing it is without **SYSTEMATIC EVIDENCE GATHERING**.

## The One Question That Matters

Before changing ANY code, ask yourself:

**"What evidence do I have that THIS is the actual problem?"**

If the answer is "none" or "I think..." or "it seems like..." - STOP and gather evidence first.

---

## CRITICAL TEST/DEBUG ANTI-PATTERNS

### 1. ❌ STUPID TIMEOUT STRATEGY
**Problem**: Adding random long timeouts (1000ms, 3000ms, 5000ms) "just in case"
- Local Blazor with mocked services runs in **single-digit milliseconds**
- Waits should be **specific** (wait for element state, not arbitrary time)
- Adding timeouts when debugging, then **leaving them in** after the real issue is found elsewhere
- This wastes time on every test run and slows the debug loop

**Fix**:
- Use **state-based waits**: `WaitForSelectorAsync()`, `WaitForAsync()` with specific conditions
- Only use fixed timeouts for actual UI animations (50-100ms max for local operations)
- **Remove timeout debugging artifacts** once you find the real issue
- If you need to wait, know WHY and how long is actually needed

### 2. ❌ RUNNING TESTS REPEATEDLY FOR PARTIAL OUTPUT
**Problem**: Running full test → grep for one piece → run again → grep for another piece
- Each test run takes 10-40 seconds
- You waste time waiting when the information already exists

**Fix**:
- **Log everything comprehensively upfront**
- Save full test output to a file
- Grep the saved output for different pieces
- Only re-run when you've actually **changed the code**
- Example: `dotnet test > test-output.txt 2>&1` then `grep "pattern" test-output.txt`

### 3. ❌ DEBUGGING BACKWARDS (FIXING BEFORE PROVING)
**Problem**: When automation fails (e.g., button click doesn't work):
- ❌ Start by changing application code
- ❌ Try different click methods randomly
- ❌ Add waits and force flags
- ❌ Modify the component

**Reality**: **The automation itself might be broken, not the app!**

**Fix - Debug in FORWARD order**:
1. **FIRST**: Prove the element exists and has correct state
   - Is the button visible?
   - Is it enabled?
   - Does it have the expected text?
2. **SECOND**: Prove the automation action worked
   - Did the click actually fire?
   - Did any DOM state change?
   - Are there console errors?
3. **THIRD**: Check application logic
   - Was the event handler called?
   - Did it complete successfully?
   - What was the result?

**Example of CORRECT order**:
```
✓ Button exists
✓ Button is enabled
✓ Button text is "Create"
✓ Click executed (no errors)
❌ Page didn't change
→ NOW check: Was event handler bound? Was it called? Did it error?
```

**Example of WRONG order** (what you did):
```
❌ Click doesn't work
→ Immediately start changing application code (LoadAuthenticationMethods)
→ Add random waits
→ Try force click
→ Never proved the click actually fired
```

### 4. ❌ IGNORING THE SCREENSHOT
**Problem**: UI test fails and creates a screenshot, but you:
- ❌ Run the test multiple times looking for different log output
- ❌ Add debugging, change code, modify waits
- ❌ Grep through logs looking for clues
- ❌ **Never actually look at the screenshot**

**Reality**: **The screenshot shows you EXACTLY what's wrong!**

**The Rule**:
**WHEN A UI TEST FAILS, LOOK AT THE SCREENSHOT FIRST. NOT LAST. FIRST.**

**Why this matters**:
- Logs can lie (automation says "✓ filled" but screenshot shows empty fields)
- Logs show what you THINK happened, screenshot shows what ACTUALLY happened
- One glance at screenshot reveals issues that would take 30 minutes of log analysis
- Screenshots show visual state that logs can't capture (disabled buttons, wrong colors, layout issues)

**Example of CORRECT debugging**:
```
1. Test fails
2. IMMEDIATELY open screenshot
3. Compare screenshot to expected state
4. Now you know what's actually wrong
5. THEN look at logs to understand why
```

**Example of WRONG debugging** (what you did):
```
1. Test fails
2. Grep logs for error messages
3. Run test again with more logging
4. Add timeouts and force clicks
5. Modify application code
6. Run test 5 more times
... 30 minutes later ...
7. Finally look at screenshot
8. Realize fields were never filled (would have been obvious in step 2)
```

**Special case - Lying logs**:
Just because your test logs "✓ Password fields filled" doesn't mean they're actually filled!
- `FillAsync()` can succeed but Blazor binding might not update
- `IsDisabledAsync()` might return wrong value
- **VERIFY VISUALLY** with screenshot or actual DOM inspection
- Don't trust "success" messages without visual confirmation

---

**This approach is not optional. It is not a suggestion. It is THE way to debug.**