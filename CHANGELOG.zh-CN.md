# 更新日志

[English](CHANGELOG.md) | [简体中文](CHANGELOG.zh-CN.md)

Sigil Combat 的所有重要变更记录于此。
格式基于 [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)，遵循 [语义化版本](https://semver.org/)。

## [Unreleased]

### 修复

- **近战判定现在在技能被取消时也会关闭，而不只是自然结束时。** 示例近战技能（`GA_MeleeAttack`、`GA_DashAttack`、`DemoMeleeAbility`）靠 `AbilityTask_WaitDelay.OnFinish` 关判定，而任务被外部取消时 OnFinish 不触发——被打断的攻击会让 `MeleeAttackTrace` 一直激活、持续扫伤。现在它们在 `OnEndAbility` 里也关判定；`MeleeAttackTrace` 新增 `OnDisable` 清判定态与陈旧 socket 位置（禁用/再启用不会产生首帧超长扫掠）。
- **子弹不再透过复合碰撞体命中发射者自己。** 自身排除用 `col.attachedRigidbody.gameObject` 与 owner 比较，owner 的碰撞体挂在子物体、且无 Rigidbody 时会落空；叠加 `CombatTeamAgent.IsHostile` 对无阵营 owner 返回 true，子弹会命中并伤到发射者。自身排除改用 `Transform.IsChildOf(Owner)` + ASC 级判断（`targetASC == SourceASC`），与近战判定对齐。
- **武器换主/重复装备不再泄漏标签。** `WeaponComponent.Equip` 直接覆盖 owner 不清旧主，注入的计数式 `Weapon.*` 松散标签永不移除（对同一 owner 重复装备还会累加计数）。现在 Equip 先撤下当前已注入的标签（用标志位跟踪）；`Unequip` 也会把近战判定的来源置空，不再指向旧 ASC。
- **`MovementCancellation` 在动画被打断时不再永久禁用 root motion。** 窗口依赖 `BeginWindow`/`EndWindow` 动画事件配对；被打断的攻击不会发 `EndWindow`，`applyRootMotion` 卡在关闭。新增 `OnDisable` 恢复 root motion 并关窗，以及 `maxWindowSeconds` 超时（默认 5 秒）自动关闭开太久的窗口。
- **连续命中后顿帧不再让 Animator 永久变慢。** `ApplyHitStop` 用方法引用启动协程却用 string 版 `StopCoroutine` 去停（静默失败）；连续顿帧于是把彼此已降速的 `animator.speed` 当成恢复基准，动画卡在慢速。现在改为按句柄停协程、从捕获的基准速度恢复，并新增 `OnDisable`：组件在顿帧中途被禁用时无条件复位速度。
- **已死目标不再在每次补刀时重复广播死亡事件。** `AttackResultProcessor_Death` 对"加 `State.Dead` 标签"做了去重，却没对死亡 `GameplayEvent` 去重，尸体每被再命中一次（补刀、持续 AOE）就重发一次死亡事件、反复触发绑定其上的技能/表现。现在把去重守卫提前 return，整个处理器每次死亡只跑一次。

## [0.1.7] - 2026-07-14

### 修复

- **战斗 Demo —— 替换示例脚本里已弃用的场景查找 API。** `CombatDemoEnemyAI` 与 `CombatDemoSmokeTest` 用了 `FindObjectsOfType<T>()` / `FindObjectOfType<T>()`，在 Unity 6000.x 上已弃用。改用当前的 `FindObjectsByType<T>()` / `FindAnyObjectByType<T>()` 重载（不排序 —— 原逻辑本就不依赖顺序），消除 CS0618 警告。仅示例、无运行时/API 变更。

## [0.1.6] - 2026-07-09

### 变更

- **性能 —— 子弹池化（行为不变）。** `BulletLauncher` 现在从共享空闲池借子弹、失效时（命中停下 / 生命到期）回收复用，不再每发 `new GameObject` + `Destroy`，避免每次发射的 GC 分配。回收的子弹在复用前会重置（清事件订阅者、置非活跃）。直接 new（不经发射器）的子弹仍自销毁 —— 行为不变。新增 `BulletLauncher.ClearPool()`（测试 / 场景切换用）与 `BulletLauncher.PooledCount` 供调试。

## [0.1.5] - 2026-07-08

### 变更

- **把使用指南从概览扩成完整参考**，对齐核心包的详尽程度：快速上手、攻击管线解剖，以及近战、远程/子弹、锁定、削韧/破防、武器各系统的 API + 代码；`AttackDefinition` / `BulletDefinition` 字段表；完整的 `ICombatInterface` 契约；阵营；伤害/动作选择。纯文档——无运行时/API 变化。

## [0.1.4] - 2026-07-05

### 变更

- **Combat Demo —— 火球的地面光标改成 `AbilityTask`**（`AbilityTask_GroundReticle`），删掉了原来那个持久的 `CombatDemoFireballReticle` MonoBehaviour。任务掌管光标圆盘的整个生命周期：激活时建圆盘、每帧按技能给的 sampler 更新落点/半径，而且——因为框架会在技能结束时自动取消其任务——火球结束时**自动销毁圆盘**（无需手动 show/hide、无持久组件、零泄漏）。示范首选做法：优先用框架原语（AbilityTask）而不是新写一个专用组件。（需核心 ≥ 0.7.1，该版让 AbilityTask 可在核心程序集外派生。）

## [0.1.3] - 2026-07-05

### 新增

- **一键「Combat Character Setup」**（*GameObject ▸ Sigil ▸ Combat Character Setup*，或 *Sigil ▸ GAS ▸ Setup ▸ Add Combat Character Components*）。给选中对象补齐常用战斗组件（缺才加、可撤销）：`AbilitySystemComponent` + `CombatTeamAgent` + `CombatSystemComponent` + `PoiseComponent`。组件保持**各自独立、单一职责**——这是配置便利，**不是**合并的"战斗"大组件——所以角色专属件（`TargetingSystemComponent` / `MeleeAttackTrace` / `WeaponComponent`）仍按需另加。

## [0.1.2] - 2026-07-05

### 修复

- **Combat Demo —— 玩家/敌人 prefab 不再显示丢失（洋红）材质。** 场景生成器给胶囊染色时用了内存里的 `new Material(...)` 当 `sharedMaterial`，但它不是资产，`SaveAsPrefabAsset` 无法序列化 → prefab 材质槽存成了 `None` → 导入后渲染成洋红。生成器现在先把染色材质存成真 `.mat` 资产（`Samples~/CombatDemo/Materials/`）再赋值，让随包 prefab 引用持久材质。用 *Sigil ▸ GAS ▸ Samples ▸ Build Combat Demo Scene* 重烘。

## [0.1.1] - 2026-07-05

### 新增

- **Combat Demo 示例**（`Samples~/CombatDemo`）——从 movement 包迁来。这是组合本战斗包与移动配套包（`com.likeon.gas.movement`）的集成 demo：第三人称运动 + 武器切换、攻击多态（冲刺/普攻/远程）、闪现、蓄力火球（伤害+眩晕随蓄力缩放）、威胁型 AI 敌人——全部搭在 GAS 上。随附 `CombatCore` 桥接件（`ICombatInterface` 实现）。**需要 `com.likeon.gas.movement`**（示例级依赖；包本体仍不依赖 movement）。

## [0.1.0] - 2026-07-05

作为独立配套包的首个版本。**从 Sigil 核心包（`com.likeon.gas`）拆出**，让 GAS 核心回归纯能力系统框架，
恢复原战斗框架本就有的模块边界（能力/属性/战斗是三个独立模块）。命名空间不变（`Likeon.GAS`）。
与 `com.likeon.gas.movement` **平级**——两者互相独立、互不依赖。

### 新增

- **攻击管线** — `AttackDefinition`、`AttackApplication`、`AttackResult`、`AttackRequest`、
  `AttackResultProcessor` 系列、`CombatFlowComponent`、`CombatSystemComponent`、`AbilityActionLibrary`。
- **命中检测** — `MeleeAttackTrace`、`CollisionTrace`。
- **韧性** — `PoiseComponent`（按属性名）+ 破防事件。
- **锁定 / 目标切换** — `TargetingSystemComponent`。
- **武器与子弹** — `IWeapon`、`WeaponComponent`、`BulletDefinition`、`BulletInstance`、`BulletLauncher`。
- **战斗支撑** — `CombatTeamAgent`、`CombatSettings`、`CombatTypes`、`MovementCancellation`、
  `DamageExecutionCalculation`（按属性名减伤+格挡）以及 `ICombatInterface` 契约。
- **Playable Demo 示例**（`Samples~/PlayableDemo`）——从核心包移来；搭在核心 + 本包上的近战/远程/
  锁定/韧性/叠层功能展示。

### 说明

- 战斗按**名字**解析属性（不写死 `AS_*`）；用核心的 codegen 工具按推荐命名约定生成属性集。
- 需配套对应的 Sigil 核心版本（`com.likeon.gas` ≥ 0.7.0，该版已移除内置 `Combat/`）。

[0.1.4]: #014---2026-07-05
[0.1.3]: #013---2026-07-05
[0.1.2]: #012---2026-07-05
[0.1.1]: #011---2026-07-05
[0.1.0]: #010---2026-07-05
