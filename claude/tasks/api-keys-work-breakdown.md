# Connections Hub - Work-Task Breakdown

**Sprint**: Connections Hub Implementation (Phase 1 & 2)
**Created**: 2025-10-02
**Updated**: 2025-10-02 (Renamed from "API Keys" to "Connections")

## Work-Task Breakdown

### Phase A: Backend Data Model (Foundational - Test Once)
1. Create ServiceType.cs with ServiceType enum (OpenRouter, OpenAI, Anthropic, Grok)
2. Add ProtocolType enum to same file (OpenAICompatible, AnthropicAPI)
3. Create ServiceRegistry.cs with ServiceDefinition and ProtocolDefinition classes
4. Implement service/protocol mappings in ServiceRegistry for 4 LLM services
5. Add validation methods to ServiceRegistry (IsValidMapping, GetDefaultProtocol, etc.)
6. Update ApiKey.cs model to use enum fields instead of strings
7. Create EF Core migration for new enum columns (stored as int)
8. Apply migration to create columns in SQLite database
9. Verify migration applied successfully and columns exist

### Phase B: Backend API - New Controller (Build + Test Together)
10. Create ConnectionsController.cs with empty class and DI setup
11. Implement GET /api/connections endpoint to list all active connections for user
12. Implement GET /api/connections/registry endpoint to return ServiceRegistry metadata
13. Implement POST /api/connections endpoint with ServiceRegistry validation
14. Implement DELETE /api/connections/{id} endpoint to soft-delete specific connection
15. Create response models (ConnectionInfo, ConnectionsListResponse, ServiceRegistryResponse)
16. Create request models (CreateConnectionRequest, CreateConnectionResponse)

### Phase C: Backend API - Remove Old Endpoints (Cleanup)
17. Remove GetApiKey and SaveApiKey methods from AuthController.cs
18. Verify AuthController compiles and has no API key references

### Phase D: Backend Tests (Test New Controller Comprehensively)
19. Create ConnectionsControllerTests.cs with WebApplicationFactory setup
20. Write list endpoint tests (5 tests: empty, single, multiple, active-only, auth-required)
21. Write registry endpoint tests (3 tests: returns all services, returns all protocols, metadata correct)
22. Write create endpoint tests (8 tests: valid, multiple-active, returns-id, validation, invalid service/protocol mapping, auth)
23. Write delete endpoint tests (5 tests: soft-delete, not-found, wrong-owner, auth)
24. Write integration tests (4 tests: create-list, create-delete, multi-service, roundtrip)
25. Run backend test suite and verify all AutoHost.Tests pass

### Phase E: Frontend Foundation (Regenerate Client + Mock Setup)
26. Regenerate NSwag client to get new ConnectionsController endpoints and enums
27. Add ConnectionHub states to MockStates.ComponentStates dictionary
28. Implement mock ServiceRegistry data generator (returns enum-based service/protocol definitions)
29. Implement mock data generator for each state (no-connections through many-connections-mixed)
30. Implement ConnectionsGetAsync in MockAutoHostClient with state-based enum data
31. Implement ConnectionsGetRegistryAsync in MockAutoHostClient returning service definitions
32. Implement ConnectionsCreateAsync in MockAutoHostClient with registry validation
33. Implement ConnectionsDeleteAsync in MockAutoHostClient with state modification
34. Rename ApiKeysSettings.razor to ConnectionHub.razor
35. Update Settings.razor to use ConnectionHub and change tab text to "Connections"
36. Add ConnectionHub case to TestPage.razor component switch
37. Manually test mock states in browser at /test?component=ConnectionHub&state=X

### Phase F: Frontend Component Refactor (Structural - Preserve Behavior)
38. Create ConnectionHub.razor.cs code-behind file with partial class
39. Move all @code block logic to code-behind file
40. Keep only markup in ConnectionHub.razor file
41. Manually test component still works in Settings page

### Phase G: Frontend Component Logic (Update to Multi-Connection with Registry)
42. Add OnInitializedAsync call to fetch registry from ConnectionsGetRegistryAsync
43. Replace LoadApiKeys to call ConnectionsGetAsync plural endpoint
44. Handle enum-based ServiceType/ProtocolType in ConnectionInfo objects
45. Update UI text throughout: "API Keys" → "Connections", "Add API Key" → "Add Connection"
46. Add ServiceType dropdown populated from registry Services list
47. Add Protocol dropdown filtered by selected service's SupportedProtocols
48. Auto-select default protocol when service selected and only one option
49. Update SaveNewConnection to include selected ServiceType/ProtocolType enums
50. Update DeleteConnection to call ConnectionsDeleteAsync with specific connection ID
51. Add service type badges with color coding based on enum values
52. Remove single-connection assumption logic and hardcoded ID values
53. Manually test all workflows work in browser (add, view, delete multiple connections)

