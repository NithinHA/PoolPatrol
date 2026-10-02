# Ability System Guide

## Architecture Overview

The ability system is a 3-layer stack. All three layers are built.

```
Layer 3 — Pool Goddess + Shop UI    ✅ built
   PoolGoddess (world) → AbilityShopUI → AbilityOfferCard
   ↓ opened by
Layer 2 — Encounter / Arena Director ✅ built
   ArenaDirector drives waves + progress; GoddessEncounterDirector schedules visits
   ↓ feeds context to
Layer 1 — Data + Services           ✅ built
   AbilityDefinition (SO) → AbilityService → RunModifierService → Binders → live components
```

### Layer 1 components

| File | Role |
|---|---|
| `Scripts/Abilities/AbilityDefinition.cs` | ScriptableObject — one per ability. Holds levels, costs, stat modifiers, eligibility filters |
| `Scripts/Abilities/AbilityDatabase.cs` | ScriptableObject — the pool of all abilities. Assigned on Bootstrap |
| `Scripts/Abilities/StatId.cs` | Enum of every modifiable stat and flag |
| `Scripts/Abilities/AbilityTags.cs` | Category, WeaponKind, ArenaFlags, Rarity, RunRequirement, InstantEffectType |
| `Services/AbilityService/AbilityService.cs` | Eligibility filtering, weighted offer generation, purchase execution |
| `Services/RunModifierService/RunModifierService.cs` | Stat accumulator: `(base + flat) × (1 + percent)`, plus boolean flags |
| `Abilities/Binders/PlayerStatBinder.cs` | Pushes modifiers → PlayerHealth, ImpulseMover, PlayerCombo |
| `Abilities/Binders/WeaponStatBinder.cs` | Pushes modifiers → WeaponBase, WeaponMagazine |

### Layer 2 components

| File | Role |
|---|---|
| `Scripts/SpawningLogic/ArenaDirector.cs` | Wave/cooldown timeline. Exposes `ArenaProgress`, `IsInWave`, `IsRunning`, `OnArenaBegan` |
| `Scripts/SpawningLogic/ArenaSpawnConfigSO.cs` | Per-level data, incl. the new `GoddessEncounters` block (visit count, window, refresh pricing, grace) |
| `Scripts/Abilities/Goddess/GoddessEncounterDirector.cs` | Schedules visits along arena progress, places her centrally, owns the encounter lifecycle |

### Layer 3 components

| File | Role |
|---|---|
| `Scripts/Abilities/Goddess/PoolGoddess.cs` | Emerge/retreat animation, 15s window, touch trigger |
| `Scripts/UI/Shop/AbilityShopUI.cs` | The ShopScreen panel: pause, offers, purchase, refresh, exit |
| `Scripts/UI/Shop/AbilityOfferCard.cs` | One offer slot (name, description, price, level, rarity tint, sold state) |
| `Services/PauseService/PauseService.cs` | Reference-counted `Time.timeScale` freeze with `OnPauseChanged` |

### Data flow

```
AbilityDefinition (SO)
   → AbilityService.TryPurchase()
      → EconomyService.TrySpend(gems)
      → RunModifierService.AddRange(level.Modifiers)
      → fires OnInstantEffect for each InstantEffectType
      → fires OnAbilityPurchased(definition, newLevel)

RunModifierService.OnModifiersChanged
   → PlayerStatBinder.ApplyModifiers()  (max lives, speed, propulsion, combo)
   → WeaponStatBinder.ApplyModifiers()  (range, speed, size, magazine, reload, fire rate)
```

---

## How to add a new ability

### Option A: Editor menu (batch)

Edit `Scripts/Editor/CreateStarterAbilities.cs`, add your ability following the existing pattern, then run **PoolPatrol → Create Starter Abilities**. This overwrites all starter abilities and re-populates the database.

### Option B: Unity Inspector (single)

