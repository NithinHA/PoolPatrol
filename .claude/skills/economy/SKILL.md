---
name: economy
description: Work with PoolPatrol's economy system — gem balances, run-scoped vs persistent currencies, collectable pickups (GemCollectable, HeartCollectable), the two-phase collection radius/grant/fly-to-HUD flow, CollectionTargets registry, or the GemWalletUI. Use when the task involves currencies, spending/granting gems, in-run upgrades, dropped item pickup behaviour, or wallet HUD.
---

# Economy system

## Service layer

**`IEconomyService`** (`Assets/Scripts/Framework/Services/EconomyService/`) — registered in `Bootstrap`, resolved via `ServiceLocator.GetEconomyService()`.

Key API:
```csharp
void Grant(CurrencyType type, int amount);       // awards; fires OnBalanceChanged
bool TrySpend(CurrencyType type, int amount);     // deducts if affordable; returns success
bool CanAfford(CurrencyType type, int amount);
int  GetBalance(CurrencyType type);
event Action<CurrencyType, int, int> OnBalanceChanged; // (type, newBalance, delta)
void BeginRun();  // resets all exhaustible currencies to 0 — called by LevelManager.Start
```

## Currency types (`CurrencyType.cs`)

| Type | Exhaustible | Storage | Purpose |
|------|-------------|---------|---------|
| `Gems` | ✅ yes | in-memory | Arena economy. Spent on in-run upgrades. Wiped by `BeginRun()`. |
| _(future)_ | no | `PlayerPrefs` | Meta-game unlocks (characters, weapons). One `Register` call, everything else is shared. |

To add a persistent currency: add an entry to `CurrencyType`, then in `EconomyService.Start`:
```csharp
Register(CurrencyType.Coins, exhaustible: false, persistentKey: "Economy_Coins");
```

## Collectable pickups

**`CollectableBase`** (`Assets/Scripts/Economy/Collectables/CollectableBase.cs`) — attach to any world-space dropped item. Two-phase behaviour:

1. Player enters **`m_CollectionRadius`** (outer) → item homes toward player at `m_HomingSpeed`.
2. Item reaches **`m_GrantRadius`** (inner) → `Grant()` runs → `OnAnyCollected` fires → item tweens to HUD target and despawns.

Subclass only needs:
```csharp
protected override CollectionTargetId TargetId => CollectionTargetId.GemWallet;
protected override void Grant() { /* apply reward */ }
```

Existing subclasses:
- **`GemCollectable`** — calls `ServiceLocator.GetEconomyService()?.Grant(CurrencyType.Gems, m_Value)`. Attach to `gem_red` / `gem_blue` prefabs.
- **`HeartCollectable`** — calls `PlayerHealth.Heal(m_HealAmount)`. Attach to heart prefabs.

`OnAnyCollected` is a **static event** — unsubscribe in `OnDestroy`.

## UI target registry (`CollectionTargets`)

Decouples world-space pickups from the HUD hierarchy. A UI element registers itself by adding `CollectionTargetMarker` (component) and picking its `CollectionTargetId`. `CollectableBase` calls `CollectionTargets.TryGetWorldPosition(id, camera)` to find where to fly.

- `CollectionTargetMarker` — auto-registers/unregisters on `OnEnable`/`OnDisable`.
- `GemWalletUI` — `TMP_Text` label that reads `IEconomyService.OnBalanceChanged` and updates its text. Also needs a `CollectionTargetMarker` (id = `GemWallet`) on the same object.
- Lives panel: add `CollectionTargetMarker` (id = `Health`) so hearts fly there. No marker = heart grants and despawns immediately.

## Adding a new collectable type

1. Create a `MonoBehaviour` that extends `CollectableBase`.
2. Override `TargetId` and `Grant()`.
3. Optionally override `Despawn()` if the prefab is pooled (call `ReturnToPool()` instead of `Destroy`).
4. Attach to the prefab and configure radii in the inspector.

## Conventions reminder

Namespace is `Economy` (collectables) / `Economy.UI` (HUD). See **poolpatrol-architecture** for `m_`/`_` field conventions.
