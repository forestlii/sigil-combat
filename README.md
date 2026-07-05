# Sigil Combat — Melee, Ranged & Attack Companion

[English](README.md) | [简体中文](README.zh-CN.md)

A **companion package** for [Sigil](https://github.com/forestlii/sigil-gas) (`com.likeon.gas`) that provides
the melee & ranged **combat** layer. Kept separate from the GAS core on purpose: combat is a *domain*
built on top of the ability system, not part of the core mechanics — so you can use the Sigil core with
your own combat, or this package with Sigil. Sits **beside** the movement companion (`com.likeon.gas.movement`);
the two are independent and don't depend on each other.

- **Depends on:** `com.likeon.gas` (Sigil core)
- **Namespace:** `Likeon.GAS` (same as the core, so it doesn't break your `using`)
- **Engine:** Unity 6 (6000.4)
- **License:** MIT

## Install

This package depends on the Sigil core package. Install both:

1. Add `com.likeon.gas` (Sigil core) first.
2. Add `com.likeon.gas.combat` (this package).

(Package Manager → *Add package from disk…* → each `package.json`.)

### Running tests

The package ships with EditMode + PlayMode tests under `Tests/`. To run them, add the
package to `"testables"` in your project's `Packages/manifest.json`, then open
**Window → General → Test Runner**:

```json
"testables": [ "com.likeon.gas.combat" ]
```

## Features

- **Attack definitions & application** — `AttackDefinition` (SO) describes the effects/tags applied on hit;
  `AttackApplication` builds the `GameplayEffectSpec`(s) and applies them to targets.
- **Hit detection** — `MeleeAttackTrace` (shape sweeps) and `CollisionTrace` (generic collision trace) surface hits.
- **Combat flow pipeline** — `CombatSystemComponent` + `CombatFlow/` (`AttackRequest` → `AttackResult` →
  `AttackResultProcessor`s) turn an attack into hit results and route them (damage, death, etc.), plus
  `AbilityActionLibrary` for tag-gated ability actions.
- **Poise & poise-break** — `PoiseComponent` tracks a poise attribute (by name) and broadcasts break events.
- **Targeting / lock-on** — `TargetingSystemComponent` finds and cycles combat targets (dead targets filtered by attribute name).
- **Weapons & bullets** — `IWeapon` / `WeaponComponent` (tag injection, source object) and
  `Bullet/` (`BulletDefinition` / `BulletInstance` / `BulletLauncher`) for projectiles.
- **Damage model** — `DamageExecutionCalculation`, a `GameplayEffectExecutionCalculation` implementing
  negation + guard by attribute name (a default combat model you can subclass/replace).
- **Combat contract** — `ICombatInterface` is the seam other systems query for target / weapon / block input /
  movement-input direction / movement modes / death lifecycle. Your character implements it (or use a bridge
  component in the sample).

### Attribute-name based, no hard-coded attribute sets

Combat systems read/write attributes **by name** (e.g. `Poise` / `MaxPoise` / `PoiseRecover`,
`Damage` / `DamageNegation` / `GuardDamageNegation` / `IncomingDamage`) rather than binding to concrete
`AttributeSet` types. Generate your own attribute sets with the core's **codegen** tool and follow the
recommended name convention — combat doesn't ship or require any specific `AS_*`.

## Samples

- **Playable Demo** (`Samples~/PlayableDemo`) — a playable feature-showcase (player/enemy prefabs +
  scene) demonstrating input → ability → melee/ranged hit → damage → GameplayCue, lock-on target
  switching, poise break, and buff stacking, all driven by data-driven loadouts. Import, open
  `PlayableDemo.unity`, press Play.

## License

[MIT](LICENSE.md) — free for any use including commercial, just keep the copyright notice.
