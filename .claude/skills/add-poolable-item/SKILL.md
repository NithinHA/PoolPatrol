---
name: add-poolable-item
description: Add a new pooled object type to PoolPatrol, or spawn/release objects through the object pool. Use when the task involves reusing objects instead of Instantiate/Destroy — particles, VFX, bullets, crosshairs, or anything spawned frequently. Explains ObjectPoolManager, IPoolableObject, and PoolableItemType.
---

# Adding / using a pooled object

Pooling avoids per-spawn `Instantiate`/`Destroy` GC churn. Lives in `Assets/Scripts/Pooling`
(namespace `Pooling`). One `ObjectPoolManager` exists in the scene; it owns a `UnityEngine.Pool.ObjectPool`
per `PoolableItemType`.

## The contract

Every pooled prefab needs a MonoBehaviour implementing **`IPoolableObject`**:
- `void Initialize(PoolableItemType type)` — called once at creation; store the type so the object can
  self-return to the correct pool.
- `void Reset()` — called each time it's fetched from the pool; restore a clean, ready state.
- `void ReturnToPool()` — call this when the object is done; it should delegate to
  `ObjectPoolManager.Instance.ReleaseItem(type, this)`.

`ObjectPoolManager` handles activation: on get it `SetActive(true)` + `Reset()`; on release
`SetActive(false)`. See `PoolableParticles` (`Assets/Scripts/Pooling/PoolableParticles.cs`) for a
reference implementation.

## Steps to add a new poolable type

1. Add a value to the `PoolableItemType` enum in `Assets/Scripts/Pooling/PoolableItemType.cs`
   (current values: `Crosshair`, `EnemySpawnParticles`, `BulletHitParticles`, `BulletComboExplosion`,
   `BulletMissedParticles`).
2. Create the prefab with a component implementing `IPoolableObject` (or reuse `PoolableParticles`
   for a particle system).
3. In the scene, select the `ObjectPoolManager` object and add a `PoolConfig` entry to its
   `m_PoolConfigs` list: set `Type`, `Prefab`, `DefaultCapacity`, `MaxSize`. Without a matching config
   the manager logs `[PoolManager] No pool configured for type: ...` and returns null.

## Spawning and releasing

```csharp
// Spawn — returns IPoolableObject; cast to your concrete type.
var explosion = (PoolableParticles)ObjectPoolManager.Instance.SpawnItem(
    PoolableItemType.BulletComboExplosion, hitPosition, Quaternion.identity);

// ... use it ...

// Release — usually the object calls this on itself when finished:
ReturnToPool(); // -> ObjectPoolManager.Instance.ReleaseItem(type, this);
```

`SpawnItem(type, position, rotation)` positions the object for you. Prefer pooling for anything spawned
often (bullet hits, VFX, crosshairs). One-off / rare objects can still use plain `Instantiate`.

## Gotchas

- The prefab's root must carry the `IPoolableObject` component — `CreatePooledObject` logs an error if
  it can't find one via `TryGetComponent`.
- `collectionCheck: true` is on, so releasing the same instance twice throws — release exactly once.
- Conventions: `m_PascalCase` serialized privates, `_camelCase` privates. See **poolpatrol-architecture**.
