# Pool visuals: making the arena look 3D

The gameplay mockup reads as 3D, but none of it needs real 3D. It is a stack of flat layers, each
faking one physical cue: depth, light through water, height and contact. This guide explains that
stack, what is already built in the project, how to tune it, and how to make the remaining art
with an image generator.

---

## 1. Why the mockup looks 3D

Each cue in the mockup, and how the project fakes it:

| Cue in the mockup | What your eye reads | How it's faked |
|---|---|---|
| Far pool wall visible, near wall hidden | Camera is tilted a few degrees | `PoolFloor` draws a band of "wall face" tiles inside the outline: deep at the top, thin on the sides, none at the bottom |
| Darker floor where it meets the walls | Contact occlusion / depth | Edge AO in `PoolFloor` |
| Shadow inside the top/left rim | The rim stands above the water | `PoolFloor` tests whether a step towards the sun leaves the pool (SDF lookup) |
| Bright moving web on the floor | Light focused by the waves | Baked tileable caustics texture, sampled twice and refracted |
| Floor wobbles under moving objects | Refraction | Floor sampled through the slope of the existing ripple sim |
| Rings that are bright on one side and dark on the other | Relief on the surface | `PoolSurface` lights the ripple slope from the sun |
| Foam line and lapping bands along the walls | Water meets a wall | `PoolSurface` edge bands |
| Blob of shade under every duck, offset | Objects float *above* the floor | `WaterContactFX` adds an underwater shadow on the `Water` sorting layer, below the surface |
| Bright ring around each duck | Waterline contact | `WaterContactFX` adds a procedural ring |
| Rounded, lit stone coping | Raised edge | `PoolDeck` lights the coping as a bullnose |
| Plants and props overhanging the frame | Things nearer the camera | Foreground sprites on the `Foreground` sorting layer, plus `HeightParallax` |

The one rule that makes it all cohere: **a single sun direction.** `PoolEnvironment.m_SunDirection`
drives the rim shadow, the object shadows, the ripple lighting and the coping highlight. Any art you
add must be lit from the same side: light coming from the **top, slightly left**.

---

## 2. Layer stack (bottom to top)

```
Sorting layer   Order   Object                              Shader / component
-------------   -----   ---------------------------------   ------------------------------
(opaque)          -     Bg/PoolFloor (quad, z = 6.5)        PoolPatrol/PoolFloor
Water             0     <object>/UnderwaterShadow (runtime) PoolPatrol/UnderwaterShadow
Water            10     Bg/Background (ripple plane)        PoolPatrol/PoolSurface  + RippleEffect
Water            20     <object>/WaterlineRing (runtime)    PoolPatrol/WaterlineRing
Water            30     Bg/PoolDeck (quad)                  PoolPatrol/PoolDeck
Default           *     ducks, lily pads, pickups, bullets, deck decor
Player            *     player
Foreground        *     overhanging leaves (add these)      HeightParallax
UI                      HUD
```

The old tile sprites under `Bg/Edge Colliders` are still there with their renderers disabled. Their
colliders are still the walls and the Cinemachine confiner. **`PoolEnvironment` fits the water to
those colliders**, so moving a wall moves the visuals too.

---

## 3. What was added

| File | Role |
|---|---|
| `Assets/Materials/Shaders/Water/PoolCommon.hlsl` | Shared globals: pool SDF, sun, ripple sampling, noise |
| `Assets/Materials/Shaders/Water/PoolFloor.shader` | Floor tiles, wall face, caustics, refraction, rim shadow, depth tint. The static part is baked at load (see §7) |
| `Assets/Materials/Shaders/Water/PoolSurface.shader` | Glints, ripple lighting, edge foam and lapping |
| `Assets/Materials/Shaders/Water/PoolDeck.shader` | Coping and deck (procedural, or your texture) |
| `Assets/Materials/Shaders/Water/UnderwaterShadow.shader` | Blurred, wobbling silhouette shadow |
| `Assets/Materials/Shaders/Water/WaterlineRing.shader` | Contact line plus spreading rings |
| `Assets/Scripts/Water/PoolEnvironment.cs` | Pool shape and sun, published as shader globals (runs in edit mode); floor bake and quality tier |
| `Assets/Scripts/Water/WaterContactFX.cs` | Per-object shadow, ring and optional bob |
| `Assets/Scripts/Water/HeightParallax.cs` | Fake height for decor and foreground |
| `Assets/Scripts/Water/RippleEffect.cs` | Now also publishes the ripple sim globally (`_PoolRippleTex`, `_PoolRippleVP`) |
| `Assets/Scripts/Editor/PoolVisualsWiring.cs` | `PoolPatrol > Pool Visuals` menu; every step is re-runnable |
| `Assets/Materials/Water/*.mat`, `Textures/Caustics.png`, `Textures/SoftCircle.png` | Generated materials and textures |
| `Assets/Settings/PoolGameplayProfile.asset` | The Game scene's own volume profile: soft bloom, light vignette, a little saturation |

