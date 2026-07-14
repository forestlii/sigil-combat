# Changelog

[English](CHANGELOG.md) | [简体中文](CHANGELOG.zh-CN.md)

All notable changes to Sigil Combat are documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/).

## [0.1.7] - 2026-07-14

### Fixed

- **Combat Demo — replaced deprecated scene-lookup APIs in the sample scripts.** `CombatDemoEnemyAI` and `CombatDemoSmokeTest` used `FindObjectsOfType<T>()` / `FindObjectOfType<T>()`, which are obsolete on Unity 6000.x. Switched to the current `FindObjectsByType<T>()` / `FindAnyObjectByType<T>()` overloads (unsorted — no ordering was relied on), clearing the CS0618 warnings. Sample-only, no runtime/API change.

## [0.1.6] - 2026-07-09

### Changed

- **Performance — bullet pooling (no behavior change).** `BulletLauncher` now rents bullets from a shared idle pool and recycles them on expiry (hit-stop / lifetime end) instead of `new GameObject` + `Destroy` per shot, avoiding per-fire GC. Recycled bullets are reset (event subscribers cleared, deactivated) before reuse. Bullets created directly (not via the launcher) still self-destroy — behavior unchanged. Adds `BulletLauncher.ClearPool()` (for tests / scene changes) and `BulletLauncher.PooledCount` for debugging.

## [0.1.5] - 2026-07-08

### Changed

- **Expanded the usage guide** from an overview into a full reference matching the core package's depth: a quick start, the attack-pipeline anatomy, and per-system API + code for melee, ranged/bullets, lock-on targeting, poise/stagger, and weapons; field tables for `AttackDefinition` / `BulletDefinition`; the full `ICombatInterface` contract; teams; and damage/action selection. Docs only — no runtime/API change.

## [0.1.4] - 2026-07-05

### Changed

- **Combat Demo — the fireball's ground reticle is now an `AbilityTask`** (`AbilityTask_GroundReticle`) instead of a persistent `CombatDemoFireballReticle` MonoBehaviour (removed). The task owns the reticle disk's whole lifecycle: it creates the disk on activate, updates position/radius each frame from a sampler the ability supplies, and — because the framework auto-cancels a ability's tasks on end — destroys the disk automatically when the fireball ends (no manual show/hide, no persistent component, no leak). Demonstrates the preferred pattern: reach for a framework primitive (AbilityTask) rather than a bespoke component. (Requires core ≥ 0.7.1, which lets AbilityTasks be subclassed outside the core assembly.)

## [0.1.3] - 2026-07-05

### Added

- **One-click "Combat Character Setup"** (*GameObject ▸ Sigil ▸ Combat Character Setup*, or *Sigil ▸ GAS ▸ Setup ▸ Add Combat Character Components*). Adds the common combat components to the selected object(s) if missing — `AbilitySystemComponent` + `CombatTeamAgent` + `CombatSystemComponent` + `PoiseComponent` (idempotent, undoable). The components stay **separate, single-responsibility** — this is a setup convenience, not a merged "combat" component — so role-specific pieces (`TargetingSystemComponent` / `MeleeAttackTrace` / `WeaponComponent`) are still added per character as needed.

## [0.1.2] - 2026-07-05

### Fixed

- **Combat Demo — the player/enemy prefabs no longer show a missing (magenta) material.** The scene builder tinted the capsules with an in-memory `new Material(...)` assigned as `sharedMaterial`; that material was never an asset, so `SaveAsPrefabAsset` couldn't serialize it and the prefab's material slot was saved as `None` — rendering magenta on import. The builder now saves the tint materials as real `.mat` assets (`Samples~/CombatDemo/Materials/`) before assigning them, so the shipped prefabs reference persistent materials. Re-bake with *Sigil ▸ GAS ▸ Samples ▸ Build Combat Demo Scene*.

## [0.1.1] - 2026-07-05

### Added

- **Combat Demo sample** (`Samples~/CombatDemo`) — moved here from the movement package. An integration demo composing this combat package with the movement companion (`com.likeon.gas.movement`): third-person locomotion + weapon switching, attack polymorphism (dash/normal/ranged), a Flash blink, a hold-to-charge Fireball (damage + stun scale with charge), and threat-AI enemies — all built on GAS. It ships the `CombatCore` bridge (`ICombatInterface` implementation). **Requires `com.likeon.gas.movement`** (a sample-level dependency; the package itself still does not depend on movement).

## [0.1.0] - 2026-07-05

First release as a standalone companion package. **Extracted from the Sigil core package**
(`com.likeon.gas`) so the GAS core stays a pure ability-system framework, restoring the module
boundary the original combat framework already had (abilities / attributes / combat as separate
modules). Namespace unchanged (`Likeon.GAS`). Sits beside `com.likeon.gas.movement` — the two are
independent and don't depend on each other.

### Added

- **Attack pipeline** — `AttackDefinition`, `AttackApplication`, `AttackResult`, `AttackRequest`,
  `AttackResultProcessor`(s), `CombatFlowComponent`, `CombatSystemComponent`, `AbilityActionLibrary`.
- **Hit detection** — `MeleeAttackTrace`, `CollisionTrace`.
- **Poise** — `PoiseComponent` (attribute-name based) + poise-break events.
- **Targeting / lock-on** — `TargetingSystemComponent`.
- **Weapons & bullets** — `IWeapon`, `WeaponComponent`, `BulletDefinition`, `BulletInstance`, `BulletLauncher`.
- **Combat support** — `CombatTeamAgent`, `CombatSettings`, `CombatTypes`, `MovementCancellation`,
  `DamageExecutionCalculation` (negation + guard by attribute name), and the `ICombatInterface` contract.
- **Playable Demo sample** (`Samples~/PlayableDemo`) — moved here from the core package; a feature
  showcase of melee/ranged/lock-on/poise/stacking built on the core + this package.

### Notes

- Combat resolves attributes **by name** (no hard-coded `AS_*`); generate attribute sets with the core's
  codegen tool following the recommended name convention.
- Requires the matching Sigil core release (`com.likeon.gas` ≥ 0.7.0, which removed the built-in `Combat/`).

[0.1.4]: #014---2026-07-05
[0.1.3]: #013---2026-07-05
[0.1.2]: #012---2026-07-05
[0.1.1]: #011---2026-07-05
[0.1.0]: #010---2026-07-05