1. **Assets → Create → PoolPatrol → Ability**
2. Fill in the fields:
   - **Id** — unique stable slug (e.g. `bigger_bullets`)
   - **DisplayName** — player-facing name
   - **Category** — General / Weapon / Arena / Synergy
   - **WeaponFilter** — set to `Any` for general abilities, or specific weapon(s)
   - **ArenaFilter** — set to `Any` for universal, or specific arena(s)
   - **RunRequirement** — `None`, `LivesBelowMax` (Extra Life), or `MaxLivesBelowCap` (+1 Max Life)
   - **Rarity** — affects selection weight multiplier
   - **SelectionWeight** — relative pick probability (default 1.0)
3. **Add Levels** (index 0 = level 1):
   - **Cost** — gem price
   - **Description** — short text shown in shop (e.g. "Bullet size +25%")
   - **Modifiers** — list of `{Stat, Type, Value}`. Type is Flat, Percent, or Flag
   - **InstantEffects** — one-shot effects like `RestoreOneLife` or `RefillMagazine`
4. Drag the asset into the **AbilityDatabase** asset's list

### Adding a new stat

If your ability modifies something not in `StatId`:

1. Add the entry to the `StatId` enum
2. Teach the relevant binder to read it:
   - Player stats → `PlayerStatBinder.ApplyModifiers()`
   - Weapon stats → `WeaponStatBinder.ApplyModifiers()`
   - New system → create a new binder MonoBehaviour that subscribes to `OnModifiersChanged`

### Adding a new instant effect

1. Add the entry to `InstantEffectType` enum
2. Handle it in `PlayerStatBinder.OnInstantEffect()` (or a new handler subscribed to `IAbilityService.OnInstantEffect`)

### Adding a new run requirement

1. Add the entry to `RunRequirement` enum
2. If the check needs new data, extend `IRunStateProvider` and update `PlayerStatBinder`
3. Add the case in `AbilityService.MeetsRunRequirement()`

---

## General vs Weapon ability examples

**General ability** (no weapon filter):
```
Id:             gem_magnet
DisplayName:    Gem Magnet
Category:       General
WeaponFilter:   Any
ArenaFilter:    Any
RunRequirement: None
Levels:
  [0] Cost=50,  Modifiers=[{GemMagnetRadius, Flat, 1}]
  [1] Cost=60,  Modifiers=[{GemMagnetRadius, Flat, 1}]
  [2] Cost=70,  Modifiers=[{GemMagnetRadius, Flat, 1}]
```
Each level adds +1 to the radius. Total at level 3 = base + 3.

**Weapon ability** (handgun only):
```
Id:             bigger_bullets
DisplayName:    Bigger Bullets
Category:       Weapon
WeaponFilter:   Handgun
ArenaFilter:    Any
Levels:
  [0] Cost=90, Modifiers=[{BulletSize, Percent, 0.25}]
  [1] Cost=90, Modifiers=[{BulletSize, Percent, 0.25}]
  [2] Cost=90, Modifiers=[{BulletSize, Percent, 0.25}]
```
Each level adds +25% (they sum: level 3 = base × 1.75).

**Flag ability** (one-shot, no levels to stack):
```
Id:             piercing_shot
DisplayName:    Piercing Shot
Category:       Arena
Levels:
  [0] Cost=100, Modifiers=[{PiercingShot, Flag, 1}]
```

---

## Integrating Pool Goddess Shop UI with AbilityService

The Goddess UI is the consumer of Layer 1. Here's the integration contract.

### 1. Generate offers when the shop opens

```csharp
IAbilityService abilities = ServiceLocator.GetAbilityService();

// Generate 2 offers. Pass null for the first time; pass previous offers on Refresh.
List<AbilityOffer> offers = abilities.GenerateOffers(2);
```

Each `AbilityOffer` contains everything the UI needs:
- `offer.DisplayTitle` — e.g. "BIGGER BULLETS II"
- `offer.Description` — e.g. "Bullet size +50%"
- `offer.Cost` — gem price
- `offer.Level` — the level the player would reach
- `offer.Definition.MaxLevel` — for "Level 2 / 3" display
- `offer.Definition.Icon` — Sprite (may be null until art is assigned)
- `offer.Definition.Rarity` — for card border color/glow

