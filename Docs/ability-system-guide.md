# Ability System Guide

## Architecture Overview

The ability system is a 3-layer stack. **Layer 1 (data + services)** is built. Layers 2 and 3 are not yet implemented.

```
Layer 3 — Pool Goddess UI          (not built)
   Goddess spawns mid-arena, player touches her, shop opens
   ↓ calls
Layer 2 — Arena Director            (not built)
   Drives waves, spawn timing, decides when Goddess appears
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

The shop should pause gameplay. Until a `PauseService` is built:

```csharp
// Open shop
Time.timeScale = 0f;

// Close shop (buy, skip, or timeout)
Time.timeScale = 1f;
```

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

## What's not built yet

| Feature | Needed for | Notes |
|---|---|---|
| **ArenaDirector** (Layer 2) | Triggering Goddess appearances at the right time | Will call into a `PoolGoddess` component |
| **PoolGoddess** (Layer 3) | Emerge/retreat animation, touch-to-open trigger | Spawns in center, 15s window, pauses on touch |
| **GoddessShopUI** (Layer 3) | The actual UI panel | Calls `GenerateOffers`, `TryPurchase`, refresh/skip |
| **PauseService** | Clean pause/unpause | `Time.timeScale` toggle with event hooks |
| **GemMagnet wiring** | `GemMagnetRadius` stat actually affecting pickup | `CollectableBase` needs to read `GetFlatSum(GemMagnetRadius)` |
| **DamageRevenge execution** | Pushback on hit | Needs implementation in `PlayerHealth` or a new component |
| **BulletRebound / PiercingShot** | Bullet behavior changes | Bullet script needs to check `GetFlag()` |
