---
name: add-enemy-type
description: Add a new enemy to PoolPatrol, or add/modify enemy movement, attack, or on-death behavior. Use when the task mentions a new enemy, enemy AI, how enemies move/chase/shoot, or drop effects (gems, hearts, poison) on death. Explains the composable EnemyController component pattern.
---

# Adding / extending an enemy

Enemies live in `Assets/Scripts/Enemy`. An enemy is an `EnemyController` (namespace `Enemy`) plus a
set of **composable components**, each deriving from `EnemyComponentBase` (which grabs the shared
`EnemyController` in `Awake`). The controller wires them together and drives their ticks.

## How EnemyController works

`EnemyController` (`Assets/Scripts/Enemy/EnemyController.cs`) in `Awake` does
`GetComponent<EnemyMovement>()`, `GetComponent<EnemyAttack>()`, `GetComponent<EnemyDeathEffectHandler>()`,
caches the `Rigidbody2D`, and registers itself with the `CinemachineTargetGroup`. Then:
- `Update()` → `_movement?.Tick(); _attack?.Tick();`
- `FixedUpdate()` → `_movement?.FixedTick();`
- `Die(EnemyDeathParameters)` → emits water ripple, runs death effects, shakes camera, removes from
  the target group, and `Destroy(gameObject)`.

Each enemy is a **prefab** carrying `EnemyController` + one movement + (optional) one attack +
(optional) `EnemyDeathEffectHandler` + any number of `IOnDeathEffect` components. Its `EnemyType`
enum value is set on the controller.

## Steps to add a new enemy

1. Add a value to the `EnemyType` enum in `Assets/Scripts/Enemy/EnemyData.cs`. (`EnemyData` class
   itself is `[Obsolete]` — only the enum is live.)
2. Pick or create the behavior components (below).
3. Build the prefab: add `EnemyController` (set `EnemyType`, assign `WaterRippleParticleEmitter`),
   one movement component, optional attack, optional `EnemyDeathEffectHandler` + effects, a
   `Rigidbody2D`, and the `Enemy` tag (`Constants.GameConstants.TAG_Enemy`).
4. Register the prefab: add it to `EnemySpawner.AllEnemies` (it builds a `EnemyType → prefab` map),
   and add the type to the relevant `PhaseData` lists in `Assets/Scripts/SpawningLogic` if it should
   spawn in waves.

## Movement components (`Enemy.Movement`)

Derive from the abstract `EnemyMovement` (`Assets/Scripts/Enemy/EnemyMovement/EnemyMovement.cs`).
It handles a random start direction, smooth `DOTween` rotation via `AdjustRotation()`, and a
`PushBack(force, dir)` impulse helper. Implement:
- `public override void Tick()` — per-frame logic (read input/state).
- `public override void FixedTick()` — physics; set `Controller.RigidBody.linearVelocity`.

Reference implementations: `RandomMovement` (bounces off screen edges via `BounceMovementHandler`),
`ChaseStepMovement`, `EnemyNoMovement`, `EnemyImpulseMover`. Use `[RequireComponent]` for dependencies
(e.g. `RandomMovement` requires `BounceMovementHandler` and `Rigidbody2D`).

## Attack components (`Enemy.Attack`)

Derive from the abstract `EnemyAttack`. It exposes `[SerializeField] protected EnemyWeaponController
m_EnemyWeaponController;` and requires `public abstract void Tick();`. See `EnemyShooterAttack` for
firing through the shared weapon system.

## On-death effects (`Enemy.Death`)

Add a plain `MonoBehaviour` implementing `IOnDeathEffect` with `void Execute(EnemyDeathParameters
parameters)`. `EnemyDeathEffectHandler` collects **all** `IOnDeathEffect` components on the object via
`GetComponents` and runs each in `TriggerDeathEffects`. Stack as many as you like on one prefab.
Existing effects: `DropGemsOnDeath`, `DropHeartOnDeath`, `DropPoisonCloudOnDeath`,
`ThrowDeathParticlesOnDeath`, `ShootOnDeath`. Copy the shape of `DropGemsOnDeath` for a new one.

## Conventions reminder

`m_PascalCase` for `[SerializeField] private`, `_camelCase` for other privates, keep the component in
the matching sub-namespace (`Enemy.Movement` / `Enemy.Attack` / `Enemy.Death`). See
**poolpatrol-architecture** for the full convention list.
