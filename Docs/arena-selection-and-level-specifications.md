# Arena Selection & Level Specifications

## Arena Selection Menu Design

The arena selection interface uses a horizontal carousel/swipe system inspired by competitive arena games (e.g., Clash Royale).

* **Default Focus:** The menu automatically opens to the highest unlocked arena for the active player profile.
* **Layout Structure:**
  * **Center Stage:** A large 2D card showing a stylized preview of the selected pool (water texture, color theme, and icon).
  * **Navigation:** Left and Right directional arrows (or horizontal swipe gestures) allow players to cycle back through previously unlocked arenas.
  * **Locked State:** Future arenas display a dark silhouette with a padlock icon, accompanied by a unlock condition string (e.g., *"Clear The Shallow End to unlock"*).
* **Card Details (Per Arena):**
  * Arena Name
  * Visual Theme & Tint Preview
  * Level completion progress (e.g., "3/5" stars or checkmarks)
  * "SELECT" Action Button → opens the Level Selection screen for that arena

---

## Level Selection Screen

Selecting an arena opens a dedicated level selection screen (inspired by Angry Birds Space / Plants vs. Zombies 2).

* **Layout:** A grid or horizontal row of 5 level nodes per arena, connected by a dotted path.
* **Level States:**
  * **Completed:** Filled star/checkmark. Tappable to replay.
  * **Current (Unlocked):** Highlighted, pulsing node. Tappable to play.
  * **Locked:** Greyed-out with a padlock. Displays *"Complete Level X to unlock"*.
* **Per-Level Info (on tap/hover):**
  * Level number (e.g., "The Shallow End — Level 3")
  * Duration estimate (8–15 min)
  * Enemy roster preview (icons of enemies introduced or featured)
  * "ENTER POOL" Action Button → loads the Game scene with the selected level config
* **Back Button:** Returns to the Arena Selection carousel.

### Progression Rules

* Levels within an arena unlock sequentially — Level 2 requires completing Level 1, etc.
* Completing all 5 levels of an arena unlocks the next arena.
* Players can replay any completed level.
* Each arena provides 40–75 minutes of total gameplay across its 5 levels.

### Level Design Principles

Each level within an arena shares the same visual theme, pool environment, and hazard set. Levels escalate by:

1. **Enemy Roster Expansion:** Early levels introduce 1–2 enemy types; later levels layer in the full roster.
2. **Wave Intensity:** More waves, larger spawn bursts, tighter cooldowns.
3. **Threat Budget Growth:** Higher concurrent enemy caps and threat budgets per wave.
4. **Hazard Frequency:** Environmental hazards appear more often or cover more area in later levels.
5. **Duration:** Earlier levels run shorter (~8 min); later levels push toward the 15 min cap.

---

## Arenas & Enemy Roster

### 1. The Shallow End (Default Pool) — 5 Levels
* **Visual Theme:** Classic rectangular public pool featuring light blue tiles, lane markers, floating ropes, and a pool ladder.
* **Environmental Hazards:** None (serves as the mechanics learning ground).
* **Enemy Roster:**
  * **White Duck:** Base enemy. Idle behaviour.
  * **Orange Duck:** Random movements.
  * **Purple Duck:** Aggressive tracking behavior; directly chases the player.
  * **Red Duck:** Stationary or slow moving; periodically fires basic projectile bubbles at the player.
  * **Yellow Duck:** Rare reward enemy. High movement speed; drops gems/heart when hit.
* **Level Progression:**
  * **Level 1:** White & Orange ducks only. Tutorial pacing. (~8 min)
  * **Level 2:** Introduces Purple Duck. Faster spawn intervals. (~10 min)
  * **Level 3:** Introduces Red Duck. Mixed wave compositions. (~12 min)
  * **Level 4:** Full roster active. Higher threat budgets. (~13 min)
  * **Level 5:** All enemies, max intensity waves, Yellow Duck bonus rounds. (~15 min)

---

### 2. The Ritz Ripples (Luxury Pool) — 5 Levels
* **Visual Theme:** Nighttime city skyline backdrop, underwater pool lights, teak deck tiles, and luxury lounge chairs along the perimeter.
* **Environmental Hazards:** Ambient pool lights that flash, adding subtle visual clutter.
* **Enemy Roster:**
  * **Cosmetic Ducks:** Standard duck variants equipped with attached cosmetics (top hats, sunglasses, cigars, suit ties).
  * **Newspaper Duck:** Armored enemy type. 
    * *Stage 1:* Reading a newspaper (blocks first shot completely).
    * *Stage 2:* Drops newspaper upon hit and becomes a standard angry duck (1 additional hit to eliminate).
* **Level Progression:**
  * **Level 1:** Cosmetic Ducks only, light flashing. (~8 min)
  * **Level 2:** Introduces Newspaper Duck. (~10 min)
  * **Level 3:** Mixed Cosmetic + Newspaper waves, increased flash frequency. (~12 min)
  * **Level 4:** Dense spawns, multiple Newspaper Ducks per wave. (~13 min)
  * **Level 5:** Full intensity, rapid flash hazards, boss-tier wave count. (~15 min)

---

