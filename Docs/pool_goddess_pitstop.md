# Arena Gameplay — Pool Goddess Mid-Game Pitstop

## 1. Purpose

The Pool Goddess is the arena's mid-run upgrade system.

She is intentionally placed in the **middle region of the pool** rather than at an arena edge.

This serves two purposes:

1. Provides a temporary shop where the player can spend gems earned during the current arena run.
2. Creates a strong movement objective that counteracts edge/corner camping caused by the player's recoil-based propulsion.

The player normally tends to shoot enemies toward the center and therefore propel toward the perimeter.

The Goddess reverses that incentive:

> **The center becomes dangerous, but valuable.**

---

# 2. Core Interaction

At configured intervals, the Pool Goddess emerges somewhere in the central region of the arena.

She offers two upgrade choices.

The player must physically touch her to open the shop.

Touching her causes the entire gameplay simulation to pause.

This includes:

- Enemies.
- Enemy projectiles.
- Player movement.
- Environmental hazards.
- Foam growth.
- Lightning telegraphs.
- Other moving objects.

The shop UI then appears.

---

# 3. Spawn Location

The Goddess should never spawn directly on top of the player.

Recommended spawn region:

`Central 40–60% of arena`

Avoid:

- Arena edges.
- Corners.
- Hazard zones.
- Extremely close proximity to the player.

A small random offset should prevent every appearance from occurring at exactly the same location.

---

# 4. Spawn Timing

The number of Goddess appearances is configured per arena.

However, the exact timing should remain hidden from the player.

The Encounter Director chooses an appropriate moment within each window.

The Goddess can appear:

- During preparation.
- During an active wave.
- Immediately after a wave.

She does not need to wait for a cooldown.

---

# 5. Appearance Sequence

Recommended sequence:

```text
Goddess absent
     ↓
Goddess emerges
     ↓
15-second availability window
     ↓
Player touches Goddess?
     ├── YES → Pause → Shop
     └── NO  → Goddess retreats
```

The Goddess should visually telegraph her appearance with:

- Water ripple.
- Rising animation.
- Sparkles/bubbles.

---

# 6. Availability Window

Recommended default:

`15 seconds`

The player has 15 seconds to physically reach her.

During this time:

- Combat continues normally.
- Enemies can pass through her.
- Enemy projectiles can pass through her.
- She cannot be damaged.
- She does not block movement.

If the player ignores her:

`Goddess → retreat → cooldown → future appearance`

Do not punish the player for ignoring her.

Skipping the encounter is a legitimate strategic choice.

---

# 7. Touch Interaction

The player only needs to touch the Goddess.

No hold interaction.

No button press.

No multi-step interaction.

The instant the player enters her interaction radius:

`PauseGameplay()`

Then open the Goddess Shop.

This prevents frustrating situations where the player reaches her but dies while waiting for an interaction animation.

---

# 8. Goddess Shop UI

The Goddess holds two items.

Example:

```text
                 POOL GODDESS

             [ BIGGER BULLETS ]
                    90 💎

                    🧜‍♀️

             [ QUICK RELOAD ]
                    80 💎


        [ ↻ REFRESH 50 💎 ]    [ SKIP ]
```

The two offers should be visually attached to her left and right hands.

The player immediately understands:

> "She is offering me two things."

---

# 9. Offer Categories

## A. General Abilities

Available regardless of weapon or arena.

Examples:

### Movement Speed
Increases base movement/drift speed.

### +1 Life
Restores one missing life.

### +1 Max Life
Increases maximum life by one.

### Faster Combo
Takes few less successful shots to attain Combo Max.

### Gem Magnet
Increases automatic gem collection range.

### Damage Revenge
When the player loses a life, nearby enemies are pushed away.

---

# 10. Weapon-Specific Abilities

The Goddess only offers abilities applicable to the currently equipped weapon.

## Handgun

Possible upgrades:

- Increased range.
- Faster bullet speed.
- Larger bullet size.
- Faster reload.
- Increased magazine size.
- Bullet rebound.

## Shotgun

Possible upgrades:

- Longer range.
- Faster reload.
- +1 magazine size.
- Increased propulsion.

## Grenade Launcher / Homing Grenades

Possible upgrades:

- Larger explosion radius.
- Faster projectile speed.
- Faster reload.
- +1 magazine size.
- Longer homing duration.

Each weapon should have its own curated upgrade pool.

---

# 11. Arena-Specific Abilities

Arena-specific upgrades should counter or interact with mechanics present in the current arena.

Examples:

## Luxury Pool

### Piercing Shot
Bullets can penetrate newspaper shields.

## Tropical Pool

### Piercing Shot
Bullets can damage protected crab claws more effectively.

## Toxic Pool

### Toxic Immunity
Player is immune to toxic contamination.

### Piercing Shot
Bullets can penetrate zombie-duck protection.

## Amazonian Pool

### Vine Cutter
Increased damage against vines.

## Cloudy Pool

### Foam Breaker
Bullets destroy foam faster.

### Storm Immunity
Lightning zones cannot damage the player.

Do not create separate versions of the same conceptual upgrade when possible.

A universal `Piercing Shot` can interact differently with different arena mechanics.

---

# 12. Ability Eligibility System

The Goddess should never show an ability that makes no sense.

Before generating offers, evaluate every ability using an eligibility filter.

Example:

