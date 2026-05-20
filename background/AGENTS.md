# Rogue Hero 5 agent instructions

## Project identity

Rogue Hero 5 is a Unity 6.4 Update 3D roguelite arena fighter prototype focused on fast 1v1 boss duels, tiny player move kits, flashy ability-driven moves, deterministic testing, replayable bugs, and performance visibility. The current local editor target is `6000.4.7f1`; Unity 6.3 LTS is only a fallback if a 6.4-specific blocker appears.

Core tagline: **Five moves. One boss. No filler.**

## Working agreements

- Add or update tests before changing gameplay behavior.
- Prefer small, testable runtime systems over large scene-driven behavior.
- Do not edit `.unity`, `.prefab`, `.mat`, `.asset`, or `.meta` files unless explicitly asked.
- If scene/prefab setup is needed, write an editor script, test scene builder, or clear manual setup instructions.
- Avoid singleton sprawl. Prefer explicit references, test factories, and dependency seams.
- Keep runtime gameplay code in `Assets/RogueHero5/Runtime`.
- Keep editor-only tooling in `Assets/RogueHero5/Editor`.
- Keep tests in `Assets/RogueHero5/Tests`.
- Keep docs in `docs/`.
- After every task, summarize changed files, tests added/changed, tests run, manual Unity steps, and risks.

## Unity/code rules

- Use C# with clear namespaces under `RogueHero5`.
- Prefer named asmdefs. Do not rely on default `Assembly-CSharp` for testable code.
- Prefer ScriptableObject definitions for moves, bosses, upgrades, and reward tables.
- Prefer deterministic test helpers over hand-built scenes for automated tests.
- Do not put expensive work in `Update` without a profiler marker or clear reason.
- Avoid allocations in per-frame combat paths.
- Use object pools for projectiles, hitboxes, VFX, and temporary combat objects once those systems exist.
- Do not call `FindObjectOfType` or broad scene searches in gameplay loops.

## Move-system contracts

Every player move should have tests or validation for:

- command/input received
- equipped slot resolves correctly
- state transition occurs
- cooldown begins and eventually resets
- startup/active/recovery timing is honored
- movement, hitbox, projectile, or effect actually occurs
- invulnerability/counter windows work if configured
- cleanup happens after completion

A dodge-like move must prove it moves the actor a minimum distance and blocks damage during its i-frame window.

## Boss-system contracts

Every boss should have tests or validation for:

- at least three configured attacks after MVP
- telegraphs before damage
- phase transitions
- deterministic smoke fight with fixed seed
- can damage the player
- can be damaged by the player
- does not get stuck in one state forever

## Performance rules

For performance-sensitive changes:

- Add or preserve profiler markers around new systems.
- Report whether the change can instantiate objects during combat.
- Avoid GC allocations in core combat after warmup.
- Update relevant runtime counters if adding projectiles, hitboxes, pools, VFX, AI, or arena hazards.
- Add or update a performance benchmark when package support is enabled.

## Review guidelines

When reviewing changes, focus on serious issues:

- broken tests or missing tests for gameplay behavior
- accidental serialized Unity asset edits
- missing null checks around content-driven assets
- nondeterministic tests
- per-frame allocation or broad scene search
- hidden scene dependencies
- unbounded object spawning
- unclear manual setup steps
- move/boss behavior that cannot be validated automatically
