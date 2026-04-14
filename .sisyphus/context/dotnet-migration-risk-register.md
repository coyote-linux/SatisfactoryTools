# .NET Migration Risk Register

## Purpose

Track the highest-risk migration concerns so future sessions do not rediscover them.

## Open Risks

| ID | Risk | Impact | Likelihood | Mitigation | Phase |
|---|---|---|---|---|---|
| R001 | Route/version behavior drifts during host takeover | High | Medium | Lock route/version behavior from `AppModule.ts` before M1 and validate via route parity matrix | M0-M2 |
| R002 | Planner request/result parity drifts during C# port | High | High | Capture representative fixtures before porting and gate M3 on parity suite | M0-M4 |
| R003 | Graph/result shaping changes user-visible planner outputs | High | High | Treat `ProductionResultFactory.ts`, `Graph.ts`, and `ProductionResult.ts` as parity-critical domain code | M3-M5 |
| R004 | Local storage and share URL compatibility breaks at planner cutover | High | Medium | Preserve storage keys and share URL format until a later explicit migration | M4-M5 |
| R005 | Database work starts too early and masks runtime migration issues | High | Medium | Keep JSON canonical until runtime/web migration is stable | M4-M8 |
| R006 | Admin scope expands into a second large rewrite | Medium | High | Keep first admin release limited to auth, import, validate, diff, publish, audit | M7-M8 |
| R007 | Graph rendering interop is harder in Blazor than expected | Medium | Medium | Keep graph library replacement out of early phases; prefer interop first | M4-M6 |
| R008 | Deployment changes are mixed with domain changes, making rollback unclear | High | Medium | Keep host slices separate from planner-domain slices and require green checkpoints | M1-M5 |

## Usage Rules

1. Add new risks when they are first discovered.
2. Update mitigation status in session handoffs.
3. Do not mark a risk resolved unless its mitigation has been validated.
