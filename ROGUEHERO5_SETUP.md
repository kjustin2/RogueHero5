# RogueHero5 Unity Setup

This repository root is the Unity project. Open this folder in Unity Hub:

```text
E:\Storage\SAAS\RogueHero5
```

The Boss Room sample has been promoted out of the nested `com.unity.multiplayer.samples.coop` clone and into the root project layout:

- `Assets/`
- `Packages/`
- `ProjectSettings/`
- `Documentation/`
- `RepoUtilities/`

## Unity Version

This repo is pinned to Unity `6000.3.16f1` in `ProjectSettings/ProjectVersion.txt`.

The original Boss Room sample targeted Unity `6000.0.52f1`. Unity 6.3 should upgrade/reimport it automatically on first open. Let the first import finish before pressing Play.

The original sample also shipped Unity Learn tutorial packages. Those packages fail to compile under Unity `6000.3.16f1`, so this repo removes `com.unity.learn.iet-framework`, `com.unity.learn.iet-framework.authoring`, and the tutorial-only editor assets. This does not remove the playable Boss Room scenes.

## First Open

1. Close any already-open Unity editor for this project.
2. In Unity Hub, choose Add project from disk.
3. Select `E:\Storage\SAAS\RogueHero5`.
4. Open it with Unity `6000.3.16f1`.
5. Wait for package restore, asset import, and script compilation.

You do not need to manually import assets. Unity imports the project assets from `Assets/` and resolves packages from `Packages/manifest.json`.

## Run The Sample

The build scenes are already configured in `ProjectSettings/EditorBuildSettings.asset`. The startup scene is:

```text
Assets/Scenes/Startup.unity
```

To try it in the editor, open `Assets/Scenes/Startup.unity` and press Play.

The main menu is now RogueHero5's duel menu:

- `Start Campaign` starts a solo local host, opens character select, then launches a 1v1 boss arena.
- After each campaign win, choose one of three cross-class ability rewards, replace one current slot, or keep the current loadout. The next boss rematch scales up health and damage for that session.
- `Host / Join 1v1` keeps the existing direct IP host/join flow, capped to two players, and removes boss room adds for a player duel.

For local multiplayer testing, use Unity's Multiplayer Play Mode package or run one editor instance plus a standalone build. UGS setup is optional for direct IP testing. The old session/lobby service path is still present in code, but the primary menu path is campaign plus direct-IP 1v1.

## Clean Reimport If Needed

If Unity was open while the project files were moved, close Unity and delete these generated folders before reopening:

```text
Library/
Temp/
Logs/
UserSettings/
```

They are ignored by Git and will be regenerated.
