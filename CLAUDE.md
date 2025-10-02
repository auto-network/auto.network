# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Structure

The solution consists of three projects:
- **AutoWeb** - Blazor WebAssembly frontend with TailwindCSS (see [claude/AutoWeb.md](claude/AutoWeb.md))
- **AutoHost** - ASP.NET Core backend API with SQLite persistence (see [claude/AutoHost.md](claude/AutoHost.md))
- **AutoHost.Tests** - Unit tests for AutoHost API

## Tech Stack
- **Framework**: .NET 9.0
- **Frontend**: Blazor WebAssembly
- **Backend**: ASP.NET Core Web API
- **Database**: SQLite with Entity Framework Core
- **CSS**: TailwindCSS v4.1.13
- **API Integration**: OpenRouter API with x-ai/grok-4-fast:free model
- **API Client**: NSwag auto-generated from OpenAPI/Swagger
- **UI Testing**: Playwright for automated screenshots and interaction testing

## Quick Start

### Development (F5 Experience)
```bash
# VSCode: Press F5 to start both AutoHost and AutoWeb
# Or manually:
cd /home/jeremy/auto/AutoWeb && npm run dev
```

### Testing
```bash
# Unit tests
cd /home/jeremy/auto/AutoHost.Tests && dotnet test

# UI screenshot tests
python /home/jeremy/auto/claude/ui/capture-screenshots.py

# Capture specific page
python /home/jeremy/auto/claude/ui/capture-screenshots.py --page Auth.razor
```

### Build
```bash
dotnet build /home/jeremy/auto/auto.sln
```

## Architecture Overview

This is a modern web application with clean separation of concerns:

1. **AutoHost** (Backend) - Provides authentication and data persistence
   - Runs on http://localhost:5050
   - SQLite database for user accounts and API keys
   - Session-based auth with SHA256 token hashing
   - Swagger documentation at /swagger

2. **AutoWeb** (Frontend) - Blazor WebAssembly SPA
   - Terminal-inspired UI with TailwindCSS
   - Direct integration with OpenRouter for chat
   - Strongly-typed client auto-generated from AutoHost API
   - Email-first authentication flow

3. **Client Generation** - Automatic API contract enforcement
   - NSwag generates client on every build
   - Ensures frontend/backend stay in sync
   - Full IntelliSense and type safety

## Development Workflow

1. Press F5 in VSCode to start everything
2. AutoHost starts on port 5050
3. AutoWeb starts with hot reload for both C# and CSS
4. API client regenerates automatically when you build

## Important Notes

- AutoHost must be running for AutoWeb authentication to work
- API keys are persisted in SQLite, not browser storage
- Multiple concurrent sessions per user are supported
- Database is created at `/home/jeremy/auto/AutoHost/autohost.db`
- The OpenRouter model ID is: `x-ai/grok-4-fast:free`

## Knowledge Map - CRITICAL FOR FINDING AND SAVING INFORMATION

### Where to FIND Existing Knowledge

**Project Documentation:**
- `CLAUDE.md` (this file) - Main entry point, project overview
- `claude/AutoHost.md` - Backend API documentation
- `claude/AutoWeb.md` - Frontend documentation

**Methodology & Process:**
- `claude/Method.md` - General debugging methodology and practices
- `claude/Reminder.md` - Critical debugging reminders (what to do)
- `claude/Stupid.md` - Monument to mistakes (what NOT to do, anti-patterns)

**UI Testing System:**
- `claude/UI.md` - UI testing framework, system architecture, lifecycle
- `claude/UI_PRESERVATION.md` - Guidelines for preserving UI features
- `claude/ui/METHOD.md` - UI testing methodology
- `claude/ui/{PageName}/{PageName}.md` - Page specifications
- `claude/ui/{PageName}/{PageName}.test.md` - Test expectations

### Where to SAVE New Knowledge

**Decision Tree for Documentation:**

1. **Is it about a specific project component?**
   - AutoHost backend → `claude/AutoHost.md`
   - AutoWeb frontend → `claude/AutoWeb.md`
   - UI testing system → `claude/UI.md`

2. **Is it a methodology or best practice?**
   - General debugging → `claude/Method.md`
   - UI testing approach → `claude/ui/METHOD.md`

3. **Is it a mistake or anti-pattern to avoid?**
   - Add to `claude/Stupid.md` with a new monument section

4. **Is it a critical reminder about debugging?**
   - Add to `claude/Reminder.md`

5. **Is it about a specific UI page?**
   - Page spec → `claude/ui/{PageName}/{PageName}.md`
   - Test expectations → `claude/ui/{PageName}/{PageName}.test.md`

### IMPORTANT: Before Adding Documentation
- Check if it already exists in the relevant file
- Don't duplicate information across files
- Keep documentation in the most specific relevant location
- Link between documents rather than duplicating

## Development Guidelines

### Debugging - CRITICAL
- **ALWAYS** follow systematic debugging approach (see [claude/Reminder.md](claude/Reminder.md))
- **NEVER** make changes without evidence of the actual problem
- **ALWAYS** verify assumptions before implementing fixes (see [claude/Method.md](claude/Method.md))
- Test the simplest case first before creating complex solutions
- Check actual error messages and system state before theorizing

### Core Debugging Principle (from claude/Reminder.md)
**EVIDENCE BEFORE ACTION**: Before changing ANY code, answer: "What evidence do I have that THIS is the actual problem?"
- List ALL possible causes
- Gather evidence systematically (easiest checks first)
- One hypothesis at a time
- Never claim success without verification

### UI Testing
- UI changes should be validated with screenshot captures
- Test specifications documented in `claude/ui/{PageName}/{PageName}.md`
- Screenshot definitions in `claude/ui/{PageName}/{PageName}.json`
- See [claude/UI.md](claude/UI.md) for complete UI testing framework

### Server Configuration
- Use `--urls` parameter when starting dotnet servers to override launchSettings.json
- Dedicated test ports: AutoHost=6050, AutoWeb=6100 for screenshot capture