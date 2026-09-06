# Arena Enemy Spawning & Progression

## 1. Core Loop

`Preparation → Wave → Clear → Preparation → Wave → … → Final Encounter`

Preparation is not empty: enemies trickle in at low intensity while arena progress advances. During an active wave, spawning becomes burst-based and **progress pauses until the wave is cleared**.

### Arena states
- **Preparation:** progress advances; low-rate enemy trickle; recovery window.
- **Wave Active:** progress frozen; burst spawning; increasing enemy combinations; ends only when the wave's spawn requirement is fulfilled and required enemies are defeated.
- **Final Encounter:** normal spawning stops, minor enemies resolve, boss/final encounter begins, then arena completes.

---

## 2. Segmented Progress Bar

The bar represents **arena completion**, not elapsed time.

`START ──●────●────●────●────★── FINISH`

Markers communicate major progression without revealing exact wave or Goddess timing.

Progress advances during Preparation and freezes during Wave Active.

Example arena configuration:

| Segment | Progress | Enemy Tier |
|---|---:|---|
| 1 | 0–10% | Idle |
| 2 | 10–22% | Idle + Random |
| 3 | 22–36% | + Chaser |
| 4 | 36–52% | + Shooter |
| 5 | 52–68% | Mixed |
| 6 | 68–85% | Mixed + Arena Special |
| 7 | 85–100% | Final/Boss |

---

## 3. Enemy Progression

Enemies should be introduced progressively rather than randomly from the start.

```text
Tier 0: Idle
Tier 1: Idle + Random
Tier 2: Idle + Random + Chaser
Tier 3: Idle + Random + Chaser + Shooter
Tier 4: Full combination / arena specials
```

Arena specials can be hinted at or sneaked as a surprise sometime early on.

Each wave profile defines eligible enemies and selection weights. Weights are preferences, not guarantees; population and threat limits still apply.

### Wave intensity
Each wave follows:

**Opening → Build-up → Peak → Resolution**

This produces the desired rhythm:

**calm → pressure → chaos → clear**

---

## 4. Threat & Population Control

Each enemy has a hidden `ThreatCost`.

| Enemy | Threat |
|---|---:|
| Idle Duck | 1 |
| Random Duck | 1 |
| Chaser Duck | 2 |
| Shooter Duck | 3 |
| Starfish | 2 |
| Crab | 3 |
| Foam | 4 |
| Lightning Zone | 4 |

The spawner checks both:

```text
AliveCount < MaxEnemiesAlive
AND
CurrentThreat + NewThreat <= AllowedThreat
```

This prevents dangerous combinations such as too many shooters even when the raw enemy count is acceptable.

If a desired spawn cannot happen because the arena is full, record **Spawn Debt** and pay it down as enemies are defeated instead of discarding it.

---

## 5. Spawn Behaviour & Safety

### Preparation
Use a low-rate trickle:

`Spawn → wait → Spawn → wait → …`

Rate responds to current enemy count, threat, desired intensity and recent spawning.

### Active Wave
Use bursts whose size, interval and composition are configurable:

```text
Burst 1: Idle + Idle + Random
Burst 2: Random + Chaser
Burst 3: Idle + Chaser + Shooter
```

Spawn locations should consider:

- Distance from player.
- Player velocity/direction.
- Arena boundaries.
- Enemy density.
- Line of sight.
- Active hazards.

Avoid cheap spawns directly beside/behind the player. Fast-moving players should generally encounter threats ahead of their movement path.

---

## 6. Player-Aware Difficulty

Use player state as a **soft modifier**, not constant rubber-banding.

Useful inputs:

- Current/max lives.
- Weapon and upgrades.
- Recent damage.
- Recent kill rate.
- Current enemy count.

If the player is struggling, slightly reduce incoming threat and improve future recovery eligibility. If dominating, allow a somewhat higher threat target or stronger combinations.

The goal is to maintain combat rhythm rather than secretly force a win/loss.

---

## 7. Reward Enemies

Reward enemies use existing movement behaviours but are governed by separate reward budgets.

### GemDropper

Example:

`500 gems maximum per arena`

Rules:
- Can reuse RandomMove or another existing behaviour.
- Has limited spawn frequency.
- Respects `MaxUncollectedGems` (e.g. 20–30).
- Becomes ineligible when the arena gem budget is exhausted.
- Drops should be distributed across the run.

Example eligibility:

```text
GemBudgetRemaining > 0
AND
UncollectedGems < MaxUncollectedGems
AND
MinimumDropInterval elapsed
```

### ExtraLifeDropper

Eligible only when:

`CurrentLives < MaxLives`

After the player loses a life, start a configurable **LifeDropCooldown** (e.g. 90 seconds). The director can increase life-drop priority when the player is critically low, but should not guarantee immediate recovery.

This prevents:

`Lose life → immediate life → no meaningful consequence`

---

## 8. Arena Hazards

Environmental mechanics should be separate encounters rather than hard-coded into the normal duck spawner.

Examples:

- **Toxic Pool:** circular zones, duration/radius, max simultaneous zones.
- **Vines:** perimeter spawn, spline growth, occupy space until destroyed.
- **Foam:** expands from a point, can be damaged, coats enemies that pass through.
- **Lightning:** warning/telegraph → strike → cooldown.

The **Arena Director decides when** a hazard can appear; its own controller handles behaviour.

---

## 9. Data-Driven Arena Configuration

Tuning should not require code changes.

### Progress
- Segment count and ranges.
- Preparation duration/rate.
- Wave profiles.

### Enemy progression
- Unlock order.
- Enemy weights.
- Threat costs.
- Max enemies.
- Burst size/interval.
- Intensity curves.

### Rewards
- Gem budget.
- GemDropper rules.
- Max uncollected gems.
- Life-drop count/cooldown.
- Other reward budgets.

### Hazards
- Unlock segment.
- Spawn weight.
- Threat cost.
- Max active instances.
- Duration.

### Final encounter
- Boss unlock point.
- Boss configuration.

---
