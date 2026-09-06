# Arena Selection & Level Specifications

## Arena Selection Menu Design

The arena selection interface uses a horizontal carousel/swipe system inspired by competitive arena games (e.g., Clash Royale).

* **Default Focus:** The menu automatically opens to the highest unlocked arena for the active player profile.
* **Layout Structure:**
  * **Center Stage:** A large 2D card showing a stylized preview of the selected pool (water texture, color theme, and icon).
  * **Navigation:** Left and Right directional arrows (or horizontal swipe gestures) allow players to cycle back through previously unlocked arenas.
  * **Locked State:** Future arenas display a dark silhouette with a padlock icon, accompanied by a unlock condition string (e.g., *"Clear Biohazard Basin Stage 5 to unlock"*).
* **Card Details (Per Arena):**
  * Arena Name
  * Visual Theme & Tint Preview
  * Enemy Roster Icons (displays known enemies in that pool)
  * "ENTER POOL" Action Button

---

## Arenas & Enemy Roster

### 1. The Shallow End (Default Pool)
* **Visual Theme:** Classic rectangular public pool featuring light blue tiles, lane markers, floating ropes, and a pool ladder.
* **Environmental Hazards:** None (serves as the mechanics learning ground).
* **Enemy Roster:**
  * **White Duck:** Base enemy. Idle behaviour.
  * **Orange Duck:** Random movements.
  * **Purple Duck:** Aggressive tracking behavior; directly chases the player.
  * **Red Duck:** Stationary or slow moving; periodically fires basic projectile bubbles at the player.
  * **Yellow Duck:** Rare reward enemy. High movement speed; drops gems/heart when hit.

---

### 2. The Ritz Ripples (Luxury Pool)
* **Visual Theme:** Nighttime city skyline backdrop, underwater pool lights, teak deck tiles, and luxury lounge chairs along the perimeter.
* **Environmental Hazards:** Ambient pool lights that flash, adding subtle visual clutter.
* **Enemy Roster:**
  * **Cosmetic Ducks:** Standard duck variants equipped with attached cosmetics (top hats, sunglasses, cigars, suit ties).
  * **Newspaper Duck:** Armored enemy type. 
    * *Stage 1:* Reading a newspaper (blocks first shot completely).
    * *Stage 2:* Drops newspaper upon hit and becomes a standard angry duck (1 additional hit to eliminate).

---

### 3. Cabana Carnage (Tropical Pool)
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

---

### 4. Biohazard Basin (Toxic Pool)
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

---

### 5. Flora Falls (Amazonian Forest Pool)
* **Visual Theme:** Stone pool embedded in a dense jungle canopy with overgrown moss, ancient rocks, and a background waterfall frame (play area remains a strict rectangle). Ducks wear standard natural cosmetics or none.
* **Environmental Hazards:** None (hazard weight shifts to native plant enemies).
* **Enemy Roster:**
  * **Edge Vines:** Spawns along the pool borders, growing procedurally along the edge splines to restrict player positioning along the walls.
  * **Flower Turret:** Drifts slowly or sits stationary in the pool.
    * When destroyed by player fire, it bursts into a 3-way or 4-way geometric pattern of sharp petal projectiles radiating outward.

---

### 6. Stratosphere Spa (Cloudy Pool)
* **Visual Theme:** High-altitude open pool resting on fluffy clouds with sky-blue water and ethereal lighting. All ducks feature tiny angelic wings.
* **Environmental Hazards:**
  * **Lightning Zone:** Dark storm cloud patches form on the water surface accompanied by a warning flash.
    * After a short delay, a lightning strike hits the center.
    * Players caught inside get electrocuted, losing 1 full life immediately.
* **Enemy Roster:**
  * **Foam Weaver:** Spawns and expands to create dense foam walls across the arena.
    * *Wall Control:* Shooting foam shrinks it step-by-step until destroyed.
    * *Buff Mechanic:* Enemies passing through intact foam gain a white foam coating, granting them +1 additional hit point armor shield.