### 3. Cabana Carnage (Tropical Pool) — 5 Levels
* **Visual Theme:** Beachfront resort pool with warm turquoise water, sandy borders, palm trees, and tropical cosmetics (garlands, straw hats).
* **Environmental Hazards:** 
  * **Coconut:** Neutral physics object sitting in the pool.
    * Invulnerable to player/enemy damage.
    * When shot, propels violently in the opposite direction, acting as a high-damage physical projectile against enemies.
    * Cracks visually on each hit and shatters completely on the 3rd shot.
* **Enemy Roster:**
  * **Perimeter Crab:** Patrols the outer tile rim of the pool.
    * *Invulnerable* to direct front/back shots while idling.
    * When the player floats near, it stretches its claws out to snap.
    * *Vulnerability:* Shooting the extended claws destroys them individually. Destroying both claws causes the crab to retreat from the arena.
  * **Starfish:** Floats idly in the water column.
    * *Mitosis Mechanic:* Upon receiving a lethal hit, it splits into 3 small, faster-moving starfish.
* **Level Progression:**
  * **Level 1:** Starfish only, 1 coconut in pool. (~8 min)
  * **Level 2:** Introduces Perimeter Crab. (~10 min)
  * **Level 3:** Mixed roster, multiple coconuts. (~12 min)
  * **Level 4:** Dense crab patrols + starfish swarms. (~13 min)
  * **Level 5:** Full chaos, max coconuts, overlapping crab/starfish waves. (~15 min)

---

### 4. Biohazard Basin (Toxic Pool) — 5 Levels
* **Visual Theme:** Murky green water, cracked slimy tiles, caution tape around the rim, and glowing barrels nearby. Ducks feature zombie skins (stitched mouth, single eye, missing head sections).
* **Environmental Hazards:**
  * **Toxic Zone:** A circular green chemical spill that spawns randomly and vanishes after a set timer.
    * Bullets pass straight through without destroying it.
    * Entering the zone triggers a green player filter and fills an infection meter.
    * When the meter fills completely, the player takes rapid continuous damage (oxygen loss mechanic).
* **Enemy Roster:**
  * **Zombie Duck:** 
    * *Stage 1:* First hit pops the inflated head section.
    * *Stage 2:* Body continues moving aggressively; second hit eliminates it.
  * **Zombie Hand:** Emerges directly from beneath the water surface.
    * Multi-hit health visualizer: Starts with 5 fingers.
    * Each hit snaps off 1 finger. Destroyed on the 5th consecutive hit.
* **Level Progression:**
  * **Level 1:** Zombie Ducks only, rare toxic zone spawns. (~8 min)
  * **Level 2:** Introduces Zombie Hand. (~10 min)
  * **Level 3:** Frequent toxic zones, mixed enemy waves. (~12 min)
  * **Level 4:** Overlapping toxic zones, dense zombie spawns. (~13 min)
  * **Level 5:** Max hazard coverage, full roster at peak intensity. (~15 min)

---

### 5. Flora Falls (Amazonian Forest Pool) — 5 Levels
* **Visual Theme:** Stone pool embedded in a dense jungle canopy with overgrown moss, ancient rocks, and a background waterfall frame (play area remains a strict rectangle). Ducks wear standard natural cosmetics or none.
* **Environmental Hazards:** None (hazard weight shifts to native plant enemies).
* **Enemy Roster:**
  * **Edge Vines:** Spawns along the pool borders, growing procedurally along the edge splines to restrict player positioning along the walls.
  * **Flower Turret:** Drifts slowly or sits stationary in the pool.
    * When destroyed by player fire, it bursts into a 3-way or 4-way geometric pattern of sharp petal projectiles radiating outward.
* **Level Progression:**
  * **Level 1:** Edge Vines only, slow growth rate. (~8 min)
  * **Level 2:** Introduces Flower Turret. (~10 min)
  * **Level 3:** Faster vine growth, multiple turrets active. (~12 min)
  * **Level 4:** Dense vine coverage forcing center play, turret crossfire. (~13 min)
  * **Level 5:** Max vine + turret density, minimal safe zones. (~15 min)

---

### 6. Stratosphere Spa (Cloudy Pool) — 5 Levels
* **Visual Theme:** High-altitude open pool resting on fluffy clouds with sky-blue water and ethereal lighting. All ducks feature tiny angelic wings.
* **Environmental Hazards:**
  * **Lightning Zone:** Dark storm cloud patches form on the water surface accompanied by a warning flash.
    * After a short delay, a lightning strike hits the center.
    * Players caught inside get electrocuted, losing 1 full life immediately.
* **Enemy Roster:**
  * **Foam Weaver:** Spawns and expands to create dense foam walls across the arena.
    * *Wall Control:* Shooting foam shrinks it step-by-step until destroyed.
    * *Buff Mechanic:* Enemies passing through intact foam gain a white foam coating, granting them +1 additional hit point armor shield.
* **Level Progression:**
  * **Level 1:** Foam Weaver only, rare lightning zones. (~8 min)
  * **Level 2:** Increased foam density, more frequent lightning. (~10 min)
  * **Level 3:** Multiple weavers active, lightning zones overlap. (~12 min)
  * **Level 4:** Rapid foam expansion, buffed enemy waves. (~13 min)
  * **Level 5:** Full intensity — dense foam walls, constant lightning, max spawns. (~15 min)
