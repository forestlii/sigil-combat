# 更新日志

[English](CHANGELOG.md) | [简体中文](CHANGELOG.zh-CN.md)

Sigil Combat 的所有重要变更记录于此。
格式基于 [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)，遵循 [语义化版本](https://semver.org/)。

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

[0.1.0]: #010---2026-07-05