### 2. Show affordability

```csharp
bool canAfford = abilities.CanPurchase(offer);
// Grey out the buy button if false
```

### 3. Purchase

```csharp
if (abilities.TryPurchase(offer))
{
    // Purchase succeeded:
    // - Gems deducted (EconomyService)
    // - Modifiers applied (RunModifierService → Binders)
    // - Instant effects fired (OnInstantEffect)
    // - OnAbilityPurchased event raised
    // Close shop or refresh offers
}
```

### 4. Refresh (discard current pair, get new ones)

```csharp
IEconomyService economy = ServiceLocator.GetEconomyService();
int refreshCost = 50; // your escalating cost logic

if (economy.CanAfford(CurrencyType.Gems, refreshCost))
{
    economy.TrySpend(CurrencyType.Gems, refreshCost);

    // Exclude current offers so they don't reappear
    var excluded = currentOffers.Select(o => o.Definition);
    List<AbilityOffer> newOffers = abilities.GenerateOffers(2, excluded);
}
```

### 5. Skip

Just close the shop UI and unpause. No service calls needed.

### 6. Pause/unpause

`PauseService` owns this. Never touch `Time.timeScale` directly.

```csharp
IPauseService pause = ServiceLocator.GetPauseService();
pause.Pause(this);    // freeze; safe to call twice from the same source
pause.Resume(this);   // resumes only once every source has released
```

Pause requests are reference-counted per source, so two systems can hold the game
frozen at once. Because it is a timescale freeze, anything driven by `Time.deltaTime`,
`FixedUpdate` or a scaled `WaitForSeconds` stops for free. `Update` still runs, so any
input reader must check `IsPaused` — `PlayerController` already does.

### 7. Listen for balance changes (wallet display)

```csharp
ServiceLocator.GetEconomyService().OnBalanceChanged += (type, newBalance) =>
{
    if (type == CurrencyType.Gems)
        walletLabel.text = newBalance.ToString();
};
```

### 8. Display owned abilities (optional sidebar)

```csharp
IReadOnlyDictionary<AbilityDefinition, int> owned = abilities.OwnedAbilities;
foreach (var (def, level) in owned)
{
    // Show icon + level indicator
}
```

---

## Which stats actually do something

| StatId | Consumer | Status |
|---|---|---|
| `MaxLives` | `PlayerStatBinder` → `PlayerHealth.SetMaxHealth` | ✅ |
| `MoveSpeed`, `Propulsion` | `PlayerStatBinder` → `ImpulseMover` | ✅ |
| `ComboThresholdReduction` | `PlayerStatBinder` → `PlayerCombo.SetThresholdReduction` | ✅ |
| `GemMagnetRadius` | `CollectableBase` (via `GemCollectable.MagnetRadiusStat`) | ✅ |
| `BulletRange`, `BulletSpeed`, `BulletSize` | `WeaponStatBinder` → `WeaponBase` | ✅ |
| `ReloadSpeed`, `FireRate`, `MagazineSize` | `WeaponStatBinder` → `WeaponMagazine` | ✅ |
| `DamageRevenge` | `DamageRevengeHandler` on the player | ✅ |
| `PiercingShot`, `BulletRebound` | `WeaponBase.BuildBulletTraits` → `Bullet.Configure` | ✅ |
| `ToxicImmunity`, `StormImmunity`, `FoamBreaker`, `VineCutter` | — | ⏳ the arena hazards they counter don't exist yet. The flags are set correctly; whoever builds those hazards reads `GetFlag(...)`. |
| `ExplosionRadius`, `HomingDuration` | — | ⏳ no grenade launcher yet. |

Instant effects (`RestoreOneLife`, `RefillMagazine`) are handled in `PlayerStatBinder.OnInstantEffect`.

## Exhaustible vs persistent abilities

