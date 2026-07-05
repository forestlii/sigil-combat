# Changelog

[English](CHANGELOG.md) | [简体中文](CHANGELOG.zh-CN.md)

All notable changes to Sigil Combat are documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/).

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

[0.1.0]: #010---2026-07-05