`WaterContactFX` was added to every `RubberDuck_*` prefab, both pickups, `DefaultFloatie` (its old
`Reflection` child is now disabled) and the scene's lily pads.

**When you add a new enemy or floating prop,** add `WaterContactFX` to it, or re-run
`PoolPatrol > Pool Visuals > 4. Add Water FX To Prefabs` after adding the prefab path to
`DuckPrefabs` / `PickupPrefabs`.

---

## 4. Tuning cheat-sheet

All of these are material or inspector values, so you can tweak them live in Play mode. Material
changes persist after you stop.

| Want | Change |
|---|---|
| Bigger or smaller caustic cells | `PoolFloor` > Caustics Tile Size (8) |
| Calmer caustics | `PoolFloor` > Caustics Color alpha (0.42), Caustics Speed (0.08) |
| More "deep pool" | `PoolFloor` > Deep Color alpha (0.45), Distance To Full Depth (5) |
| Taller-looking walls | `PoolFloor` > Face Depth x (top) and y (sides) |
| Stronger rim shadow | `PoolEnvironment` > Rim Shadow Length; `PoolFloor` > Rim Shadow Strength |
| Ripples refract more | `PoolFloor` > Ripple Refraction (5); `PoolSurface` > Ripple Slope Scale (35) |
| Sparklier water | `PoolSurface` > Glint Threshold (lower) / Glint Strength |
| Object shadows further away (objects look higher) | `PoolEnvironment` > Object Shadow Distance (0.35), or per object `WaterContactFX` > Shadow Height |
| Darker or bluer object shadows | `UnderwaterShadow.mat` > Shadow Color |
| Ring size per object | `WaterContactFX` > Ring Diameter |
| Different sun | `PoolEnvironment` > Sun Direction. Then re-generate art lit from the new side |
| Use a painted floor | Assign `PoolFloor` > Floor Texture and raise Floor Texture Weight |
| Use a painted deck | Assign `PoolDeck` > Deck Texture and set Deck Texture Weight to 1 |
| Preview a quality tier | `PoolEnvironment` > Quality Source = ForceLow / ForceMid / ForceHigh (works in edit mode too) |

**Camera framing.** At the current Cinemachine lens (ortho size 7) the camera mostly shows water, so
the deck and coping only appear near the edges. The mockup frames the whole pool. Try ortho size
around 8.5–9 on `CinemachineCamera` if you want more of the surround on screen. That's a gameplay
call (smaller sprites on phones), so it was left unchanged.

---

## 5. Art to generate

The procedural layers already give you water, floor, coping and deck. What still separates the game
from the mockup is **set dressing** (props, plants, foreground leaves) and optionally **painted
deck and floor textures.** Generate those with the prompts below.

### 5.1 Style block (paste into every prompt)

Keep this identical across prompts so the set matches. Most of the 3D read comes from consistent
lighting, so don't drop the lighting line.

```
Stylized casual mobile game art, hand-painted look with soft cel shading, clean readable shapes,
vibrant saturated summer palette (turquoise, coral, sunny yellow, leaf green, warm terracotta),
strict top-down orthographic view looking straight down, key light from the top and slightly left,
soft short shadows falling down and slightly right, gentle ambient occlusion in creases,
no text, no watermark, no border.
```

For **single props**, add:

```
Single isolated object, centered, fully in frame with generous padding, transparent background
(PNG with alpha), no ground plane, no cast shadow on the background.
```

> Cast shadows are added in-engine (`WaterContactFX`, or a soft blob for deck props) so they always
> agree with the sun. Ask the generator **not** to paint shadows into props.

For **tileable textures**, add:

```
Seamless tileable texture, edges wrap perfectly on all four sides, evenly lit with no vignette,
no perspective, flat lighting, square 1:1, 2048x2048.
```

### 5.2 Textures

**Deck tiles (replaces the procedural deck)**
```
[style block] [tileable block] Top-down outdoor pool deck made of large warm sandstone pavers,
3 by 3 pavers per tile, slightly rounded paver edges, thin light grout lines, subtle sun-bleached
color variation between pavers, faint speckle and a few hairline cracks.
```
Alternative look:
```
[style block] [tileable block] Top-down wooden pool deck, warm honey-brown planks running
horizontally, 6 planks per tile, staggered plank ends, soft wood grain, small dark gaps between
planks, slight color variation per plank.
```
Import: Texture Type `Default`, Wrap `Repeat`, sRGB on, Max Size 1024 (mobile). Put it in
`PoolDeck.mat` > Deck Texture, set Deck Texture Size (world units per tile, ~4), and Weight 1.

