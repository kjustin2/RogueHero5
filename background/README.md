# Rogue Hero 5

**Five moves. One boss. No filler.**

Rogue Hero 5 is a Unity 6.4 Update prototype for a fast 3D roguelite arena fighter built around short 1v1 boss duels, tiny move kits, flashy ability-driven attacks, strong test coverage, and an AI-assisted code/test/playtest loop.

This repository is intentionally a **bare skeleton**, not a finished Unity project. It gives Codex/Cursor/Claude Code a clean structure, instructions, tests, and first combat primitives so AI agents can start safely without destroying scenes or prefabs.

## Core design

The player fights one computer-controlled arena boss at a time. The player can equip only a few moves, and after victories can upgrade, mutate, or swap moves. The design goal is not a Soulslike combo simulator; it is an ability-duel game where each move has a measurable contract:

- input received
- move starts
- state changes
- cooldown begins
- movement/effect occurs
- invulnerability/damage windows occur
- hitbox/projectile/VFX cleanup occurs

## Unity target

- Unity: **6.4 Update** / `6000.4.x`
- Current local editor target: `6000.4.7f1`
- Unity 6.3 LTS fallback: only if a 6.4-specific blocker appears
- Language: C#
- Rendering target: URP later, but this skeleton keeps rendering dependencies minimal
- Recommended packages to add after first import:
  - Input System
  - Cinemachine
  - Performance Testing package
  - Profile Analyzer
  - Memory Profiler

## First manual steps

1. Unzip this repo skeleton.
2. Open the folder in Unity Hub as a Unity 6.4 project.
3. Let Unity generate `.meta`, `Library/`, and project settings.
4. Run EditMode and PlayMode tests in Unity Test Runner.
5. Commit Unity-generated `.meta` files after the first successful import.

## Local GitHub push

```bash
git init
git add .
git commit -m "Initial Rogue Hero 5 skeleton"

gh auth login
gh repo create kjustin2/RogueHero5 --private --source=. --remote=origin --push
```

For a public repo, replace `--private` with `--public`.

## Test philosophy

Manual playtesting decides whether the combat is fun. Automated tests catch early breakage such as:

- dodge starts but does not move
- invulnerability never activates
- damage ignores invulnerability
- cooldowns never reset
- move definitions are missing required timing/effect data
- boss states get stuck
- per-frame combat code allocates unexpectedly

See [`docs/testing_strategy.md`](docs/testing_strategy.md).

## Codex handoff

This repo includes:

- [`AGENTS.md`](AGENTS.md): repository instructions Codex should read before making changes.
- [`.github/codex/prompts/review.md`](.github/codex/prompts/review.md): PR review prompt.
- [`docs/codex_cloud_start_prompt.md`](docs/codex_cloud_start_prompt.md): first task to paste into Codex cloud.
- [`.github/workflows/codex-pr-review.yml`](.github/workflows/codex-pr-review.yml): optional manual Codex GitHub Action workflow once `OPENAI_API_KEY` is added as a repo secret.

## Current skeleton contents

- Runtime combat primitives:
  - `Health`
  - `InvulnerabilityWindow`
  - `MoveDefinition`
  - `MoveRuntimeState`
  - `MoveRunner`
- Editor validation menu:
  - `Rogue Hero 5 > Validation > Validate Move Definitions`
- Tests:
  - EditMode cooldown/damage/invulnerability tests
  - PlayMode dodge movement + i-frame regression test
- Docs:
  - game design brief
  - testing strategy
  - performance plan
  - Unity 6.4 notes
  - Codex cloud start prompt

## Development loop

```text
You describe feature / bug / feel issue
        ↓
AI writes or updates tests first
        ↓
AI implements code
        ↓
Local tests run
        ↓
CI / workflow tests run
        ↓
You manually play with debug overlay
        ↓
Your feedback becomes the next issue/test/perf capture
```
