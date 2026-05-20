# Rogue Hero 5

**Five moves. One boss. No filler.**

Rogue Hero 5 is a Unity 6.4 Update prototype for a fast 3D roguelite arena fighter built around short 1v1 boss duels. The player carries a tiny move kit, wins by reading boss telegraphs and landing high-impact punish windows, then upgrades, mutates, or swaps moves between fights.

The project should stay ability-driven and data-driven. The goal is not to build a full Soulslike combo simulator; the move system is the product.

## Current Status

This repository currently contains an extracted Unity skeleton in [`RogueHero5-skeleton/RogueHero5`](RogueHero5-skeleton/RogueHero5). It is not a finished playable game yet.

The first milestone is a Unity-openable, testable combat foundation with enough runtime code, validation, and tests to catch regressions like "dodge does nothing" before manual playtesting.

Unity target:

- Unity `6000.4.7f1` / Unity 6.4 Update
- C#
- Rendering dependencies kept minimal for the skeleton
- URP, Cinemachine, Input System, and Unity performance tooling can be added after the foundation imports cleanly
- Unity 6.3 LTS remains the fallback only if a 6.4-specific blocker appears

## Game Pillars

- Short boss duels, usually 45-120 seconds.
- One computer-controlled arena boss at a time.
- Five move identity: Primary, Secondary, Mobility, Defensive, Ultimate.
- Small loadouts where each move is distinct, flashy, and mechanically meaningful.
- Readable boss telegraphs and clear punish windows.
- Low-poly or stylized visuals with heavy VFX polish once the combat loop works.
- Manual playtesting for feel, automated tests for breakage.

## First Vertical Slice

Build a narrow three-fight mini-run before scaling content:

- one arena
- one player actor
- one boss actor
- five player move definitions
- three boss attacks
- one reward screen
- ten move upgrades
- one three-fight run using boss variants or phase changes

## Starter Player Moves

- **Forward Dodge** - mobility plus invulnerability frames.
- **Spear Dash** - line attack plus movement.
- **Counter Burst** - defensive timing window.
- **Void Spear** - ranged punish move.
- **Meteor Kick** - leap and slam burst.

Every move should have a measurable contract:

- command/input received
- equipped slot resolves to the move
- state transition occurs
- cooldown begins and eventually resets
- startup, active, and recovery timing are honored
- movement, hitbox, projectile, VFX, or other effect occurs
- invulnerability, counter, or damage windows work when configured
- spawned combat objects clean up after completion

## First Boss

The first boss is **The Duelist**.

Purpose: teach timing, spacing, and punish windows.

Attacks:

- red-line dash slash
- close-range three-hit pattern
- delayed arena-crossing lunge

Phase 2:

- shorter recovery windows
- occasional feint before dash slash

## Current Skeleton Contents

The extracted Unity project already includes the early code and documentation scaffolding:

- runtime combat primitives:
  - `Health`
  - `InvulnerabilityWindow`
  - `DamageEvent`
  - `ActorState`
- move-system primitives:
  - `MoveDefinition`
  - `MoveRuntimeState`
  - `MoveRunner`
  - `MoveSlot`
- telemetry primitives:
  - `RuntimePerfCounters`
  - `DebugOverlay`
- editor validation:
  - `Rogue Hero 5 > Validation > Validate Move Definitions`
- EditMode and PlayMode tests for cooldowns, damage, invulnerability, move validation, and dodge behavior
- design, testing, performance, DevOps, playtest, and AI-agent handoff docs in [`RogueHero5-skeleton/RogueHero5/docs`](RogueHero5-skeleton/RogueHero5/docs)

## Repository Layout

- [`README.md`](README.md) - project-level overview.
- [`RogueHero5-skeleton/RogueHero5`](RogueHero5-skeleton/RogueHero5) - Unity project skeleton to open in Unity Hub.
- [`RogueHero5-skeleton/RogueHero5/AGENTS.md`](RogueHero5-skeleton/RogueHero5/AGENTS.md) - agent working rules for future code changes.
- [`RogueHero5-skeleton/RogueHero5/docs/game_design_brief.md`](RogueHero5-skeleton/RogueHero5/docs/game_design_brief.md) - source design brief.
- [`RogueHero5-skeleton/RogueHero5/docs/testing_strategy.md`](RogueHero5-skeleton/RogueHero5/docs/testing_strategy.md) - test philosophy and target coverage.
- [`RogueHero5-skeleton/RogueHero5/docs/performance_plan.md`](RogueHero5-skeleton/RogueHero5/docs/performance_plan.md) - early budgets and profiling plan.
- [`RogueHero5-skeleton/RogueHero5/docs/unity_6_4_notes.md`](RogueHero5-skeleton/RogueHero5/docs/unity_6_4_notes.md) - Unity 6.4 upgrade notes, opportunities, and risks.
- [`RogueHero5-skeleton/RogueHero5/docs/devops_plan.md`](RogueHero5-skeleton/RogueHero5/docs/devops_plan.md) - local, PR, and CI workflow.
- [`RogueHero5-skeleton/RogueHero5/docs/manual_playtest_report_template.md`](RogueHero5-skeleton/RogueHero5/docs/manual_playtest_report_template.md) - template for turning manual feedback into tasks.

