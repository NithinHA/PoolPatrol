---
name: enemy-spawning
description: Everything about PoolPatrol's arena spawn system — how waves and cooldown phases work, how the progress bar advances, gem and heart dropper economy, the ScriptableObject data model, and how to configure or extend the system. Use this skill whenever the task involves spawning logic, arena pacing, wave design, enemy pressure/budget tuning, the progress bar, or gem/heart dropper behaviour. Also read this before touching ArenaDirector, ArenaSpawnConfigSO, CooldownSectionSO, WaveSectionSO, SpawnSectionSO, or ArenaProgressBar.
---

# Enemy Spawning System

The arena spawn system replaces the old timer-based `Spawner.cs`. It is data-driven (ScriptableObjects per arena), self-regulating via a live threat budget, and decouples combat spawning from the reward economy entirely.

## Key files

| File | Namespace | Role |
|------|-----------|------|
| `Assets/Scripts/SpawningLogic/ArenaDirector.cs` | `SpawningLogic` | MonoBehaviour that drives the arena. Walks sections, spawns enemies, tracks progress, fires `OnArenaComplete`. |
| `Assets/Scripts/SpawningLogic/ArenaSpawnConfigSO.cs` | `SpawningLogic` | ScriptableObject holding the ordered section list + reward budget. One asset per arena. |
| `Assets/Scripts/SpawningLogic/SpawnSectionSO.cs` | `SpawningLogic` | Abstract base SO. Holds the enemy bucket, concurrency cap, spawn interval range, and pressure bias. |
| `Assets/Scripts/SpawningLogic/CooldownSectionSO.cs` | `SpawningLogic` | Concrete section — a timed stretch. Advances the progress bar. |
| `Assets/Scripts/SpawningLogic/WaveSectionSO.cs` | `SpawningLogic` | Concrete section — a fixed-count wave. Pauses the bar. |
| `Assets/Scripts/SpawningLogic/WeightedEnemy.cs` | `SpawningLogic` | `[Serializable] struct { EnemyController Prefab; float Weight; }` used in buckets. |
| `Assets/Scripts/UI/HUD/ArenaProgressBar.cs` | `UI.HUD` | Fills a `Image.fillAmount` bar; places and updates `WaveMarker` instances. |
| `Assets/Scripts/UI/HUD/WaveMarker.cs` | `UI.HUD` | Individual checkpoint dot. Three states: pending → pulsing (DOTween) → cleared (checkmark). |

---

## Section model

An `ArenaSpawnConfigSO` holds an ordered `List<SpawnSectionSO>`. Two concrete types:

### CooldownSectionSO — calm stretches

- **Duration** (`float`, seconds) — how long the section runs before moving on. Time-based, not kill-based.
- **ProgressWeight** (`float`) — how much of the progress bar this section fills, *independent of Duration*. The bar shows `weight_so_far / total_weight`, so a 10s warmup can take 5% of the bar while a 60s mid-section takes 30%. Only cooldowns contribute bar width; waves contribute zero.
- **DifficultyCurve** (`AnimationCurve`, 0→1 over normalised time) — shapes the pressure ramp. Linear by default (starts easy, ends hard). Curve it for lulls or spikes mid-section.
- **MinThreatBudget / MaxThreatBudget** (`float`) — the on-screen threat target lerped by pressure. Spawning pauses whenever `CurrentThreat() >= ThreatBudgetAt(pressure)`, and resumes as the player kills enemies.

### WaveSectionSO — checkpoint waves

- **EnemyCount** (`int`) — total enemies spawned before the wave is considered "fully launched".
- **SpawnBurstSize** (`int`) — enemies spawned per interval tick. Smaller = more staggered, larger = more wall-of-enemies feel.
- **IsBoss** (`bool`) — semantic flag; no mechanical effect yet, used for future boss-phase logic.
- **MaxConcurrentEnemies / SpawnIntervalMin / SpawnIntervalMax** — same concurrency cap and interval range as cooldowns but tuned for burst pacing.

Wave sections do **not** advance the bar. They instead show a pulsating marker at the bar position where the wave falls. The wave must be fully launched **and** every enemy in the arena cleared before play resumes.

---

## How ArenaDirector runs an arena

```
Start()
  ├── compute _totalProgressWeight (sum of all CooldownSectionSO.ProgressWeight)
  ├── BuildProgressMarkers()  — places one WaveMarker per wave at its cumulative-weight position
  └── StartCoroutine(RunArena()) + StartCoroutine(RunRewardDirector())

RunArena()  — iterates m_Config.Sections in order
  ├── CooldownSectionSO → RunCooldownSection(startProgress, endProgress)
  └── WaveSectionSO     → RunWaveSection(markerIndex)

RunArena() end → _arenaProgress = 1f, OnArenaComplete fired
```