**Pool floor mosaic (optional; the procedural floor is already close to the mockup)**
```
[style block] [tileable block] Top-down swimming pool floor of small square glossy mosaic tiles,
light aqua and pale turquoise tiles with slight random shade variation, thin white grout lines,
8 by 8 tiles per texture, viewed dry (no water, no caustics, no reflections).
```
Ask for **no caustics and no water**: the shader adds those, and painted ones would fight them.
Import as above, assign to `PoolFloor` > Floor Texture, size ~4, and Weight 0.6–1.

### 5.3 Pool fixtures (sit on the coping, `Default` layer)

```
[style block] [prop block] Stainless steel pool ladder seen from directly above, two curved
chrome handrails bending down into the water, three flat steps, bright specular highlights on the
chrome, cartoon proportions.
```
```
[style block] [prop block] Square pool skimmer grate seen from directly above, dark grey plastic
grid of slots, slightly rounded corners, a thin lighter rim.
```
```
[style block] [prop block] Round pool floor drain cover seen from directly above, pale grey
plastic with radial slots, subtle highlight.
```
The drain sits **under** the water: put it on the `Water` sorting layer at order -1, or bake it
into the floor texture.

### 5.4 Deck decor (`Default` layer, add `HeightParallax`, Height 0.3–1)

```
[style block] [prop block] Striped beach towel lying flat, seen from directly above, navy and
white stripes, slightly rumpled corner.
```
```
[style block] [prop block] Wooden sun lounger seen from directly above, white cushion with blue
stripes, slatted wooden frame, folded towel at the foot.
```
```
[style block] [prop block] Terracotta pot with a lush tropical plant seen from directly above,
big glossy monstera and palm leaves spreading outward from the center, leaves overlapping the pot rim.
```
```
[style block] [prop block] Coral and white beach umbrella seen from directly above, eight panels,
scalloped edge, small center cap.
```
```
[style block] [prop block] Climbing ivy vine strand with small rounded leaves and a few tiny white
five-petal flowers, long and thin, seen from above, for draping along a pool edge.
```

### 5.5 Foreground leaves (`Foreground` layer, `HeightParallax` Height 2–3)

These sell the "camera is above a real place" feeling in the mockup's corners.
```
[style block] Large out-of-focus tropical leaves overhanging a corner of the frame, seen from
below a canopy, dark green monstera and palm fronds, soft depth-of-field blur, darker than the
scene, entering from the bottom-left corner, transparent background (PNG with alpha), 2048x2048.
```
Make two or three variants (bottom-left, bottom-right, top-left). Keep them small on screen so
they never cover gameplay. Parent them to the camera, or give them a high `HeightParallax` value.

### 5.6 Floating props (`Default` layer, add `WaterContactFX`)

```
[style block] [prop block] Inflatable beach ball seen from directly above, purple and white
segments, glossy highlight on the top-left.
```
```
[style block] [prop block] Pink frosted donut pool float seen from directly above, colorful
sprinkles, glossy vinyl highlights on the top-left.
```
```
[style block] [prop block] Single round green lily pad seen from directly above, a narrow notch
cut into one side, visible veins, slightly glossy.
```

### 5.7 Optional character refresh

The mockup's ducks are shaded as 3D toys: a highlight on the top-left and a darker lower-right.
Matching that on your sprites adds a lot of depth:
```
[style block] [prop block] Cute rubber duck toy seen from directly above at a slight angle,
glossy vinyl, strong specular highlight on the top-left of the head and back, darker shading on
the lower-right, chunky toy proportions. Variants: yellow, white, pink, red with black sunglasses.
```

### 5.8 Prompting tips

- Generate at 2048, downscale to the size you need, and set the sprite's Pixels Per Unit so a duck
  is ~1 world unit wide (as now).
- AI "seamless" textures are rarely perfect. Check them with an Offset filter (Photoshop/Krita/GIMP:
  offset by half the width and height) and heal the seam.
- If the generator can't output alpha, ask for a flat **#00FF00** background and key it out.
- Generate one hero prop, then feed it back as a style reference for the rest of the set.

---

## 6. Adding decor in the scene

1. Drop the sprite under `Bg` (or a new `Bg/Decor` object). Sorting layer `Default`, order 0–5.
2. Props *on* the deck: add `HeightParallax` on the root and point **Visual** at the sprite child.
   Use Height 0.3 (towel) to 1 (umbrella).