## Quick Start

1. Open [`RogueHero5-skeleton/RogueHero5`](RogueHero5-skeleton/RogueHero5) in Unity Hub with Unity `6000.4.7f1` or another compatible Unity `6000.4.x` editor.
2. Let Unity generate `Library/`, `.meta` files, and any missing local project state.
3. Run EditMode and PlayMode tests from the Unity Test Runner.
4. Use the manual playtest template once a playable scene exists.
5. Commit Unity-generated `.meta` files after the first successful import.

Windows command-line test example, after adjusting the Unity path if needed:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.4.7f1\Editor\Unity.exe" `
  -batchmode `
  -projectPath "E:\Storage\SAAS\Rogue-Hero-5\RogueHero5-skeleton\RogueHero5" `
  -runTests `
  -testPlatform EditMode `
  -testResults test-results-editmode.xml `
  -quit
```

Run PlayMode tests by replacing `EditMode` with `PlayMode`.

## Development Loop

```text
Create task or capture manual complaint
AI writes or updates tests
AI implements runtime/editor code
Run Unity tests locally
Manual playtest with debug overlay
Convert feedback into a test, replay, tuning note, or profiler capture
Commit
```

Every PR should report:

- goal
- acceptance criteria
- tests added or changed
- test commands run
- manual Unity steps
- performance risks

## Testing Strategy

Every fun mechanic needs a measurable contract. Manual playtesting decides whether the combat is fun; automated tests catch early breakage.

Use EditMode tests for pure rules and validation:

- cooldown logic
- damage logic
- invulnerability windows
- upgrade legality
- reward table validation
- deterministic boss decision scoring

Use PlayMode tests when frame updates, Unity objects, movement, physics, or timing matter:

- dodge actually moves the actor
- i-frames block damage during the active window
- hitboxes damage targets once
- projectiles despawn
- fights enter victory or defeat state
- reward screen appears after boss death

## Performance Targets

Early PC prototype budgets:

- target 60 FPS
- median frame time at or below 16.67 ms
- P95 frame time warning above 22 ms
- worst combat hitch warning above 50 ms
- zero combat instantiates after warmup
- zero pool misses after warmup
- no recurring GC allocations in core combat

Track projectiles, hitboxes, VFX, pool misses, combat instantiates, boss decision time, hitbox resolution time, physics overlap calls, and per-frame GC allocations as the game systems appear.

## Agent Working Agreements

Future AI/code-agent work should follow the extracted [`AGENTS.md`](RogueHero5-skeleton/RogueHero5/AGENTS.md):

- add or update tests before changing gameplay behavior
- prefer small, testable runtime systems over scene-driven behavior
- avoid direct `.unity`, `.prefab`, `.mat`, `.asset`, or `.meta` edits unless explicitly requested
- keep gameplay code in `Assets/RogueHero5/Runtime`
- keep editor tooling in `Assets/RogueHero5/Editor`
- keep tests in `Assets/RogueHero5/Tests`
- prefer ScriptableObject definitions for moves, bosses, upgrades, and reward tables
- avoid per-frame allocations and broad scene searches in combat loops

## Next Build Priorities

1. Open the skeleton in Unity and verify it compiles.
2. Confirm runtime, editor, EditMode test, and PlayMode test assemblies are separated cleanly.
3. Strengthen the existing dodge/cooldown/invulnerability tests.
4. Add reusable test helpers such as `TestMoveFactory` or `TestMoveLibrary`.
5. Build the five starter move definitions as data.
6. Implement The Duelist with three readable attacks and a phase-2 variant.
7. Add the first reward screen and ten move upgrades.
8. Turn the result into a three-fight mini-run before adding broader roguelite systems.