### RunCooldownSection

Each frame:
1. Advance `elapsed`, compute `normalizedTime = elapsed / section.Duration`.
2. Set `_arenaProgress = Lerp(startProgress, endProgress, normalizedTime)` and push to the progress bar.
3. Compute `pressure = section.PressureAt(normalizedTime)` (evaluates DifficultyCurve).
4. **Threat gate**: if `CurrentThreat() >= section.ThreatBudgetAt(pressure)` or `activeEnemies.Count >= MaxConcurrentEnemies`, skip spawning this frame.
5. Decrement spawn timer; when it hits zero, call `TrySpawnEnemy(section, pressure)`, reset timer.

The section ends when `elapsed >= section.Duration` — it does **not** wait for enemies to die.

### RunWaveSection

1. Sets the marker to pulsing.
2. Spawns enemies in bursts until `spawned >= section.EnemyCount`. Pressure ramps as `spawned / (EnemyCount - 1)` so the wave builds in difficulty.
3. **No threat-budget gate** — waves are relentless and always spawn their full count.
4. After all enemies are spawned, waits until `_activeEnemies.Count == 0`.
5. Sets marker to cleared, resumes the section loop.

### TrySpawnPrefab

Attempts 10 random positions within `EnvironmentConstants.SpawnWidth/Height`. A position is valid if every currently-active enemy is at least `m_EnemySpacing` away from it (distance check against `_activeEnemies` list — **not** `Physics2D.OverlapCircle`, which hits walls, ground, and the player).

Spawning is deferred by `m_SpawnEffectDelay` seconds while the spawn VFX plays, then `Instantiate(prefab)` and add to `_activeEnemies`.

### EnemyController bookkeeping

- `EnemyController.OnAnyEnemyDied` (static `Action<EnemyController>`) — `ArenaDirector` subscribes in `OnEnable`, unsubscribes in `OnDisable`. On death, the enemy is removed from `_activeEnemies`.
- `EnemyController.ThreatCost` (`float`) — the difficulty rating used by `CurrentThreat()` and the pressure-biased selection. Set per-prefab. Typical values: idle=1, randomMove=2, chase=4, shooter=5.

---

## Pressure-biased enemy selection

`SpawnSectionSO.PickWeightedEnemy(pressure01)`:

1. Finds the min and max `ThreatCost` within the bucket.
2. Normalises each enemy's cost to `hardness ∈ [0, 1]` within this bucket.
3. Computes a **triangular affinity**: `affinity = 1 - |hardness - pressure|`. Enemy matches current pressure → affinity ≈ 1; opposite end → affinity ≈ 0. Floor of 0.05 so no enemy fully starves.
4. Blends with the designer's base `Weight` via `PressureBias` (0–1 on the section): `effectiveWeight = baseWeight * Lerp(1, affinity, PressureBias)`.
5. Standard weighted random roll.

**What this means in practice**: early in a section (low pressure) light enemies dominate; as pressure rises, tougher enemies become increasingly likely — automatically, using only the enemies already in the bucket. Setting `PressureBias = 0` disables this and falls back to pure designer weights.

---

## Progress bar

`ArenaProgressBar` holds a single `Image` (fill mode) plus a `List<WaveMarker>`.

- `BuildMarkers(List<float> positions01)` — instantiates one `WaveMarker` prefab per wave, anchored to the bar at the given normalised position.
- `SnapTo(float progress01)` — immediately sets `Image.fillAmount` (no tween; the bar moves every frame during cooldowns so tweening would lag).
- `SetMarkerActive(int index)` / `SetMarkerCleared(int index)` — delegates to the marker.

`WaveMarker` states:
- **Pending** — static dot, visible on the bar.
- **Pulsing** — DOTween yoyo scale loop, indicating the active wave.
- **Cleared** — dot hidden, checkmark shown; DOTween killed.

The bar never jumps. Waves sit at the exact position reached by the preceding cooldown, hold there while the wave plays, then the next cooldown continues from the same position.

---

## Reward economy (decoupled from combat)

`ArenaSpawnConfigSO.RewardBudget` holds all reward config. `ArenaDirector.RunRewardDirector()` runs as a parallel coroutine alongside the section loop.

### Gem droppers — budget-driven