3. Props *in* the water: add `WaterContactFX`, assign `UnderwaterShadow.mat`, `WaterlineRing.mat`
   and `SoftCircle` (copy the setup from a lily pad).
4. Things that hang over the deck (lounger, plants) can use a soft dark ellipse sprite under them for
   a contact shadow: `SoftCircle` tinted black at ~35% alpha, offset slightly down-right.

---

## 7. Mobile cost and quality tiers

### Floor bake
The floor's static layer (tiles, wall face, depth tint, edge AO, rim shadow) never changes during a
run, so `PoolEnvironment` renders it **once at scene load** into a texture (`PoolFloor`'s
`BakeStatic` pass) and enables the global keyword `_POOL_BAKED`. From then on, each frame the floor
only does one baked read (through the refraction) plus the parts that move: caustics, refraction and
ripple light. The look is the same; a parity test in the Editor measured an average difference of
2.4/255 against the live path.

- In **edit mode** the floor still runs fully live, so material tweaks show immediately.
- In **Play mode in the Editor** it re-bakes whenever the PoolFloor material, the walls or the sun
  change, so live tuning still works. In builds it bakes once (and again if the GPU drops the texture).
  If you change floor material values from code, call `PoolEnvironment.Instance.Rebake()`.
- Memory: about 8 MB at 64 px/unit (High), about 3 MB at 40 px/unit (Low). Set these with
  `Bake Pixels Per Unit` / `Low Bake Pixels Per Unit`.
- Turn **Bake Floor** off on `PoolEnvironment` to go back to the fully live shader.

### Quality tier
The player picks **Low / Mid / High** in the main menu's Settings popup. The choice is stored by
`SettingsService` (PlayerPrefs key `Settings_GraphicsQuality`). Until the player picks a tier, it
follows the device: `GraphicsQualityProfile.DefaultForDevice()` gives Low to phones with ≤ 3.5 GB RAM
or ≤ 4 cores, Mid to other phones with ≤ 6 GB, and High to everything else.

What each tier changes is defined in one place: `GraphicsQualityProfile.For()` in
`Assets/Scripts/Framework/Services/SettingsService/GraphicsQuality.cs`. Today:

| Knob (profile field) | Low | Mid | High |
|---|---|---|---|
| `CheapWaterShaders`: no glints, no chromatic caustics, no caustic brightness variation, flat deck | on | off | off |
| `Bloom` | off | off | on |
| `FloorBakePixelsPerUnit` | 40 (~3 MB) | 48 (~4.6 MB) | 64 (~8 MB) |

**To add a knob later:** add a field to `GraphicsQualityProfile`, set it for each tier in `For()`, and
read `ISettingsService.GraphicsProfile` in the system that owns the effect, re-applying on
`OnSettingsChanged` (see how `PoolEnvironment.ApplyQuality` does it). Project-wide knobs (target frame
rate, URP render scale, Unity quality level) would be applied from `SettingsService` when the tier changes.

**Testing a tier in the Editor:** `PoolEnvironment` > Quality Source = `ForceLow` / `ForceMid` /
`ForceHigh`. `Settings` (default) follows the player's choice. In edit mode it always previews High,
and when the Game scene is played without Bootstrap it uses the device default.

### Cost per pixel per frame (estimates)

| Pass | Before bake | High (baked) | Low (baked) |
|---|---|---|---|
| PoolFloor (whole screen) | 10 reads, heavy maths | 10 reads, light maths (Mid too) | 6 reads, light maths |
| PoolSurface (water area) | 5 reads | 5 reads | 3 reads |
| PoolDeck | light | light | lighter |

The biggest saving is the maths, not the reads: the tile, wall-face and pool-shape calculations,
around two thirds of the floor's work, now run once at load instead of every frame.

### Other costs
- Per floating object: two extra sprites (shadow and ring). They batch.
- The ripple sim is unchanged (512² RHalf plus one camera).
- These are estimates. Measure on a real low-end phone with Android GPU Inspector or Snapdragon
  Profiler before shipping.

---

## 8. Ideas for later

- **Irregular pool shape** (like the mockup's curvy outline): replace the rounded-rect SDF in
  `PoolCommon.hlsl` with a baked SDF texture of the outline, and match the edge colliders to it with
  `PolygonCollider2D`/`EdgeCollider2D`.
- **Duck bob:** duck sprites sit on the physics root, so `WaterContactFX`'s bob can't be used. Move
  the visuals under a `GFX` child (like `RubberDuck_shooter`) and set it as Bob Target.
- **Wake trails:** emit ripple particles continuously while an object moves, scaled by speed. The
  surface shader already lights whatever the ripple sim produces.
