---
name: add-weapon
description: Add a new weapon to PoolPatrol, or change firing, ammo/reload/cooldown, projectile aiming indicators, or combo-based weapon effects. Use when the task mentions a weapon (handgun, shotgun, etc.), bullets, magazine/reload, fire-on-release aiming, or combo hit effects. Explains WeaponBase, WeaponMagazine, WeaponAttack, and IComboWeapon.
---

# Adding / extending a weapon

The weapon subsystem lives in `Assets/Weapons` (bases under `Assets/Weapons/Base`, each weapon in its
own folder e.g. `HandGun/`, `Shotgun/`) plus controllers in `Assets/Scripts/Weapon`. Namespace: `Weapon`.

## Core pieces

- **`WeaponBase`** (`Assets/Weapons/Base/WeaponBase.cs`) — base MonoBehaviour for a weapon. Holds the
  `FirePoint`, `m_BulletPrefab`, bullet tuning (`BulletSpeed`, `BulletSizeMultiplier`, `BulletRange`,
  collider multiplier), an optional `ProjectileIndicator`, and a `WeaponMagazine` (built in `Awake`
  from serialized `WeaponAmmoSettings`). Key method:
  `public virtual WeaponAttack FireWeapon(Vector2 direction, BulletSource source)` — it calls
  `Magazine.TryConsumeBullet()`, and **returns null if the weapon can't fire** (cooldown / reloading).
  On success it instantiates the bullet, calls `bullet.Fire(...)`, and returns a `WeaponAttack`.
- **`WeaponControllerBase`** — holds the `ActiveWeapon` and a `BulletSource`. `PlayerWeaponController`
  (in `Assets/Scripts/Weapon`) subclasses it, drives input, aiming, crosshair, range indicator, and
  weapon switching; `EnemyWeaponController` is the enemy counterpart.
- **`WeaponMagazine`** — pure C# ammo/reload/cooldown state machine. Two `ReloadStyle`s:
  `MagazineAtOnce` (handgun: full reload after emptying; firing mid-reload resets the whole timer) and
  `BoltAction` (loads one bullet at a time; can fire already-loaded rounds mid-reload). Exposes
  `CurrentAmmo`, `IsReloading`, `CanFire`, `CooldownProgress`, `ReloadProgress`, and events
  (`OnBulletFired`, `OnReloadStarted`, `OnBulletReloaded`, `OnReloadComplete`) for the HUD to consume.
  Driven by `WeaponBase.Update` calling `Magazine.Tick(Time.deltaTime)`.
- **`WeaponAttack`** — a callback object returned per shot: `OnHitSuccess(Vector3 hitPos)`,
  `OnHitFail()`, `OnAttackComplete()`, plus `Clear()`. The bullet raises these; the controller
  subscribes to feed combo logic, VFX, and the crosshair. This is how hits flow back — do not poll.
- **`ProjectileIndicatorBase`** — abstract aim preview. `Show(bool canShoot)`, `Hide()`,
  `UpdateAim(origin, direction, range)`, `UpdateColor(canShoot)`. Firing is **on release**: the
  controller shows/updates the indicator during hold and fires in `OnFireRelease`.
- **`IComboWeapon`** — optional interface for combo reactivity: `OnComboLevelChanged(ComboLevel)` for
  passive stat changes, and `PerformComboHitEffect(hitPosition, currentLevel)` for per-hit effects
  (AoE explosions, chaining). See `WeaponHandGun` for a full example (radius damage + pooled explosion
  VFX + dynamic camera shake).

## Steps to add a new weapon

1. Create `Assets/Weapons/<WeaponName>/Script/Weapon<WeaponName>.cs`, class `Weapon<Name> : WeaponBase`
   (namespace `Weapon`), optionally `, IComboWeapon`.
2. Override `FireWeapon`: call `base.FireWeapon(direction, source)` first; **if it returns null, return
   null** (respect ammo/cooldown). On non-null, add weapon-specific behavior (muzzle flash, spread,
   extra bullets) and return the attack. See `WeaponHandGun` / `WeaponShotgun`.
3. Configure `WeaponAmmoSettings` (magazine size, `ReloadStyle`, reload time, cooldown) in the inspector.
4. Build the weapon prefab with a `FirePoint`, bullet prefab, and a `ProjectileIndicator` (subclass
   `ProjectileIndicatorBase`, e.g. `HandgunIndicator` / `ShotgunIndicator`).
5. Add the weapon to `PlayerWeaponController.m_AvailableWeapons` so `SwitchWeapon()` can cycle to it.
   The controller calls `ActiveWeapon.Setup(this)` in `Awake` — call `Setup` again if you swap weapons.

## Gotchas

- Shots that miss must trigger `OnHitFail` so `PlayerCombo.BreakCombo()` runs; successful hits trigger
  `OnHitSuccess` which adds combo and runs `PerformComboHitEffect`. Keep this contract when adding
  bullet/attack types.
- For AoE damage, follow `WeaponHandGun.PerformComboHitEffect`: `Physics2D.OverlapCircleAll`, filter by
  `Constants.GameConstants.TAG_Enemy`, call `EnemyController.Die(new EnemyDeathParameters{...})`.
- Pooled VFX come from `ObjectPoolManager.Instance.SpawnItem(...)` — see **add-poolable-item**.
- Conventions: `m_PascalCase` serialized privates, `_camelCase` privates. See **poolpatrol-architecture**.
