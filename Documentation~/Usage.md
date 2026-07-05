# Sigil Combat — Usage

[English](Usage.md) | [简体中文](Usage.zh-CN.md)

This companion package adds the melee & ranged **combat** layer on top of the Sigil GAS core
(`com.likeon.gas`). For core concepts (abilities, effects, attributes, tags, input) see the core
package's own `Documentation~/Usage.md`. This document covers the combat layer specifically.

## 1. Where combat sits

```
Sigil core (com.likeon.gas)        ← abilities / effects / attributes / tags / input
        ▲                    ▲
        │                    │
  Sigil Combat          Sigil Movement    ← two independent domain companions
 (this package)        (com.likeon.gas.movement)   (they don't depend on each other)
```

Combat only depends on the core. It reads/writes attributes **by name**, so it composes with
whatever attribute sets you generate with the core's codegen tool.

## 2. Attribute-name convention

Combat systems resolve attributes by name (never by concrete `AttributeSet` type). Generate
attribute sets (core: *Sigil ▸ GAS ▸ ...* codegen) that expose these names:

| System | Attribute names it reads/writes |
|---|---|
| `PoiseComponent` | `Poise`, `MaxPoise`, `PoiseRecover` |
| `DamageExecutionCalculation` | `Damage`, `DamageNegation`, `GuardDamageNegation`, `IncomingDamage` |
| `TargetingSystemComponent` (dead filter) | `Health` |

The names are configurable on the components/executions; the defaults above match the original
combat framework. Only the *names* matter — combat doesn't care which set they live in.

## 3. Attack flow at a glance

1. An ability (or `WeaponComponent`) issues an **AttackRequest** through `CombatSystemComponent`.
2. Hit detection (`MeleeAttackTrace` / `CollisionTrace`) produces targets.
3. `AttackDefinition` describes what to apply on hit; `AttackApplication` builds the
   `GameplayEffectSpec`(s) and applies them.
4. `AttackResultProcessor`s route the outcome (damage via `DamageExecutionCalculation`, death, etc.).
5. `PoiseComponent` tracks poise and broadcasts break; `TargetingSystemComponent` handles lock-on.

## 4. The combat contract (`ICombatInterface`)

Other systems query a character through `ICombatInterface` — target, current weapon, block input,
movement-input direction, movement modes, and death lifecycle. Your character implements it. The
**Playable Demo** and the movement package's **Combat Demo** show a bridge component (`CombatCore`)
that implements it by forwarding to the movement/combat components present on the object.

## 5. Sample

Import **Playable Demo** (`Samples~/PlayableDemo`) from the Package Manager for a ready-baked,
playable scene showing the full loop (melee/ranged/lock-on/poise/stacking) on the core + this package.

## 6. See also

- Core `Documentation~/Usage.md` — abilities, effects, attributes (incl. **codegen**), tags, input, recipes.
- `com.likeon.gas.movement` — the movement companion (pairs with combat in the Combat Demo).