Goal: deliver exactly `TargetGemDropperCount` gem droppers across the run, spread proportionally by arena progress.

Each frame (cooldowns only; gated by `!_inWave`):
- `dueByNow = Ceil(TargetGemDropperCount * _arenaProgress)`
- If `gemsSpawned < dueByNow`, attempt a spawn and increment `gemsSpawned`.
- A `GemMinInterval` timestamp guard prevents clumping.

Since `_arenaProgress` only advances during cooldowns, gems naturally land in calmer moments. The player receives a predictable total, making ability affordability tuneable by design.

### Heart droppers — hurt-duration gated

Hearts spawn only when the player has sustained damage for long enough to "earn" relief. The required wait time scales with how hurt they are.

**Fields on `RewardBudget`:**
- `BarelyHurtDelay` (default 60s) — seconds to wait when only 1 life is missing (least urgent).
- `CriticalDelay` (default 10s) — seconds to wait when on the last life (most urgent).
- `PostSpawnCooldown` (default 45s) — after a heart spawns, the hurt timer freezes for this long before it can accumulate again. Prevents back-to-back spawns.
- `MaxHeartDroppers` — hard cap on total heart spawns for the run.

**Logic (each frame):**
1. Compute `missingRatio = (maxHealth - currentHealth) / (maxHealth - 1)` → 0 at full health, 1 at last life.
2. If `missingRatio == 0`, reset `hurtTimer = 0`.
3. If `postSpawnCooldown > 0`, decrement it and skip (struggle window).
4. Otherwise, increment `hurtTimer`. Required delay = `Lerp(BarelyHurtDelay, CriticalDelay, missingRatio)`.
5. If `hurtTimer >= requiredDelay`, spawn a heart dropper, reset timer, start `postSpawnCooldown`.

Hearts are allowed during waves (relief matters most there). Gems are cooldown-only.

---

## Wiring the system in the scene

1. **ArenaDirector** — MonoBehaviour on a scene GameObject.
   - `m_Config` → assign an `ArenaSpawnConfigSO` asset.
   - `m_ProgressBar` → assign the `ArenaProgressBar` component in the HUD.
   - `m_SpawnEffectDelay` — seconds before enemy appears after VFX (default 1s).
   - `m_EnemySpacing` — minimum distance between spawn point and existing enemies (default 1.5).
2. **LevelManager** — subscribe to `m_ArenaDirector.OnArenaComplete += OnLevelComplete` to trigger win state.
3. **ArenaProgressBar** — child of the In-Game Canvas. Needs an `Image` (fill left-to-right) and a WaveMarker prefab slot.
4. **WaveMarker prefab** — two child GameObjects: `m_DotVisual` and `m_CheckVisual`. DOTween required for the pulsing animation.

---

## Creating an ArenaSpawnConfigSO

Right-click in the Project window → `Create > PoolPatrol > Spawning > Arena Spawn Config`.

Add sections to the `Sections` list in order. Mix `CooldownSection` and `WaveSection` assets (create via `Create > PoolPatrol > Spawning > Cooldown Section` / `Wave Section`).

**Recommended structure:** Cooldown → Wave → Cooldown → Wave → ... ending with a finale wave. A run targeting 12–15 minutes typically uses 4 cooldowns (45–60s each) and 4 waves (8–18 enemies each), with `ProgressWeight` distributed non-proportionally to the durations for pacing feel.

**Threat budget tuning guide:**
- Warmup cooldown: `MinThreatBudget 2`, `MaxThreatBudget 6` — very light.
- Mid cooldowns: `Min 5–8`, `Max 14–20` — ramps to a meaningful crowd.
- Final cooldown: `Min 10`, `Max 24` — sustained heavy pressure.

**PressureBias**: 0.75 is a good default. Lower it (toward 0) if you want enemy type distribution to feel more random/weighted by designer values. Raise it (toward 1) if you want enemy type to track difficulty almost exclusively.

---

## Extending the system

**New section type**: subclass `SpawnSectionSO`, add a `[CreateAssetMenu]`, handle the new type in `ArenaDirector.RunArena()` with a new `case`.

**Adjusting reward pacing**: tune `TargetGemDropperCount`, `GemMinInterval`, `BarelyHurtDelay`, `CriticalDelay`, and `PostSpawnCooldown` on the `RewardBudget` in the config asset — no code changes needed.

**Adding a new enemy to a section's bucket**: add a `WeightedEnemy` entry to the section's `EnemyBucket` list and set a `ThreatCost` on the prefab's `EnemyController`. The pressure-biased selection handles the rest automatically.
