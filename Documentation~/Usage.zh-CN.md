# Sigil Combat — 使用指南

[English](Usage.md) | [简体中文](Usage.zh-CN.md)

> 近战 & 远程**战斗**配套包 `com.likeon.gas.combat`，叠在 Sigil GAS 核心（`com.likeon.gas`）之上。
> 核心概念（技能、效果、属性、标签、输入）见核心包自己的 `Documentation~/Usage.md`。本文只讲战斗层。
>
> 战斗只依赖核心，且按**属性名**读写属性——所以能和你用核心 codegen 生成的任何属性集组合。命名空间是 `Likeon.GAS`。

## 目录

1. [战斗在架构里的位置](#1-战斗在架构里的位置)
2. [核心概念速览](#2-核心概念速览)
3. [快速上手](#3-快速上手)
4. [属性名约定](#4-属性名约定)
5. [攻击管线](#5-攻击管线)
6. [近战攻击](#6-近战攻击)
7. [远程攻击 & 子弹](#7-远程攻击--子弹)
8. [锁定](#8-锁定)
9. [削韧 & 破防](#9-削韧--破防)
10. [武器 & 换武器](#10-武器--换武器)
11. [阵营 & 战斗契约](#11-阵营--战斗契约)
12. [伤害 & 动作选择](#12-伤害--动作选择)
13. [示例](#13-示例)
14. [编辑器速查](#14-编辑器速查)

---

## 1. 战斗在架构里的位置

```
Sigil 核心 (com.likeon.gas)        ← 技能 / 效果 / 属性 / 标签 / 输入
        ▲                    ▲
        │                    │
   Sigil Combat         Sigil Movement    ← 两个独立的领域配套包
  (本包)               (com.likeon.gas.movement)   (互不依赖)
```

战斗只依赖核心。它从不引用具体的 `AttributeSet` 类型——按名解析属性，所以对你生成的任何属性集都适用。

---

## 2. 核心概念速览

| 概念 | 在这里的含义 |
|---|---|
| **属性按名解析** | 削韧/伤害/生命都按*字符串名*读，不绑固定 C# 类型——战斗不带任何属性集；你用核心 codegen 生成自己的（§4）。 |
| **AttackDefinition = "命中施加什么"** | 一个数据资产（效果、cue、击退、顿帧）。命中检测产出目标，再把攻击施加到每个目标（§5）。 |
| **各自单一职责的组件** | 阵营/削韧/命中判定/锁定/武器是分开的、可组合的组件，不是一个 God 组件。一键 setup 工具帮你补齐常用一套（§3）。 |
| **战斗契约（`ICombatInterface`）** | 各系统通过一个由角色实现的接口查询它（目标/武器/移动模式/死亡）——把战斗与你具体的移动/控制器解耦。 |

---

## 3. 快速上手

**1）造一个战斗角色** —— 一键工具补齐常用一套。选中一个 GameObject，然后
*GameObject ▸ Sigil ▸ Combat Character Setup*（幂等、可撤销）。它加上：
`AbilitySystemComponent` + `CombatTeamAgent` + `CombatSystemComponent` + `PoiseComponent`。按需再给角色加
`TargetingSystemComponent` / `MeleeAttackTrace` / `WeaponComponent`。

**2）给它属性** —— 生成一个暴露战斗所需名字的属性集（§4），如
`Health` / `Damage` / `DamageNegation` / `IncomingDamage` / `Poise` / `MaxPoise` / `PoiseRecover`。

**3）第一个近战攻击** —— 示例用的模式（可直接抄的真实版本在
`Samples~/CombatDemo/GA_MeleeAttack.cs`）：

```csharp
using Likeon.GAS;

public class GA_MeleeAttack : GameplayAbility
{
    [SerializeField] private int   traceEntryIndex = 0;   // 用哪条 MeleeAttackTrace 配置
    [SerializeField] private float traceWindow     = 0.3f; // 判定窗口开多少秒

    protected override void OnActivateAbility(GameplayEventData triggerData)
    {
        if (!CommitAbility()) { EndAbility(true); return; }   // 扣消耗 + 进冷却

        var trace = ASC.GetComponent<MeleeAttackTrace>();
        if (trace == null) { EndAbility(); return; }
        trace.BeginAttackTrace(traceEntryIndex);              // 开判定窗口

        var wait = AbilityTask_WaitDelay.WaitDelay(this, traceWindow);
        wait.OnFinish += () => { trace.EndAttackTrace(); EndAbility(); };
        wait.Activate();
    }
}
```

生产里你用攻击动画上的 **Animation Event** 驱动 `BeginAttackTrace` / `EndAttackTrace`，而非固定延时——见 §6。

---

## 4. 属性名约定

战斗系统按名解析属性（从不按具体 `AttributeSet` 类型）。用核心 codegen（*Sigil ▸ GAS ▸ …*）生成暴露这些名字的属性集。
这些名字在组件/execution 上**可配**；下表默认值对齐原战斗框架。

| 系统 | 它读写的属性名 |
|---|---|
| `PoiseComponent` | `Poise`、`MaxPoise`、`PoiseRecover` |
| `DamageExecutionCalculation` | `Damage`、`DamageNegation`、`GuardDamageNegation`、`IncomingDamage` |
| `TargetingSystemComponent`（死亡过滤） | `Health` |
| `AttackResultProcessor_Death` | `Health` |

只有*名字*要紧——战斗不在乎它们住在哪个属性集里。

---

## 5. 攻击管线

端到端流程，以及每一步的类型：

1. 一个技能（或 `WeaponComponent`）开启**命中检测** —— `MeleeAttackTrace`（近战）或子弹（`BulletLauncher`）。
2. 命中时，**`AttackApplication.ApplyAttack(...)`** 从 `AttackDefinition` 构造 `GameplayEffectSpec` 并施加给目标
   （伤害 GE + SetByCaller + 效果容器 + cue）。
3. 产出一个 **`AttackResult`**，交给目标的 `CombatSystemComponent.RegisterAttackResult(result)`。
4. 这触发 **`CombatSystemComponent.OnAttackResultReceived`**，由一个 **`CombatFlowComponent`** 监听，
   对它跑 **`AttackResultProcessor`** 处理器链（死亡 → 游戏事件 → cue → …）。

### `AttackDefinition` —— "命中施加什么"

*Create → Sigil → Combat → Attack Definition*（有增强 inspector）。字段：

| 字段 | 含义 |
|---|---|
| `AttackTags` | 攻击类型标签（近战/远程/劈砍/…）作为动态资产标签注入 GE spec。 |
| `SetByCallerMagnitudes` | 标签→数值对，作为 SetByCaller 注入 spec（如伤害、削韧伤害）。 |
| `TargetEffect` | 命中施加的主 `GameplayEffect`（伤害 GE）。 |
| `TargetEffectLevel` | 目标效果等级（`< 1` = 用技能等级）。 |
| `TargetEffectContainer` | 命中时给目标施加的额外效果。 |
| `TargetGameplayCues` | 在命中点播的 cue 标签。 |
| `KnockbackDistance` / `KnockbackMultiplier` | 击退调参。 |
| `HitStallingDuration` / `HitPlayRateFactor` | 顿帧：命中时冻结/减速 animator（`≤ 0` = 关；因子 `0.1–0.9`）。 |

### `AttackResult` & 处理器

`AttackResult` 携带 `ImpactResult`（Hit/Blocked/…）、`TaggedValues`（属性值如伤害，`GetTaggedValue(tag)`）、
`EffectContext`、聚合的来源/目标标签、`HitLocation`、`Consumed`。

`CombatFlowComponent`（`[RequireComponent(CombatSystemComponent)]`）持有一个 `[SerializeReference]`
的 `AttackResultProcessor` 列表，按序执行。内置处理器：

- **`AttackResultProcessor_Death`** —— 读 `HealthAttributeName`（默认 `Health`），生命 ≤ 0 时挂 `DeadTag`、
  广播 `DeathEventTag`。
- **`AttackResultProcessor_GameplayEvent`** —— 按来源/目标标签查询过滤后，把 `EventTriggers` 发给攻击方或受击方
  （如发一个 `Event.OnHit` 激活受击反应技能）。
- **`AttackResultProcessor_GameplayCue`** —— 在命中点执行 `GameplayCues`。

`CombatSystemComponent` 本身暴露：`RegisterAttackResult(result)`、`NotifyDealtDamage(result)`、
`ApplyHitStop(duration, playRateFactor)`、`PlayAttackAction(action)`、`QueryAbilityActions(...)`、
`PlayAbilityActionByTag(tag, targetTags = null)`；事件 `OnAttackResultReceived` / `OnDealtDamage`；以及
`Animator` / `AbilitySystem` / `ActionLibrary` / `LastProcessedAttackResult` 属性。

---

## 6. 近战攻击

`MeleeAttackTrace` 在判定窗口打开期间对一组 socket 做球扫，一次挥砍内对目标去重（每个目标只命中一次），
过滤自身 + 友方，然后施加攻击。

```csharp
trace.SetSource(attackerASC, attackerCombat);   // 通常自动接线
trace.BeginAttackTrace(entryIndex);             // 开窗口
// … 挥砍动画播放 …
trace.EndAttackTrace();                          // 关窗口
```

`trace.Entries` 里每条 `AttackTraceEntry` 把一个 `AttackDefinition` 和一个 `CollisionTraceDefinition`
（socket + 半径 + 命中层）配对。**用攻击动画上的 Animation Event 驱动窗口**：

```
Animation Event @ 0.10s → BeginAttackTrace(0)
Animation Event @ 0.40s → EndAttackTrace()
```

要更底层、不施伤的命中检测（陷阱、AoE、环境），用 **`CollisionTrace`**：`SetSockets(...)`、
`ToggleTraceState(bool)`、一个 `OnHit(Collider)` 事件、可选的 `HitFilter` 谓词——它只报告命中，施加什么由你定。

> 可直接抄的 `GA_MeleeAttack` 在 `Samples~/CombatDemo` 里。

---

## 7. 远程攻击 & 子弹

`BulletDefinition`（*Create → Sigil → Combat → Bullet Definition*）描述一枚弹丸：

| 字段 | 含义 |
|---|---|
| `Duration` | 存在秒数（`≤ 0` = 无限）。 |
| `BulletCount` / `LaunchAngle` / `LaunchAngleInterval` / `LaunchElevationAngle` | 散射样式（霰弹/扇形）。 |
| `InitialSpeed` / `GravityScale` | 运动（0 重力=直线，1=抛物线）。 |
| `HitRadius` / `HitLayers` | 命中球 + 层。 |
| `PenetrateCharacter` / `PenetrateMap` | 命中后是否穿透。 |
| `Attack` | 命中施加的 `AttackDefinition`。 |
| `HitBulletDefinition` | 命中/失效时生成的后续子弹（爆裂/分裂）。 |

在技能里用 `BulletLauncher`（静态）发射。它按基准方向生成 `BulletCount` 发子弹并返回 `BulletInstance`：

```csharp
protected override void OnActivateAbility(GameplayEventData triggerData)
{
    if (!CommitAbility()) { EndAbility(true); return; }

    Vector3 dir = muzzle.forward;
    var target = CombatInterface.Get(ASC.gameObject)?.GetCombatTargetActor();
    if (target != null) dir = (target.transform.position - muzzle.position).normalized;

    var bullets = BulletLauncher.Fire(bulletDef, ASC.gameObject, ASC, muzzle.position, dir);
    // 伤害由 bulletDef.Attack 在命中时施加；要额外逻辑就订阅：
    foreach (var b in bullets) b.OnHit += (inst, targetASC, point) => { /* targetASC == null → 命中地图 */ };

    EndAbility();
}
```

`BulletLauncher.Fire` 有两个重载（基准 `Quaternion` 或 `Vector3` 方向）。单个 `BulletInstance` 还暴露
`Launch(...)`、`Tick(dt)`（关掉 `AutoTick` 可手动驱动，如测试里）、以及 `OnHit` / `OnExpired` 事件。

> 可直接抄的 `GA_RangedAttack` / `GA_Fireball`（蓄力火球）在 `Samples~/CombatDemo` 里。

---

## 8. 锁定

`TargetingSystemComponent` 在范围内找到并锁定最佳目标，遵守视角、阵营、死亡过滤。

```csharp
targeting.ToggleLock();                       // 锁定最佳 / 解锁（锁定键）
targeting.StaticSwitchToNewTarget(true);      // 切到右边的相邻目标（false = 左）
GameObject t  = targeting.TargetedActor;      // 当前锁定（无则 null）
bool locked   = targeting.IsLockedOn;

targeting.OnTargetLockOn  += target => { /* HUD：显示准星 */ };
targeting.OnTargetLockOff += target => { /* HUD：清除准星 */ };
```

运行时可调：`SearchRadius`、`MaxViewAngle`（视锥，度；`> 0` 启用）、`OnlyHostile`、`FilterDead`、
`ViewSource`（相机）、`RequiredTags` / `BlockedTags`。需要更底层的 API：`SearchForActorToTarget()`、
`RefreshPotentialTargets()`、`SelectBestActor()` / `SelectClosestActor()`、`SetTargetedActor(go)`、
`CanBeTargeted(go)`、`CalculateViewAngle(go)`。

---

## 9. 削韧 & 破防

`PoiseComponent` 跟踪一个削韧属性；归零时角色破防（硬直），延迟后恢复。

```csharp
poise.OnPoiseBroken    += () => { /* 破绽——处决窗口 */ };
poise.OnPoiseRecovered += () => { /* 恢复正常 */ };
bool staggered = poise.IsStaggered;
```

可配：属性名 `poiseName` / `maxPoiseName` / `poiseRecoverName`（默认 `Poise` / `MaxPoise` / `PoiseRecover`）、
`staggeredTag`（默认 `State.Staggered`）、可选的 `staggerEffect`、`staggerDuration`（运行时 `StaggerDuration`）、
`recoverDelay`（`RecoverDelay`）。削韧伤害 = 把削韧值放进攻击的 `SetByCallerMagnitudes` 并修改削韧属性。

机制：削韧被减 → 恢复延迟计时重置 → 削韧 ≤ 0 → **Break**（挂硬直标签 + 可选效果，触发 `OnPoiseBroken`）
→ `staggerDuration` 后 → **Recover**（解标签、削韧回满，触发 `OnPoiseRecovered`）。

---

## 10. 武器 & 换武器

`WeaponComponent`（实现 `IWeapon`）持有武器标签、枪口、近战判定实例。装备时把武器标签注入 owner ASC——
这正是**单个攻击键变多态**的原理（一个 `GameplayTagQuery` 门控的输入处理器，按当前存在的 `Weapon.*` 标签
选近战还是远程技能）。

```csharp
current?.Unequip();          // 从 owner ASC 移除它的武器标签
next.Equip(gameObject);      // 关联 owner + ASC、注入 weaponTags、重设判定实例来源
current = next;
// 现在 owner 有如 Weapon.Ranged → 攻击键经 tag 门控的 InputProcessor_ActivateAbilityByTag
// （StateQuery 含 Weapon.Ranged）激活 GA_RangedAttack。
```

关键成员：`Equip(owner, attachSocket = null)`、`Unequip()`、`SetWeaponActive(bool)`、
`SetTargeting(bool)` / `ToggleTargeting()`、`RefreshTraceInstances()`、`FireProjectile(BulletDefinition)`；
事件 `OnEquipped` / `OnUnequipped` / `OnWeaponActiveStateChanged` / `OnTargetingChanged`。`IWeapon` 接口
（`WeaponOwner`、`WeaponTags`、`IsWeaponActive`、`MuzzleTransform`、`SourceObject`、`IsTargeting` 等）让别的
系统抽象地查询当前武器。

---

## 11. 阵营 & 战斗契约

**`CombatTeamAgent`**（`ITeamAgent`）赋一个 `TeamId`；战斗用它辨敌我：

```csharp
agent.SetTeamId(1);
ETeamAttitude att = agent.GetAttitudeTowards(otherGo);   // Friendly / Hostile / Neutral
bool hostile = CombatTeamAgent.IsHostile(sourceGo, targetGo);  // 静态便捷
```

同 id → Friendly，不同（都 ≥ 0）→ Hostile，任一 `-1` → Neutral。`CombatSettings.DisableAffiliationCheck`
可关掉检查用于调试。

**`ICombatInterface`** 是各战斗系统查询的、面向角色的契约。你的角色（或示例的 `CombatCore` 桥接件）实现它：

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
// 从任意处解析一个：
var combat = CombatInterface.Get(gameObject);   // 或 Get(component)
```

> 示例自带一个开箱的 **`CombatCore`** 桥接件，通过转发到对象上的移动/战斗组件来实现 `ICombatInterface`。

---

## 12. 伤害 & 动作选择

**伤害** —— `DamageExecutionCalculation`（*Create → Sigil → Combat → Damage Execution*）是一个
`GameplayEffectExecutionCalculation`：

```
最终 = (来源 Damage + SetByCaller 伤害) − 目标 DamageNegation
       − (若格挡) GuardDamageNegation        → 写入 IncomingDamage（一个 meta 属性）
```

属性名（`SourceDamageName` / `DamageNegationName` / `GuardDamageNegationName` / `IncomingDamageName`）
全部可配。把它放进伤害 GE 的 *Executions*；子类化或替换成你自己的公式。

**动作选择** —— `AbilityActionLibrary`（*Create → Sigil → Combat → Ability Action Library*）按能力标签 +
来源/目标状态选出一组动画/动作（连段选择）。经 `CombatSystemComponent.QueryAbilityActions(...)` 或便捷的
`PlayAbilityActionByTag(tag)` 查询。一个 `AbilityAction` 携带 `AnimationClip` / `StateName`、`PlayRate`、
RootMotion 缩放、起始时间、可选的消耗效果。

---

## 13. 示例

- **Playable Demo**（`Samples~/PlayableDemo`）—— 一个烘好、可直接玩的场景，在核心 + 本包上展示完整循环
  （近战 / 远程 / 锁定 / 削韧 / 叠层）。**不需要 movement 包。** 可抄的技能：`DemoMeleeAbility`、`DemoFocusAbility` 等。
- **Combat Demo**（`Samples~/CombatDemo`）—— 一个**集成 demo**，把本包和 movement 配套包
  （`com.likeon.gas.movement`）组合：第三人称运动 + 武器切换、攻击多态、闪现、蓄力火球、威胁型 AI 敌人。
  自带 `CombatCore` 桥接件和可抄的 `GA_MeleeAttack` / `GA_RangedAttack` / `GA_DashAttack` / `GA_Flash` / `GA_Fireball`。
  **需要 `com.likeon.gas.movement`。**

通过 Package Manager → Samples 导入。场景由编辑器生成器烘出，其代码本身就是一份完整接线参考；冒烟测试随示例发布。
表现是占位程序美术。

---

## 14. 编辑器速查

创建菜单资产 —— 全部**在编辑器里、零代码**创作（*Create → Sigil → Combat → …*，在 Project 窗口右键）：

| 菜单项 | 是什么 |
|---|---|
| **Attack Definition** | 命中施加什么（效果、cue、击退 / 顿帧）。增强 inspector。 |
| **Bullet Definition** | 弹丸（速度 / 散射 / 穿透 / 子弹链）。 |
| **Combat Settings** | 项目级战斗配置（网格查询标签、敌我检查调试开关）。用 `CombatSettings.SetActive(...)` 设当前生效实例。 |
| **Ability Action Library** | 按来源/目标状态选动作（连段选择）。 |
| **Damage Execution** | 减伤 + 格挡伤害模型（`GameplayEffectExecutionCalculation`）；可子类化/替换。 |

**工具：** *GameObject ▸ Sigil ▸ Combat Character Setup*（或 *Sigil ▸ GAS ▸ Setup ▸ Add Combat Character Components*）
一键给选中对象补齐常用战斗组件 —— `AbilitySystemComponent` + `CombatTeamAgent` + `CombatSystemComponent` +
`PoiseComponent`（幂等、可撤销）。组件仍各自独立、单一职责；按需再给角色加
`TargetingSystemComponent` / `MeleeAttackTrace` / `WeaponComponent`。

这里**不**带属性集——战斗按名解析属性；用核心的 **Attribute Set Definition** codegen 生成你自己的（§4）。
核心资产/工具（效果、装载、输入、标签、GAS Debugger、codegen）见核心包使用指南 §21。
