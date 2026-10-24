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

**Score bubble** (on each kill): pooled via `ObjectPoolManager` (new `PoolableItemType`). Shows only the final `hitScore`, floats upward briefly (~0.5s) and fades. Color-coded by tier: low / medium / high gain (thresholds configurable). No breakdown shown.

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
UnlockType { Free, Coins, ArenaClear, IAP }   // + price / arena id / product id
```
- **Character heads & floaties**: pure art, many possible. Each arena completion unlocks one new head or floatie. Rest are coin-priced; some IAP (e.g. country-flag floatie pack).
- **Weapons**: unlocked via coins or arena clear — **never IAP** (avoid pay-to-win). Weapons are sidegrades.
- Starting coin price tiers (tune later): ~300 / 800 / 1500+.

