# Sigil Combat — Usage Guide

[English](Usage.md) | [简体中文](Usage.zh-CN.md)

> The melee & ranged **combat** companion package `com.likeon.gas.combat`, layered on the Sigil GAS
> core (`com.likeon.gas`). For core concepts (abilities, effects, attributes, tags, input) see the
> core package's own `Documentation~/Usage.md`. This document covers the combat layer specifically.
>
> Combat only depends on the core, and reads/writes attributes **by name** — so it composes with
> whatever attribute sets you generate with the core's codegen tool. Namespace is `Likeon.GAS`.

## Contents

1. [Where combat sits](#1-where-combat-sits)
2. [Core concepts at a glance](#2-core-concepts-at-a-glance)
3. [Quick start](#3-quick-start)
4. [Attribute-name convention](#4-attribute-name-convention)
5. [The attack pipeline](#5-the-attack-pipeline)
6. [Melee attacks](#6-melee-attacks)
7. [Ranged attacks & bullets](#7-ranged-attacks--bullets)
8. [Lock-on targeting](#8-lock-on-targeting)
9. [Poise & stagger](#9-poise--stagger)
10. [Weapons & weapon switching](#10-weapons--weapon-switching)
11. [Teams & the combat contract](#11-teams--the-combat-contract)
12. [Damage & action selection](#12-damage--action-selection)
13. [Samples](#13-samples)
14. [Editor cheat sheet](#14-editor-cheat-sheet)

---

## 1. Where combat sits

```
Sigil core (com.likeon.gas)        ← abilities / effects / attributes / tags / input
        ▲                    ▲
        │                    │
  Sigil Combat          Sigil Movement    ← two independent domain companions
 (this package)        (com.likeon.gas.movement)   (they don't depend on each other)
```

Combat depends only on the core. It never references a concrete `AttributeSet` type — it resolves
attributes by name, so it works with any set you generate.

---

## 2. Core concepts at a glance

| Concept | What it means here |
|---|---|
| **Attributes by name** | Poise / damage / health are read by *string name*, not a fixed C# type — combat ships no attribute sets; you generate yours with the core codegen (§4). |
| **AttackDefinition = "what to apply on hit"** | A data asset (effects, cues, knockback, hit-stop). Hit detection produces targets, then the attack is applied to each (§5). |
| **Separate single-responsibility components** | Team / poise / hit-trace / targeting / weapon are separate components you compose, not one god component. A one-click setup tool adds the common set (§3). |
| **The combat contract (`ICombatInterface`)** | Systems query a character (target / weapon / movement modes / death) through one interface the character implements — decoupling combat from your specific movement/controller. |

---

## 3. Quick start

**1) Make a combat character** — the one-click tool adds the common set. Select a GameObject, then
*GameObject ▸ Sigil ▸ Combat Character Setup* (idempotent, undoable). It adds:
`AbilitySystemComponent` + `CombatTeamAgent` + `CombatSystemComponent` + `PoiseComponent`. Add
`TargetingSystemComponent` / `MeleeAttackTrace` / `WeaponComponent` per character as needed.

**2) Give it attributes** — generate an attribute set exposing the names combat expects (§4), e.g.
`Health` / `Damage` / `DamageNegation` / `IncomingDamage` / `Poise` / `MaxPoise` / `PoiseRecover`.

**3) A first melee attack** — the pattern the samples use (real, copyable versions ship in
`Samples~/CombatDemo/GA_MeleeAttack.cs`):

```csharp
using Likeon.GAS;

public class GA_MeleeAttack : GameplayAbility
{
    [SerializeField] private int   traceEntryIndex = 0;   // which MeleeAttackTrace entry
    [SerializeField] private float traceWindow     = 0.3f; // seconds the hitbox is live

    protected override void OnActivateAbility(GameplayEventData triggerData)
    {
        if (!CommitAbility()) { EndAbility(true); return; }   // pay cost + cooldown

        var trace = ASC.GetComponent<MeleeAttackTrace>();
        if (trace == null) { EndAbility(); return; }
        trace.BeginAttackTrace(traceEntryIndex);              // open the hit window

        var wait = AbilityTask_WaitDelay.WaitDelay(this, traceWindow);
        wait.OnFinish += () => { trace.EndAttackTrace(); EndAbility(); };
        wait.Activate();
    }
}
```

In production you drive `BeginAttackTrace` / `EndAttackTrace` from **Animation Events** on the attack
clip instead of a fixed delay — see §6.

---

## 4. Attribute-name convention

Combat systems resolve attributes by name (never by concrete `AttributeSet` type). Generate attribute
sets (core: *Sigil ▸ GAS ▸ …* codegen) that expose these names. The names are **configurable** on the
components/executions; the defaults below match the original combat framework.

| System | Attribute names it reads/writes |
|---|---|
| `PoiseComponent` | `Poise`, `MaxPoise`, `PoiseRecover` |
| `DamageExecutionCalculation` | `Damage`, `DamageNegation`, `GuardDamageNegation`, `IncomingDamage` |
| `TargetingSystemComponent` (dead filter) | `Health` |
| `AttackResultProcessor_Death` | `Health` |

Only the *names* matter — combat doesn't care which set they live in.

---

## 5. The attack pipeline

The end-to-end flow, and the type at each step:

1. An ability (or `WeaponComponent`) opens **hit detection** — `MeleeAttackTrace` (melee) or a bullet
   (`BulletLauncher`).
2. On a hit, **`AttackApplication.ApplyAttack(...)`** builds the `GameplayEffectSpec`(s) from the
   `AttackDefinition` and applies them to the target (damage GE + SetByCaller + effect container + cues).
3. An **`AttackResult`** is produced and handed to the target's `CombatSystemComponent.RegisterAttackResult(result)`.
4. That fires **`CombatSystemComponent.OnAttackResultReceived`**, which a **`CombatFlowComponent`** listens to
   and runs its **`AttackResultProcessor`** chain over (death → gameplay event → cue → …).

### `AttackDefinition` — "what to apply on hit"

*Create → Sigil → Combat → Attack Definition* (has an enhanced inspector). Fields:

| Field | Meaning |
|---|---|
| `AttackTags` | Attack-type tags (melee/ranged/slash/…) injected as dynamic asset tags onto the GE spec. |
| `SetByCallerMagnitudes` | Tag → value pairs passed into the spec as SetByCaller (e.g. damage, poise damage). |
| `TargetEffect` | The main `GameplayEffect` applied on hit (the damage GE). |
| `TargetEffectLevel` | Level for the target effect (`< 1` = use the ability's level). |
| `TargetEffectContainer` | Extra effects applied to the target on hit. |
| `TargetGameplayCues` | Cue tags played at the hit. |
| `KnockbackDistance` / `KnockbackMultiplier` | Knockback tuning. |
| `HitStallingDuration` / `HitPlayRateFactor` | Hit-stop: freeze/slow the animator on hit (`≤ 0` = off; factor `0.1–0.9`). |

### `AttackResult` & processors

`AttackResult` carries `ImpactResult` (Hit/Blocked/…), `TaggedValues` (attribute values like damage,
`GetTaggedValue(tag)`), `EffectContext`, the aggregated source/target tags, `HitLocation`, and `Consumed`.

`CombatFlowComponent` (`[RequireComponent(CombatSystemComponent)]`) holds a `[SerializeReference]`
list of `AttackResultProcessor`s and runs them in order. Built-in processors:

- **`AttackResultProcessor_Death`** — reads `HealthAttributeName` (default `Health`), applies `DeadTag` and
  broadcasts `DeathEventTag` when health ≤ 0.
- **`AttackResultProcessor_GameplayEvent`** — filters by source/target tag query, then sends `EventTriggers`
  to the attacker or the victim (e.g. fire an `Event.OnHit` that activates a hit-react ability).
- **`AttackResultProcessor_GameplayCue`** — executes `GameplayCues` at the hit point.

`CombatSystemComponent` itself exposes: `RegisterAttackResult(result)`, `NotifyDealtDamage(result)`,
`ApplyHitStop(duration, playRateFactor)`, `PlayAttackAction(action)`, `QueryAbilityActions(...)`,
`PlayAbilityActionByTag(tag, targetTags = null)`; events `OnAttackResultReceived` / `OnDealtDamage`; and
`Animator` / `AbilitySystem` / `ActionLibrary` / `LastProcessedAttackResult` properties.

---

## 6. Melee attacks

`MeleeAttackTrace` sweeps a set of sockets while a hit window is open, de-duping targets within one
swing (each target hit once), filtering self + friendlies, then applies the attack.

```csharp
trace.SetSource(attackerASC, attackerCombat);   // usually auto-wired
trace.BeginAttackTrace(entryIndex);             // open the window
// … swing plays …
trace.EndAttackTrace();                          // close it
```

Each `AttackTraceEntry` in `trace.Entries` pairs an `AttackDefinition` with a `CollisionTraceDefinition`
(sockets + radius + hit layers). **Drive the window from Animation Events** on the attack clip:

```
Animation Event @ 0.10s → BeginAttackTrace(0)
Animation Event @ 0.40s → EndAttackTrace()
```

For a lower-level, non-damage hit check (traps, AoE, environment), use **`CollisionTrace`** instead:
`SetSockets(...)`, `ToggleTraceState(bool)`, an `OnHit(Collider)` event, and an optional `HitFilter`
predicate — it only reports hits; you decide what to apply.

> A copyable `GA_MeleeAttack` ships in `Samples~/CombatDemo`.

---

## 7. Ranged attacks & bullets

`BulletDefinition` (*Create → Sigil → Combat → Bullet Definition*) describes a projectile:

| Field | Meaning |
|---|---|
| `Duration` | Lifetime in seconds (`≤ 0` = unlimited). |
| `BulletCount` / `LaunchAngle` / `LaunchAngleInterval` / `LaunchElevationAngle` | Spread pattern (shotgun / fan). |
| `InitialSpeed` / `GravityScale` | Motion (0 gravity = straight line, 1 = arc). |
| `HitRadius` / `HitLayers` | Hit sphere + layers. |
| `PenetrateCharacter` / `PenetrateMap` | Whether it passes through after a hit. |
| `Attack` | The `AttackDefinition` applied on hit. |
| `HitBulletDefinition` | A follow-up bullet spawned on hit/expire (burst/split). |

Fire from an ability with `BulletLauncher` (static). It spawns `BulletCount` bullets around the base
direction and returns the `BulletInstance`s:

```csharp
protected override void OnActivateAbility(GameplayEventData triggerData)
{
    if (!CommitAbility()) { EndAbility(true); return; }

    Vector3 dir = muzzle.forward;
    var target = CombatInterface.Get(ASC.gameObject)?.GetCombatTargetActor();
    if (target != null) dir = (target.transform.position - muzzle.position).normalized;

    var bullets = BulletLauncher.Fire(bulletDef, ASC.gameObject, ASC, muzzle.position, dir);
    // damage is applied by bulletDef.Attack on hit; subscribe for extra logic:
    foreach (var b in bullets) b.OnHit += (inst, targetASC, point) => { /* targetASC == null → hit map */ };

    EndAbility();
}
```

`BulletLauncher.Fire` has two overloads (base `Quaternion` or `Vector3` direction). A single
`BulletInstance` also exposes `Launch(...)`, `Tick(dt)` (turn off `AutoTick` to drive manually, e.g.
in tests), and `OnHit` / `OnExpired` events.

> A copyable `GA_RangedAttack` / `GA_Fireball` (hold-to-charge) ship in `Samples~/CombatDemo`.

---

## 8. Lock-on targeting

`TargetingSystemComponent` finds and locks the best target in range, honoring view angle, team, and a
dead filter.

```csharp
targeting.ToggleLock();                       // lock the best target / unlock (the lock-on key)
targeting.StaticSwitchToNewTarget(true);      // cycle to the neighbor on the right (false = left)
GameObject t  = targeting.TargetedActor;      // current lock (null if none)
bool locked   = targeting.IsLockedOn;

targeting.OnTargetLockOn  += target => { /* HUD: show reticle */ };
targeting.OnTargetLockOff += target => { /* HUD: clear reticle */ };
```

Runtime-tunable: `SearchRadius`, `MaxViewAngle` (cone, degrees; `> 0` enables), `OnlyHostile`,
`FilterDead`, `ViewSource` (camera), `RequiredTags` / `BlockedTags`. Lower-level API if you need it:
`SearchForActorToTarget()`, `RefreshPotentialTargets()`, `SelectBestActor()` / `SelectClosestActor()`,
`SetTargetedActor(go)`, `CanBeTargeted(go)`, `CalculateViewAngle(go)`.

---

## 9. Poise & stagger

`PoiseComponent` tracks a poise attribute; when it hits zero the character is staggered (poise break),
then recovers after a delay.

```csharp
poise.OnPoiseBroken    += () => { /* opening — punish window */ };
poise.OnPoiseRecovered += () => { /* back to normal */ };
bool staggered = poise.IsStaggered;
```

Configurable: attribute names `poiseName` / `maxPoiseName` / `poiseRecoverName` (default `Poise` /
`MaxPoise` / `PoiseRecover`), `staggeredTag` (default `State.Staggered`), an optional `staggerEffect`,
`staggerDuration` (`StaggerDuration` at runtime), and `recoverDelay` (`RecoverDelay`). Deal poise damage
by putting a poise value in the attack's `SetByCallerMagnitudes` and modifying the poise attribute.

Mechanic: poise reduced → recovery delay resets → poise ≤ 0 → **Break** (apply staggered tag + optional
effect, fire `OnPoiseBroken`) → after `staggerDuration` → **Recover** (clear tag, refill poise, fire
`OnPoiseRecovered`).

---

## 10. Weapons & weapon switching

`WeaponComponent` (implements `IWeapon`) holds weapon tags, a muzzle, and melee trace instances.
Equipping injects the weapon's tags onto the owner ASC — which is how a **single attack key becomes
polymorphic** (a `GameplayTagQuery`-gated input processor picks the melee vs ranged ability by the
`Weapon.*` tag currently present).

```csharp
current?.Unequip();          // removes its weapon tags from the owner ASC
next.Equip(gameObject);      // wires owner + ASC, injects weaponTags, re-sources the trace instances
current = next;
// Now the owner has e.g. Weapon.Ranged → the attack key activates GA_RangedAttack via a tag-gated
// InputProcessor_ActivateAbilityByTag (StateQuery contains Weapon.Ranged).
```

Key members: `Equip(owner, attachSocket = null)`, `Unequip()`, `SetWeaponActive(bool)`,
`SetTargeting(bool)` / `ToggleTargeting()`, `RefreshTraceInstances()`, `FireProjectile(BulletDefinition)`;
events `OnEquipped` / `OnUnequipped` / `OnWeaponActiveStateChanged` / `OnTargetingChanged`. The `IWeapon`
interface (`WeaponOwner`, `WeaponTags`, `IsWeaponActive`, `MuzzleTransform`, `SourceObject`,
`IsTargeting`, …) lets other systems query the equipped weapon abstractly.

---

## 11. Teams & the combat contract

**`CombatTeamAgent`** (`ITeamAgent`) assigns a `TeamId`; combat uses it to tell friend from foe:

```csharp
agent.SetTeamId(1);
ETeamAttitude att = agent.GetAttitudeTowards(otherGo);   // Friendly / Hostile / Neutral
bool hostile = CombatTeamAgent.IsHostile(sourceGo, targetGo);  // static helper
```

Same id → Friendly, different (both ≥ 0) → Hostile, any `-1` → Neutral. `CombatSettings.DisableAffiliationCheck`
turns the check off for debugging.

**`ICombatInterface`** is the character-facing contract every combat system queries through. Your
character (or the sample `CombatCore` bridge) implements it:

```csharp
public interface ICombatInterface
{
    GameObject GetCombatTargetActor();      Transform GetCombatTargetObject();
    bool QueryAbilityActions(GameplayTagContainer abilityTags, GameplayTagContainer sourceTags,
                             GameplayTagContainer targetTags, List<AbilityAction> outActions);
    IWeapon GetCurrentWeapon();
    bool IsHoldingBlockInput();             Vector3 GetMovementInputDirection();
    void SetRotationMode(GameplayTag);      GameplayTag GetRotationMode();
    void SetMovementSet(GameplayTag);       GameplayTag GetMovementSet();
    void SetMovementState(GameplayTag);     GameplayTag GetMovementState();  GameplayTag GetDesiredMovementState();
    void StartDeath();  void FinishDeath();  bool IsDead();
}
// Resolve one from anywhere:
var combat = CombatInterface.Get(gameObject);   // or Get(component)
```

> The samples ship a ready **`CombatCore`** bridge that implements `ICombatInterface` by forwarding to
> the movement/combat components on the object.

---

## 12. Damage & action selection

**Damage** — `DamageExecutionCalculation` (*Create → Sigil → Combat → Damage Execution*) is a
`GameplayEffectExecutionCalculation`:

```
final = (source Damage + SetByCaller damage) − target DamageNegation
        − (if blocking) GuardDamageNegation      → written to IncomingDamage (a meta attribute)
```

The attribute names (`SourceDamageName` / `DamageNegationName` / `GuardDamageNegationName` /
`IncomingDamageName`) are all configurable. Put it in the damage GE's *Executions*; subclass or replace
it for your own formula.

**Action selection** — `AbilityActionLibrary` (*Create → Sigil → Combat → Ability Action Library*) picks
an animation/action set by ability tag + source/target state (combo selection). Query it via
`CombatSystemComponent.QueryAbilityActions(...)` or the convenience `PlayAbilityActionByTag(tag)`. An
`AbilityAction` carries an `AnimationClip` / `StateName`, `PlayRate`, root-motion scale, start time, and
an optional cost effect.

---

## 13. Samples

- **Playable Demo** (`Samples~/PlayableDemo`) — a ready-baked, playable scene showing the full loop
  (melee / ranged / lock-on / poise / stacking) on the core + this package. **No movement package needed.**
  Copyable abilities: `DemoMeleeAbility`, `DemoFocusAbility`, …
- **Combat Demo** (`Samples~/CombatDemo`) — an **integration demo** composing this package with the
  movement companion (`com.likeon.gas.movement`): third-person locomotion + weapon switching, attack
  polymorphism, a Flash blink, a hold-to-charge Fireball, and threat-AI enemies. Ships the `CombatCore`
  bridge and copyable `GA_MeleeAttack` / `GA_RangedAttack` / `GA_DashAttack` / `GA_Flash` / `GA_Fireball`.
  **Requires `com.likeon.gas.movement`.**

Import via Package Manager → Samples. The scenes are baked by editor generators whose code doubles as a
complete wiring reference; smoke tests ship with the samples. Presentation is placeholder programmer art.

---

## 14. Editor cheat sheet

Create-menu assets — authored **in the Editor, no code** (*Create → Sigil → Combat → …*, right-click in the Project window):

| Menu item | What it is |
|---|---|
| **Attack Definition** | What to apply on hit (effects, cues, knockback / hit-stop). Enhanced inspector. |
| **Bullet Definition** | Projectile (speed / spread / penetration / bullet chains). |
| **Combat Settings** | Project-level combat config (mesh-lookup tag, affiliation debug toggle). Set the active one with `CombatSettings.SetActive(...)`. |
| **Ability Action Library** | Pick actions by source/target state (combo selection). |
| **Damage Execution** | A negation + guard-damage model (`GameplayEffectExecutionCalculation`); subclass/replace it. |

**Tool:** *GameObject ▸ Sigil ▸ Combat Character Setup* (or *Sigil ▸ GAS ▸ Setup ▸ Add Combat Character Components*)
adds the common combat components to the selected object(s) in one click — `AbilitySystemComponent` +
`CombatTeamAgent` + `CombatSystemComponent` + `PoiseComponent` (idempotent, undoable). Components stay
separate and single-responsibility; add `TargetingSystemComponent` / `MeleeAttackTrace` / `WeaponComponent`
per character as needed.

Attribute sets are **not** shipped here — combat resolves attributes by name; generate your own with the
core's **Attribute Set Definition** codegen (§4). For core assets/tools (effects, loadouts, input, tags,
GAS Debugger, codegen), see the core package's usage guide §21.