The distinction is in the data, not in a separate flag:

* An ability level whose `InstantEffects` list is non-empty **spends itself at purchase**
  (`+1 Life` heals immediately and is gone).
* An ability level whose `Modifiers` list is non-empty **lasts for the rest of the run**
  (Bigger Bullets, +1 Max Life). It is recorded in `RunModifierService` and re-applied by
  the binders on every change.

`IAbilityService.ActiveAbilities` is the list of abilities still exerting a lasting
effect — that's what a HUD or debug overlay should show. `OwnedAbilities` is the raw
purchase ledger and includes spent one-shots. Both are cleared by `ResetRun()`, which
`LevelManager` calls at the start of every run, so nothing survives level complete or
game over.

---

## Scene wiring checklist

Code-side everything is done; these are the Editor steps.

### Already wired for you

* `Prefabs/AbilitySelection/PoolGoddess.prefab` — `PoolGoddess` component added, with
  `Visual` = the `gfx` child and `Touch Trigger` = the root `CircleCollider2D`.
  Optionally assign `Emerge Effect` / `Retreat Effect` particle systems.
* `Prefabs/Player variants/Player.prefab` — `WeaponStatBinder` added to `WeaponHolder`
  (it was missing, so no weapon upgrade did anything), and `DamageRevengeHandler` added
  to the `Player` root.

### Game scene — ShopScreen

1. Add **AbilityShopUI** to the `ShopScreen` GameObject itself, and set **Root** to that
   same GameObject (it initialises lazily, so it is fine that it starts inactive).
2. Add **AbilityOfferCard** to `Option_1`, `Option_2`, `Option_3` and fill in:
   * `Button` → the Button on the option root
   * `Name Text` → `name_text`
   * `Description Text` → `desc_text`
   * `Price Text` → `pricetag`
   * `Icon` → `icon`
   * `Background` → the Image on the option root (for the rarity tint)
   * `Level Text` / `Sold Overlay` → optional, create if you want them
3. Drag the three cards into `AbilityShopUI.Cards`, in display order.
4. **Add an Exit button** to `ShopScreen` (does not exist yet) and assign it to
   `AbilityShopUI.Exit Button`. Nothing else closes the shop.
5. Optional: a Refresh button + its cost label → `Refresh Button` / `Refresh Cost Text`;
   a gem balance label → `Wallet Text`; a "nothing left to offer" object →
   `Nothing Left Message`.

### Game scene — encounter director

6. Create an empty GameObject (e.g. `GoddessDirector`) and add
   **GoddessEncounterDirector**. Assign:
   * `Goddess Prefab` → `Prefabs/AbilitySelection/PoolGoddess`
   * `Arena Director` → the `EnemySpawner` object (auto-found if left empty)
   * `Shop UI` → the `ShopScreen` object (auto-found if left empty)
7. On `LevelManager`, assign **Shop UI** → `ShopScreen` (auto-found if left empty). This
   is what force-closes the shop on win/loss so the game can never stay frozen.
8. Delete the leftover inactive `GoddessOnMap` object — the director spawns her now.

### Data

9. On each `ArenaDefinitionSO`, set the new **Arena** flag (ShallowEnd, RitzRipples, …).
   Until this is set the arena stays `Any` and arena-specific abilities are all eligible.
10. On each `ArenaSpawnConfigSO`, tune the new **Goddess** block: `Visit Count`,
    `Availability Window` (15s), `Offer Count` (3 for the current layout),
    `Refresh Cost` / `Refresh Cost Increment` / `Max Refreshes Per Visit`,
    `Min Seconds Between Visits`, `Reentry Grace Seconds`.
11. Make sure `Bootstrap` still has the `AbilityDatabase` asset assigned.

### Player-facing collider note

The goddess trigger fires against anything tagged `Player`. The player root carries the
Rigidbody2D, so the trigger works without adding one to the goddess. She has no
Rigidbody2D on purpose — enemies, bullets and the player all pass through her.
