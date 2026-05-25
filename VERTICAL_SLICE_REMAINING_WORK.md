# RogueHero5 Tiny Vertical Slice - Remaining Work

Current stop point: Unity is not in Play Mode, not compiling, and the active scene is `Assets/Scenes/BossRoom.unity`. The console currently has no errors, only unrelated editor/Cinemachine warnings.

## What is already in place

- Build settings are narrowed to `Startup`, `MainMenu`, and `BossRoom`.
- `MainMenu` is a single Start flow into a hidden local-host campaign session.
- Character selection and multiplayer lobby flow are bypassed for the slice.
- The fixed player avatar is `TankBoy`.
- `BossRoom` uses a runtime-created tiny arena instead of the original additive dungeon path.
- The old dungeon, additive scene loaders, debug panels, message feed, settings canvas, and emote bar were disabled in `BossRoom`.
- A result overlay exists with a Restart button for the boss-fight loop.
- The verifier menu item `Boss Room/Verify Duel Arena` passes and checks build settings, menu avatar, arena bounds, navmesh coverage, and boss prefab setup.
- A play smoke reached `BossRoom` as a local host with one connected client, one player character, one boss, and the runtime arena present.

## Cleanup partially completed

- `Startup` cleanup succeeded and was saved:
  - Removed `NetworkSimulator`
  - Removed `NetworkSimulatorUICanvas`
  - Removed `NetworkOverlay`
  - Removed `NetStatsMonitorPrefab`
- `MainMenu` cleanup succeeded and was saved:
  - Removed old session/profile/IP/settings/simulator-facing UI objects
  - Removed `SignInSpinner`
  - Removed Unity/sample logo objects
- `BossRoom` cleanup command started but failed before completion because Unity reported an invalid scene handle during the automated pass. `BossRoom` was already simplified before this, but it still needs a deliberate follow-up cleanup pass.

## Left to do

1. Finish `BossRoom` scene cleanup.
   Remove or permanently strip remaining cheat/debug/helper objects and serialized references instead of only disabling them.

2. Re-run script validation after Unity recompiles.
   Validate at least:
   - `ClientMainMenuState.cs`
   - `IPUIMediator.cs`
   - `ServerBossRoomState.cs`
   - `DuelResultOverlay.cs`
   - `DuelArenaVerifier.cs`

3. Re-run `Boss Room/Verify Duel Arena`.
   It was passing before the latest cleanup, but should be rerun after the scene objects are fully stripped.

4. Re-run the full play smoke.
   Start from `Startup`, invoke Start, wait until `BossRoom` settles, then verify:
   - local host is listening
   - exactly one player character exists
   - exactly one boss exists
   - `DuelArenaVerticalSlice` exists
   - player and boss are positioned near `(-5, 0, 0)` and `(5, 0, 0)`
   - defeating the boss shows the result overlay
   - Restart reloads the fight cleanly
   - stopping Play Mode produces no errors

5. Decide whether to delete unused assets/scripts or keep them parked.
   The current direction is a playable strip only, not hard-deleting old Boss Room systems. If the next pass should be more aggressive, remove unused scenes, debug-cheat scripts, simulator UI scripts, session UI, profile UI, and package references only after confirming no compile/runtime dependencies remain.

6. Simplify the remaining UI surface.
   Keep only:
   - Start button
   - in-fight player/boss combat HUD
   - result/restart overlay

7. Check the dirty worktree before continuing.
   Pre-existing unrelated items are still present and should not be touched unless explicitly requested:
   - deleted `img1/image.png`
   - untracked `.codex/`
   - untracked `asd/`

## Known notes

- `IPUIMediator` and `ClientMainMenuState` were updated to tolerate removed UI references.
- The latest console errors from stopping Play Mode were caused by stale spinner references; those null checks were added, but the fixed stop path still needs a fresh Play Mode verification.
- The verifier creates the runtime arena in editor for inspection, but the scene was not left dirty at the stop point.