```text
AbilityEligible =
    ArenaCompatible
    AND WeaponCompatible
    AND RunStateCompatible
    AND NotAlreadyOwned
    AND NotBlocked
    AND MeetsPrerequisites
```

Only eligible abilities enter the candidate pool.

---

# 13. General Eligibility Examples

### +1 Life

Eligible only when:

`CurrentLives < MaxLives`

### +1 Max Life

Eligible if:

`CurrentMaxLives < MaxLifeCap`

---

# 14. Weapon Eligibility

If the player has a handgun:

Do not offer:

- Shotgun spread.
- Grenade explosion radius.
- Homing duration.

---

# 15. Duplicate Handling

Most one-time abilities should disappear once purchased.

Example:

`Foam Breaker`

Once obtained:

`Foam Breaker → removed from candidate pool`

Stackable abilities are exceptions.

Examples:

- +1 Life.
- +1 Max Life.
- +1 Magazine.
- Bullet speed.
- Reload speed.

For stackable abilities, use a configured maximum level.

Example:

```text
MagazineSize:
Base = 6
Max = 10

Available upgrades:
+1 → 7
+1 → 8
+1 → 9
+1 → 10
```

Once the cap is reached, the ability becomes ineligible.

---

# 16. Upgrade Levels

Each ability should be represented by a level rather than a collection of unrelated upgrades.

Example:

```text
BulletSize

Level 0 = 100%
Level 1 = 125%
Level 2 = 150%
Level 3 = 180%
```

The Goddess may offer:

> Bigger Bullets — Level 2

This makes balancing much easier.

---

# 17. Smart Offer Generation

The Goddess should not simply randomly select two abilities.

Recommended pipeline:

```text
1. Build complete ability pool.
2. Remove ineligible abilities.
3. Remove already-maxed abilities.
4. Remove abilities blocked by prerequisites.
5. Score remaining candidates.
6. Apply category diversity rules.
7. Select two offers.
8. Validate that offers are meaningfully different.
```

---

# 18. Avoid Over-Optimizing the Player's Choices

The system should be smart, but not so smart that it always gives the mathematically perfect upgrade.

There should still be surprise.

Recommended approach:

`Eligibility = strict`

`Selection = weighted`

In other words:

- Never offer nonsense.
- Prefer useful abilities.
- Do not guarantee the best possible ability every time.

---

# 19. Pricing

Prices should be based on **impact**, not category.

Example:

| Ability | Example Cost |
|---|---:|
| +1 Life | 50 |
| Gem Magnet | 50 |
| Enemy Pushback | 70 |
| Faster Reload | 80 |
| Bigger Bullets | 90 |
| Faster Bullet | 80 |
| +1 Magazine | 100 |
| Max Life +1 | 120 |
| Strong Arena Counter | 100–150 |

These are starting values only.

The economy should be balanced after playtesting.

---

# 20. Refresh

Refresh lets the player discard both current offers and generate two new ones.

Example:

`REFRESH — 50 💎`

Recommended rules:

- Refresh is optional.
- Refresh cost is paid immediately.
- New offers cannot be identical to the discarded pair during the same visit.
- Consider increasing refresh cost after repeated use.

Possible pricing:

`50 → 75 → 100`

Alternatively, keep it at 50 for simplicity and limit the number of refreshes per Goddess visit.

---

# 21. Skip

Skip closes the shop without spending gems. Goddess performs the Disappear/Hide animation and the game resumes as usual.

No penalty.

The player may choose:

> "Neither upgrade is worth the gems."

This is important because it makes the player feel that the shop is offering choices rather than forcing a purchase.

---

# 22. Gem Economy

Gems collected during the current arena are **run currency**.

They are spent during Goddess visits.

Recommended rule:

`Run Gems reset when the arena/run ends.`

This makes each run economically meaningful.

The player must decide:

> Spend now or save for a stronger future offer?

---

# 23. Ability Application

Ability effects should be applied through a central `RunModifier/AbilityManager`.

Do not directly modify player/weapon values from the Goddess UI.

Example:

```text
Goddess
   ↓
AbilityManager
   ↓
RunModifier
   ├── PlayerStats
   ├── WeaponStats
   ├── ArenaInteractions
   └── Economy
```

This makes save/load, UI display, debugging and future upgrades much easier.

---

# 24. Ability Tags

Each ability should have metadata such as:

```text
Category:
General / Weapon / Arena / Synergy

Weapon:
Any / Handgun / Shotgun / Grenade

Arena:
Any / Luxury / Tropical / Toxic / Amazon / Cloud

Stackable:
true / false

MaxLevel:
3

Cost:
90

Rarity:
Common / Rare / etc.

Prerequisites:
...

Conflicts:
...
```

The Goddess selection system can then operate almost entirely from data.

---

# 25. Goddess Encounter Frequency

The number of appearances is arena-configured.

Example:

```text
Short Arena:
2 Goddess visits

Medium Arena:
3 visits

Long Arena:
4 visits
```

Exact timing is hidden from the player.

Avoid spawning her too close together.

Recommended:

`Minimum Goddess Interval = 90–120 seconds`

Adjust after playtesting.

---

# 26. Important UX Rule

The player should always understand:

1. What the ability does.
2. How much it costs.
3. Whether it is permanent for the run.
4. Whether it stacks.
5. What level they will reach.

Example:

> **BIGGER BULLETS II**
>
> Bullet size +25%
>
> `90 💎`
>
> Level 2 / 3

Keep the description short.

---