### Phase H: Frontend Unit Tests (Component Logic - Fast Feedback)
54. Create UnitTests.cs with bUnit TestContext setup
55. Write initialization tests (8 tests: load states, registry fetch, service types, sorting, enums)
56. Write add form tests (12 tests: show/hide, service dropdown, protocol dropdown, auto-select, validation)
57. Write save connection tests (10 tests: API calls, enum params, success, errors, multi-connection, validation)
58. Write view connection key tests (4 tests: toggle show/hide, button text)
59. Write delete connection tests (8 tests: specific delete, errors, list updates)
60. Write service type tests (6 tests: badge display, colors, mixed services, enum rendering)
61. Write conditional rendering tests (6 tests: empty state, lists, forms, messages)
62. Run unit tests and verify all 50-60 tests pass

### Phase I: Frontend Render Tests (HTML Structure - Fast Validation)
52. Create RenderTests.cs with bUnit TestContext setup
53. Write HTML structure tests (3 tests: container, cards, form elements)
54. Write CSS class tests (3 tests: buttons, badges, inputs)
55. Write accessibility tests (2 tests: labels, aria attributes)
56. Write service type rendering tests (2 tests: dropdown options, badge elements)
57. Run render tests and verify all 8-10 tests pass

### Phase J: Frontend Layout Tests (Browser Rendering - Visual Validation)
58. Create LayoutTests.cs with Playwright fixture setup
59. Write state rendering tests for all 5 mock states
60. Write element visibility test for connection UI components
61. Run layout tests and verify all 5-6 tests pass

### Phase K: Frontend Interaction Tests (User Workflows - End-to-End)
62. Create InteractionTests.cs with Playwright fixture setup
63. Write add connection workflow tests (4 tests: add with service type, multiple adds, cancel, validation)
64. Write view connection key workflow tests (2 tests: show/hide toggle)
65. Write delete connection workflow tests (3 tests: delete from single, delete from multiple, specific connection)
66. Write complex workflow tests (3 tests: add-view-delete, multiple services, error recovery)
67. Run interaction tests and verify all 10-12 tests pass

### Phase L: Documentation (Capture Knowledge)
68. Create _SPEC.md with component overview and Connections Hub concept
69. Document all 5 mock states and their data structures
70. Document test coverage breakdown by all 4 layers
71. Document backend API integration and service type extensibility
72. Document known limitations and future enhancements

### Phase M: Integration & Verification (Full System Check)
73. Run complete test suite for entire solution (all projects)
74. Verify all 220+ tests pass with no failures
75. Manually test complete workflows in browser with real backend
76. Verify database persistence of multiple connections with service types
77. Test error cases and edge conditions manually
78. Verify "Connections" terminology used consistently throughout UI

### Phase N: Commit & Deploy (Version Control)
79. Stage all changes across all modified files
80. Write detailed commit message documenting Connections Hub implementation
81. Create GPG-signed commit following project standards
82. Push to GitHub and verify "Verified" badge appears

## Task Dependencies

**Sequential Phases** (must complete in order):
- A → B → C → D (backend stack must be complete before frontend)
- D → E (need passing backend before regenerating client)
- E → F → G (mock setup, rename, refactor, then logic changes)
- G → H → I → J → K (logic complete before testing pyramid)

**Parallel Opportunities Within Phases**:
- Phase B tasks 6-8 can be drafted together, tested separately
- Phase D tasks 15-18 can be written in parallel once 14 is done
- Phase E tasks 23-25 can be written in parallel once 22 is done
- Phase H tasks 44-50 can be written in parallel once 43 is done
- Phase I-K test files can be created in parallel once Phase G complete

**Critical Validation Points**:
- After task 9: Database schema correct with enum columns
- After task 16: API contracts defined with registry endpoint
- After task 25: Backend fully tested including registry validation
- After task 37: Mock infrastructure validated with enum support
- After task 53: Component logic working manually with registry-driven UI
- After task 84: Full system integration verified (updated for new task count)

## Estimated Timing by Phase

- Phase A: 3.5 hours (enums + registry + model + migration)
- Phase B: 3.5 hours (new API with registry endpoint)
- Phase C: 30 min (cleanup)
- Phase D: 5 hours (backend tests including registry tests)
- Phase E: 4 hours (mocks + client + registry support)
- Phase F: 2 hours (refactor)
- Phase G: 4 hours (multi-connection logic with registry-driven UI)
- Phase H: 4 hours (unit tests including registry tests)
- Phase I: 1 hour (render tests)
- Phase J: 1.5 hours (layout tests)
- Phase K: 2.5 hours (interaction tests)
- Phase L: 2 hours (docs including registry architecture)
- Phase M: 2.5 hours (verification)
- Phase N: 30 min (commit)

**Total: 32-37 hours** (increased due to service registry infrastructure)
