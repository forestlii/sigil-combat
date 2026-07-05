# Sigil Movement · Combat Demo

[English](README.md) | [简体中文](README.zh-CN.md)

A combat sample that puts the Movement Demo's locomotion together with the core GAS combat flow.
You move a third-person character with **WASD + mouse look** (the Movement Demo's input path), switch
between a **melee and a ranged weapon**, and use a set of abilities — a **dash attack**, a **blink (Flash)**
and a **hold-to-charge Fireball** — against simple threat-AI enemies that chase you and hit back.
Everything is built on GAS features: abilities, cost/cooldown effects, `SetByCaller` damage,
granted-tag stun, and poise.

## Run it

This sample ships a **ready-baked scene + player/enemy prefabs** — just:

1. Open **`CombatDemo.unity`** (in this folder).
2. Press **Play**.
3. Controls:
   - **WASD** move · **mouse** look · **Shift** toggle sprint · **Esc** release cursor
   - **Left mouse** attack — polymorphic: *sprint + melee* = dash attack, *melee* = normal attack, *ranged* = fire a bullet
   - **Tab** switch melee ⇄ ranged weapon
   - **Right mouse (hold)** Fireball — a ground AoE reticle appears while charging; **release** to fire. The longer you charge, the bigger the blast / damage / stun.
   - **Q** Flash (blink in your movement direction) · **E** lock on

### Re-bake (optional)

The scene, the `CombatDemoPlayer` / `CombatDemoEnemy` prefabs (under `Resources/`) and all the data
assets (effects / attacks / bullets / abilities / input config / loadouts / movement def) are generated
by an editor script. To regenerate them (e.g. after editing the `.inputactions`), run
**Sigil ▸ GAS ▸ Samples ▸ Build Combat Demo Scene** (idempotent; the Console prints the output path).

## What it shows

- **Movement + input reuse** — the same `InputProcessor_Move` / `InputProcessor_Look` /
  `InputProcessor_ToggleAbilityByTag` (Shift → `GA_M_Sprint`) path as the Movement Demo, on a
  `CharacterMovementSystemComponent` + third-person camera.
- **Attack polymorphism on one button** — the `InputControlSetup` runs in `FirstOnly` mode and each
  attack processor carries a `GameplayTagQuery` state gate: `Sprint + Weapon.Melee` → `GA_DashAttack`,
  `Weapon.Melee` → `GA_MeleeAttack`, `Weapon.Ranged` → `GA_RangedAttack`. The sprint state is the
  `Movement.State.Sprint` tag the movement system mirrors onto the ASC; the weapon tag is injected by
  the equipped `WeaponComponent`.
- **Weapon switching** — Tab broadcasts a `GameplayEvent`; `CombatDemoController` equips the other
  `WeaponComponent`, which injects its `Weapon.*` tag onto the ASC and re-drives the attack polymorphism.
- **Flash** (`GA_Flash`) — teleports along the movement-input direction via the movement component's
  `Teleport` entry point, gated by a stamina cost + cooldown effect.
- **Fireball** (`GA_Fireball`) — activates on press, shows a ground AoE reticle while charging
  (`AbilityTick`), waits for the button release with `AbilityTask_WaitInputPress(Canceled)`, then fires a
  projectile whose **damage / AoE radius / stun duration scale with charge time**. On hit it applies a
  damage `GameplayEffect` (`SetByCaller`) plus a duration stun effect whose `GrantedTags` = `State.Stunned`.
  The stun is pure GAS state: it blocks the target's ability activation (`ActivationBlockedTags`) and
  freezes its AI.
- **Poise / stun both ways** — player and enemies carry `AS_Poise` + `PoiseComponent`, so heavy hits
  break poise into a `State.Staggered` stun, and both sides' abilities are blocked while stunned/staggered.
- **Threat-type enemy AI** (`CombatDemoEnemyAI`) — chases the player (driving the movement component),
  activates a melee ability on cooldown when in range, freezes while stunned or poise-broken, and dies
  when health reaches zero.

> Requires the core package `com.likeon.gas`. Placeholder programmer art (plane + capsules); real combat
> feel (animations, VFX, camera, hit-stop) needs your own presentation layer — the abilities here only
> guarantee the logic (a correct white-box).
