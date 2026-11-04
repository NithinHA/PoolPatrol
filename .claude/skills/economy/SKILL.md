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
| `Coins` | no | `PlayerPrefs` (`Economy_Coins`, flushed on every change) | Meta currency: run-end payout, coin pouches, (future) IAP packs. Spent on cosmetics / weapons. |

To add another persistent currency: add an entry to `CurrencyType`, then one `Register(type, exhaustible: false, persistentKey: "...")` call in `EconomyService.Start`.

## Score (run stat, not a currency) — Docs/economy-and-progression.md

- Lives on `IRunStatsService` (`Score`, `HighestStreak`, `CoinsCollected`, `CoinPouchesDropped`), reset by `BeginRun`.
- **Attribution**: a player shot's `WeaponAttack` travels in `EnemyDeathParameters.Attack`; `EnemyController.Die` calls `attack.RegisterKill(this)` (and dies only once). Any new kill source belonging to a shot (AoE, chain, pierce) must pass the attack — `IComboWeapon.PerformComboHitEffect` receives it for that. Enemy friendly fire passes null and never scores.
- **`ScoreKeeper`** (Game scene, on LevelManager) listens to `PlayerWeaponController.OnShotFired(attack, streakAtFire)`, groups each shot's kills, and scores once per group when the shot resolves or after `ResolutionWindow` without a kill: `Σ EnemyController.ScoreValue × (1 + 0.2·s + 0.5·(n−1))`. Also +2 per gem pickup. Spawns a pooled `ScoreBubble` (`PoolableItemType.ScoreBubble`).
- All numbers live in `EconomyConfigSO` (`Assets/ScriptableObjects/Economy/EconomyConfig.asset`), including the run-end payout (`ComputePayout`).
- **Run end**: `LevelManager.SettleRun` → on win `IProgression.RecordLevelWin` (completed / best score / best streak, returns first-clear + arena-completion flags) → grants `RunPayout.Total` coins → `PostGameUI.Show(won, RunSummary)`.
- Coin pouches: `DropCoinPouchOnDeath` (gem dropper; chance + per-run cap) → `CoinCollectable` grants immediately. `CoinWalletUI` slides in over the gem wallet only for grants during an active run.
- Scene/prefab setup is re-runnable: **PoolPatrol/Economy/Wire Score & Coins** (`Editor/EconomyWiring.cs`).

## Unlockables and the Shop

- Every `LoadoutItemSO` (character / floatie / weapon — pool textures were removed) has `Unlock` = `Free | Coins | ArenaClear | IAP | BundleOnly` plus `CoinPrice`, `ArenaIndex`, `IapProductId`, `IapFallbackPrice`. Prices live on the item assets.
- `LoadoutService.IsUnlocked(item)` is the single rule: Free → yes; ArenaClear → `IProgression.IsArenaCompleted(ArenaIndex)` or owned; Coins / IAP / BundleOnly → owned (`PlayerPrefs` `Loadout_Owned_{Id}`). `TryPurchase` spends via `IEconomyService.TrySpend`; `GrantOwnership` is how IAP fulfilment grants items. Selections restore in `Start`, after progression is registered.
- Loadout screen lists **unlocked items only**. `LoadoutSelectorUI` filters internally but still reports absolute indices. Everything locked lives in the Shop.

### Main-menu Shop (`[Screen4] Shop`, unrelated to the in-run Goddess shop in `UI.Shop`)

- **Data:** the Characters / Floaties / Weapons tabs read `LoadoutCatalogSO`. The Bundles tab reads `ShopCatalogSO` (`Assets/ScriptableObjects/Shop`, assigned on `Bootstrap`), which lists `CoinPackSO` (consumable) and `ItemBundleSO` (non-consumable, references items directly). Both derive from `ShopOfferSO`; namespace `Monetization`.
- **`IShopService`** (`Framework/Services/ShopService`) is the UI's only way to buy: `BuyWithCoins(item)`, `BuyProduct(productId)`, `GetPrice`, `CanStartPurchase`, `RestorePurchases`, plus `OnPurchaseResolved` / `OnStoreStateChanged`. It builds one product table from both catalogs and fulfils payments.
- **`IStoreGateway`** hides the billing backend. `UnityIapStoreGateway` is the only file that references `UnityEngine.Purchasing`; `SimulatedStoreGateway` runs in the editor (`StoreGatewayFactory` compiles it out of players). Set `FailNextPurchaseWith` to rehearse a failure.
- **Fulfilment order** (`ShopService.HandlePurchasePending`): validate → record `Shop_Txn_{id}` → grant → flush PlayerPrefs → confirm. Never confirm before the grant is on disk; a redelivered transaction skips the grant via the ledger. `IReceiptValidator` defaults to `AcceptAllReceiptValidator` — replace before shipping.
- **UI** (`UI/Menu/Shop`, namespace `UI.Menu`): `ShopScreen` owns tabs, wallet and status; `ShopPanel` subclasses list cards; `ShopPriceTag` is a button for coin / money prices and an unclickable label for Owned / arena names. `MenuNavigator` runs the vertical slide.
- **Rebuild:** **PoolPatrol/Shop/Build Shop** (`Editor/ShopWiring.cs`) regenerates placeholder art, prefabs and the scene hierarchy. It creates content assets only on the first run, so edited prices are safe. Text uses the Shop's own Lato font because the default Liberation Sans has no ₹ glyph.

