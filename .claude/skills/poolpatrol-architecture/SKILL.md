---
name: poolpatrol-architecture
description: Overview of the PoolPatrol Unity project — architecture map, coding conventions, namespaces, and where each system lives. Use this FIRST when working anywhere in this repo, when unsure where code belongs, or to route to a more specific skill (add-enemy-type, add-weapon, add-poolable-item, add-game-service, enemy-spawning).
---

# PoolPatrol architecture & conventions

PoolPatrol is a top-down 2D arcade shooter built in **Unity 6 (6000.0.36f1)** with **URP**, the
**new Input System**, **Netcode for GameObjects** (multiplayer, in progress), **DOTween**, and the
**Feel / More Mountains** feedback suite. Scripts live under `Assets/Scripts` (game code) and
`Assets/Weapons` (weapon subsystem). Third-party code is under `Assets/ThirdParty`, `Assets/Plugins`,
`Assets/Feel`, `Assets/Imports` — do not modify those.

## Boot flow

`Bootstrap` (a `Singleton`) runs first, registers persistent services with `ServiceLocator`
(`IGameService`, `IHighscore`, `ISceneService`), then loads the `Game` scene and switches the
`GameManager` state to `MainMenu`. See `Assets/Scripts/Framework/Bootstrap.cs`.

## System map

| System | Location | Notes |
|--------|----------|-------|
| Framework (Singleton, ServiceLocator, Bootstrap) | `Assets/Scripts/Framework` | Core plumbing. Services implement `IService`. |
| Game services (state machine, highscore, scenes) | `Assets/Scripts/Framework/Services/**` | Plain C# classes, registered in `Bootstrap`. See **add-game-service**. |
| Managers (Audio, Level, Session) | `Assets/Scripts/Managers` | Scene `Singleton`s (MonoBehaviours). |
| Object pooling | `Assets/Scripts/Pooling` | `ObjectPoolManager` + `IPoolableObject`. See **add-poolable-item**. |
| Enemies | `Assets/Scripts/Enemy/**` | `EnemyController` + composable `EnemyComponentBase` parts. See **add-enemy-type**. |
| Weapons | `Assets/Weapons/**` + `Assets/Scripts/Weapon` | `WeaponBase` + `WeaponControllerBase` + `WeaponMagazine`. See **add-weapon**. |
| Spawning / waves | `Assets/Scripts/SpawningLogic` | `ArenaDirector` + ScriptableObject sections (`CooldownSectionSO`, `WaveSectionSO`). See **enemy-spawning**. |
| Player | `Assets/Scripts/Player/**` | `PlayerController` (input events), `PlayerCombo`, `PlayerHealth`. |
| UI / HUD | `Assets/Scripts/UI/HUD` | Polls weapon/player state; some listen to `WeaponMagazine` events. |
| Movement | `Assets/Scripts/Movement` | `ImpulseMover`, `BounceMovementHandler`. |
| Constants | `Assets/Scripts/Util/Constants.cs` | Tags, scene names, screen bounds, audio keys, multiplayer property keys. |

## Coding conventions (match these exactly)

- **Private serialized fields**: `[SerializeField] private Type m_PascalCase;` (the `m_` prefix is
  used consistently for inspector-facing private fields, e.g. `m_BulletPrefab`, `m_SpawnDelay`).
- **Private non-serialized fields**: `_camelCase` (e.g. `_movement`, `_reloadTimer`).
- **Public fields / properties**: `PascalCase`.
- **Namespaces** are per-subsystem and do NOT mirror the folder path: `PTL.Framework`,
  `PTL.Framework.Services`, `Enemy`, `Enemy.Movement`, `Enemy.Attack`, `Enemy.Death`, `Weapon`,
  `Pooling`, `SpawningLogic`, `Player`, `Movement`, `Utils`. `Constants` has **no** namespace.
  When adding a file, use the namespace already used by its sibling files, not a new one.
- Prefer `[Header("...")]` / `[Space]` to group inspector fields, as existing weapons/enemies do.
- Use `Debug.LogError`/`LogWarning` for misconfiguration (missing components/prefabs), following the
  `[SystemName]` message prefix style seen in `ObjectPoolManager`.
- Reference tags via `Constants.GameConstants.TAG_*`, never string literals.
- Gameplay code uses `DOTween` (`DG.Tweening`) for tweens; kill cached tweens in `OnDestroy`.

## Composition over inheritance

Enemies and weapons favor **small composable components** and callback objects over deep hierarchies:
- Enemy behavior = separate `EnemyMovement`, `EnemyAttack`, and `IOnDeathEffect` components on one prefab.
- A shot returns a `WeaponAttack` with `OnHitSuccess` / `OnHitFail` / `OnAttackComplete` callbacks
  rather than the weapon polling results.
Keep new features in this style.

## Important gotchas

- `Singleton<T>.Instance` uses `FindObjectOfType` lazily; managers may be null before the scene loads —
  existing code uses null-conditional calls (`AudioManager.Instance?.PlaySound(...)`).
- The scene has exactly one `ObjectPoolManager`; spawn/release only through it.
- Netcode/multiplayer is partially wired (`PlayerController.Local`, `MultiplayerConstants`); most
  gameplay is still single-player/local. Confirm before assuming networked behavior.
