# Economy & Progression Design

Reference spec for implementing score, coins, level progress and unlocks. Numbers are starting values — expose them in ScriptableObjects/config for tuning.

## Currencies & stats (keep it to these)

| Name | Scope | Earned by | Spent on |
|---|---|---|---|
| **Gems** (existing) | Run-scoped, lost at run end | Gem-dropper enemies | Abilities in mid-run shop only. **No conversion to coins.** |
| **Score** | Run-scoped stat (not a currency) | Kills, gem pickups | Nothing — converted to coins at run end, recorded in level progress |
| **Coins** (new) | Persistent, global | Run-end payout, coin pouch drops, IAP packs | Cosmetics, weapons |

No other currencies, no XP bar, no premium/hard currency. IAP coin packs buy the same coins.

## New Economy: Score

Per kill:
```
hitScore = (Σ baseValue of enemies killed) × (1 + 0.2·s + 0.5·(n − 1))
```
- `s` = current streak at kill time (0..6, max streak = 6). Streak grows on consecutive hits; any miss resets to 0.
- `n` = enemies killed by the same shot (multi-kill combo).
- `baseValue` per enemy type (lives on enemy data): e.g. idle and dropper 10, random move 15, chaser 20, shooter 25.
- Additive multipliers by design (max ≈ ×3.2 at s=6, n=3).
- **Gem pickup**: small flat score bonus (unmultiplied), e.g. +2.

### Score bubble

One bubble **per successful shot / hit**, not per enemy. It shows the total `hitScore` earned from everything that shot killed.

**Behaviour**
- Triggered when a shot resolves and kills ≥1 enemy (direct hit plus any secondary kills: shockwave, shotgun spread, launcher AoE, laser pierce). A shot that kills nothing spawns no bubble.
- All kills attributed to the same shot are collected first (a short resolution window, ~0.3s, or the weapon's own impact event), then scored once: `Σ baseValue × multiplier`, with `n` = number of enemies in that group.
- `s` is the streak **at the moment of the shot**, before this shot's own hit increments it. The streak increment happens after scoring.
- Spawn position: at the primary impact point (or the centroid of the killed enemies for multi-kills), nudged so it doesn't overlap the HUD.
- Animation: floats upward and fades over ~0.5s. Text is the final integer only, with no breakdown or `+` stacking.
- Pooled via `ObjectPoolManager` (new `PoolableItemType`); returned to pool when the animation ends.
- Color/size tier by `hitScore` (low / medium / high; thresholds configurable, size grows slightly with tier).
- The same value is added to the run's score total once. A gem pickup uses the same bubble style with its flat bonus.

**Example**: streak `s = 4`, player hits one enemy and the handgun shockwave kills 3 more, so `n = 4` enemies of base 10 each.
```
Σ baseValue = 4 × 10 = 40
multiplier  = 1 + 0.2·4 + 0.5·(4 − 1) = 1 + 0.8 + 1.5 = 3.3
hitScore    = 40 × 3.3 = 132   → one bubble showing "132" (high tier)
```
For comparison, a single kill at the same streak scores `10 × 1.8 = 18`.

**Streak display**: not near crosshair (crosshair is transient). Display on/around the player/floatie — TBD.

## Run end

Win/lose screen shows **current run** stats: score, highest streak, coins earned.

## New Economy: Coin

Coin payout (all granted on the Run-End summary screen):

| Source | Win | Lose |
|---|---|---|
| Score conversion `floor(score / 50)` | 100% | 50% |
| Level clear bonus | ✓ | – |
| First-clear bonus (one-time per level) | ✓ | – |
| Arena completion bonus (one-time per arena) | ✓ | – |
| Coin pouches picked up in-run | kept | kept |

Design intent: one-time progression bonuses ≫ score conversion, so replaying easy levels is a slow trickle (acceptable grind) and progressing is the best earner.
No per-arena coin rate; conversion rate is uniform for now. If needed, might change in the future.

The coin pouch picked gets credited immediately. Maybe a Coin wallet HUD is located above the Gem wallet HUD outside the screen bounds, and when coin is granted mid-run:
```
The Gem wallet HUD gets disabled -> the Coin wallet HUD slides down to display -> animates the increment in Coin count -> then slides back to original position -> Gem wallet HUD gets re-enabled. 
```
The remaining items from above like score conversion, level clear bonus, arena completion bonus, etc. can be calculated and granted at Run End screen.

## Coin pouch drops

- Dropped by gem-dropper rubber ducks with a small probability; grants 3 or 5 coins on pickup (reuse existing collectable flow).
- Probability and per-run cap defined in the dropper duck's config.
- No in-run coin HUD counter; total shown on the run-end summary.

## Level progress (persistent, per level)

```
LevelProgress { bool completed; int bestScore; int bestStreak; }
```
- Updated on win only (best-of comparison). First clear triggers first-clear bonus.
- Level card (level select) shows: completion status, all-time best score, all-time best streak.

## Unlockables

Each cosmetic item has a data-driven unlock type:
```
UnlockType { Free, Coins, ArenaClear, IAP, BundleOnly }   // + price / arena id / product id
```
- **Character heads & floaties**: pure art, many possible. Each arena completion unlocks one new head or floatie. Rest are coin-priced; some IAP, singly or in item bundles (e.g. a country-flag floatie pack).
- **Weapons**: unlocked via coins, arena clear or IAP, singly or in bundles. Weapons must stay sidegrades so real-money ones aren't pay-to-win.
- Starting coin price tiers (tune later): ~300 / 800 / 1500+.
- Locked items appear only in the main-menu Shop; the loadout screen lists what the player has unlocked.