## Collectable pickups

**`CollectableBase`** (`Assets/Scripts/Economy/Collectables/CollectableBase.cs`) — attach to any world-space dropped item. Two-phase behaviour:

1. Player enters **`m_CollectionRadius`** (outer) → item homes toward player at `m_HomingSpeed`.
2. Item reaches **`m_GrantRadius`** (inner) → `Grant()` runs → `OnAnyCollected` fires → item tweens to HUD target and despawns.

### Lifetime (items sink)

An idle item is only around for **`m_IdleLifetime`** seconds (gems 15, hearts 20), then it sinks —
drifts down, shrinks, fades — and despawns **ungranted**. It flashes for the last
`m_SinkWarningTime` seconds as a warning.

The clock ticks **only in the `Idle` state**. Homing is a one-way door, so an item that has started
travelling toward the player can never sink, however long the flight takes. States `Sinking` and
`Collected` are both terminal and mutually exclusive — that is what guarantees exactly one despawn
per drop (the pool throws on a double release). Pause freezes the clock for free, since it is
`Time.deltaTime`-driven.

Subclass only needs:
```csharp
protected override CollectionTargetId TargetId => CollectionTargetId.GemWallet;
protected override void Grant() { /* apply reward */ }
```

Existing subclasses:
- **`GemCollectable`** — calls `ServiceLocator.GetEconomyService()?.Grant(CurrencyType.Gems, m_Value)`. Attach to `gem_red` / `gem_blue` prefabs.
- **`HeartCollectable`** — calls `PlayerHealth.Heal(m_HealAmount)`. Attach to heart prefabs.

`OnAnyCollected` is a **static event** — unsubscribe in `OnDestroy`.

### Pooling

`CollectableBase` implements `IPoolableObject`, so every drop comes from `ObjectPoolManager`
(`PoolableItemType.DroppedGem` / `DroppedHeart`) and `Despawn()` returns it to the pool — never
`Instantiate`/`Destroy`. `DropGemsOnDeath` / `DropHeartOnDeath` take a `PoolableItemType`, not a
prefab; the prefab lives in the scene's `ObjectPoolManager` pool config. All per-drop state resets in
`OnEnable` (which the pool triggers by activating the object), **not** `Awake` — a reused instance
never runs `Awake` again. See **add-poolable-item**.

## UI target registry (`CollectionTargets`)

Decouples world-space pickups from the HUD hierarchy. A UI element registers itself by adding `CollectionTargetMarker` (component) and picking its `CollectionTargetId`. `CollectableBase` calls `CollectionTargets.TryGetWorldPosition(id, camera)` to find where to fly.

- `CollectionTargetMarker` — auto-registers/unregisters on `OnEnable`/`OnDisable`.
- `GemWalletUI` — `TMP_Text` label that reads `IEconomyService.OnBalanceChanged` and updates its text. Also needs a `CollectionTargetMarker` (id = `GemWallet`) on the same object.
- Lives panel: add `CollectionTargetMarker` (id = `Health`) so hearts fly there. No marker = heart grants and despawns immediately.

## Adding a new collectable type

1. Create a `MonoBehaviour` that extends `CollectableBase`.
2. Override `TargetId` and `Grant()`.
3. Add a `PoolableItemType` entry and register the prefab in the scene's `ObjectPoolManager` config.
4. Attach to the prefab root, wire `m_Gfx` (the sprite that fades/flashes), and configure the radii
   and `m_IdleLifetime` in the inspector.

## Conventions reminder

Namespace is `Economy` (collectables) / `Economy.UI` (HUD). See **poolpatrol-architecture** for `m_`/`_` field conventions.
