# Sigil Combat — 使用文档

[English](Usage.md) | [简体中文](Usage.zh-CN.md)

本配套包在 Sigil GAS 核心（`com.likeon.gas`）之上加了近战与远程的**战斗**层。核心概念（能力/效果/
属性/标签/输入）见核心包自己的 `Documentation~/使用文档`。本文只讲战斗层。

## 1. 战斗层的位置

```
Sigil 核心 (com.likeon.gas)        ← 能力 / 效果 / 属性 / 标签 / 输入
        ▲                    ▲
        │                    │
   Sigil Combat         Sigil Movement    ← 两个互相独立的领域配套包
  （本包）             (com.likeon.gas.movement)   （彼此不依赖）
```

Combat 只依赖核心。它按**名字**读写属性，所以能和你用核心 codegen 工具生成的任意属性集组合。

## 2. 属性名约定

战斗系统按名字解析属性（绝不绑定具体 `AttributeSet` 类型）。用核心的 codegen（*Sigil ▸ GAS ▸ …*）
生成暴露这些名字的属性集：

| 系统 | 读/写的属性名 |
|---|---|
| `PoiseComponent` | `Poise`、`MaxPoise`、`PoiseRecover` |
| `DamageExecutionCalculation` | `Damage`、`DamageNegation`、`GuardDamageNegation`、`IncomingDamage` |
| `TargetingSystemComponent`（死亡过滤） | `Health` |

这些名字在组件/执行上可配；上面的默认值对齐原战斗框架。只有*名字*要紧——combat 不在乎它们在哪个集里。

## 3. 攻击流程一览

1. 一个技能（或 `WeaponComponent`）经 `CombatSystemComponent` 发起 **AttackRequest**。
2. 命中检测（`MeleeAttackTrace` / `CollisionTrace`）产出目标。
3. `AttackDefinition` 描述命中后施加什么；`AttackApplication` 构建 `GameplayEffectSpec` 并施加。
4. `AttackResultProcessor` 系列路由结果（伤害经 `DamageExecutionCalculation`、死亡等）。
5. `PoiseComponent` 跟踪韧性并广播破防；`TargetingSystemComponent` 处理锁定。

## 4. 战斗契约（`ICombatInterface`）

其它系统经 `ICombatInterface` 查询角色——目标、当前武器、防御输入、移动输入方向、移动模式、死亡生命周期。
由你的角色实现。**Playable Demo** 与 movement 包的 **Combat Demo** 展示了一个桥接组件（`CombatCore`）：
它转发到物体上在场的移动/战斗组件来实现这个接口。

## 5. 示例

从 Package Manager 导入 **Playable Demo**（`Samples~/PlayableDemo`），一个烘好即玩的场景，
在核心 + 本包上展示完整循环（近战/远程/锁定/韧性/叠层）。

## 6. 另见

- 核心 `Documentation~/使用文档` — 能力、效果、属性（含 **codegen**）、标签、输入、配方。
- `com.likeon.gas.movement` — 移动配套包（在 Combat Demo 里与 combat 搭配）。
```
