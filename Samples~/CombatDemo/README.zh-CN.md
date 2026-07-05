# Sigil Movement · 战斗 Demo

[English](README.md) | [简体中文](README.zh-CN.md)

把 Movement Demo 的运动/输入和核心 GAS 战斗流程拼在一起的战斗示例。你用 **WASD + 鼠标视角**
（沿用 Movement Demo 的输入链路）操控第三人称角色，在**近战与远程武器**间切换，并使用一组技能——
**冲刺攻击**、**闪现（Flash）**、**长按蓄力火球**——对付会追你并反击的简单威胁型 AI 敌人。
一切都用 GAS 特性实现：技能、消耗/冷却效果、`SetByCaller` 伤害、授予标签式眩晕、削韧。

## 运行

本示例开箱带 **烘好的场景 + 玩家/敌人 prefab**：

1. 打开本目录下的 **`CombatDemo.unity`**。
2. 按 **Play**。
3. 操作：
   - **WASD** 移动 · **鼠标** 视角 · **Shift** 冲刺开关 · **Esc** 释放鼠标
   - **左键** 攻击——多态：*冲刺 + 近战* = 冲刺攻击，*近战* = 普通攻击，*远程* = 发射子弹
   - **Tab** 切换近战 ⇄ 远程武器
   - **右键长按** 火球——蓄力时地面出现 AoE 光标，**松手**释放。蓄得越久，爆炸/伤害/眩晕越强。
   - **Q** 闪现（朝移动方向瞬移）· **E** 锁定

### 重新烘焙（可选）

场景、`CombatDemoPlayer` / `CombatDemoEnemy` prefab（在 `Resources/` 下）以及全部数据资产
（效果 / 攻击 / 子弹 / 技能 / 输入配置 / 装载 / 移动定义）都由一个编辑器脚本生成。要重新生成
（如改了 `.inputactions` 后），运行 **Sigil ▸ GAS ▸ Samples ▸ Build Combat Demo Scene**
（幂等；Console 会打印输出路径）。

## 展示了什么

- **复用运动 + 输入**——与 Movement Demo 同一套 `InputProcessor_Move` / `InputProcessor_Look` /
  `InputProcessor_ToggleAbilityByTag`（Shift → `GA_M_Sprint`）链路，跑在 `CharacterMovementSystemComponent`
  + 第三人称相机上。
- **单键攻击多态**——`InputControlSetup` 用 `FirstOnly` 模式，每个攻击处理器带一个 `GameplayTagQuery`
  状态门控：`Sprint + Weapon.Melee` → `GA_DashAttack`，`Weapon.Melee` → `GA_MeleeAttack`，
  `Weapon.Ranged` → `GA_RangedAttack`。冲刺状态 = 移动系统镜像到 ASC 的 `Movement.State.Sprint` 标签；
  武器标签由装备的 `WeaponComponent` 注入。
- **武器切换**——Tab 广播一个 `GameplayEvent`；`CombatDemoController` 装上另一把 `WeaponComponent`，
  它把自己的 `Weapon.*` 标签注入 ASC，重新驱动攻击多态。
- **闪现**（`GA_Flash`）——经移动组件的 `Teleport` 入口，朝移动输入方向瞬移；用体力消耗 + 冷却效果门控。
- **火球**（`GA_Fireball`）——按下激活，蓄力时逐帧（`AbilityTick`）显示地面 AoE 光标，用
  `AbilityTask_WaitInputPress(Canceled)` 等松手，然后发射一枚**伤害 / AoE 半径 / 眩晕时长随蓄力缩放**
  的投射物。命中时施加伤害 `GameplayEffect`（`SetByCaller`）+ 一个 Duration 眩晕效果，其
  `GrantedTags` = `State.Stunned`。眩晕纯用 GAS 状态实现：挡住目标的技能激活（`ActivationBlockedTags`）
  并冻结其 AI。
- **双向削韧 / 眩晕**——玩家与敌人都带 `AS_Poise` + `PoiseComponent`，重击会把韧性削破成
  `State.Staggered` 硬直，双方被眩晕/破防时技能都被挡住。
- **威胁型敌人 AI**（`CombatDemoEnemyAI`）——追击玩家（驱动移动组件），进入攻击距离后按冷却激活近战技能，
  被眩晕或破防时冻结，血量归零则死亡。

> 依赖核心包 `com.likeon.gas`。占位白模美术（地面 + 胶囊）；真实战斗手感（动画/特效/相机/顿帧）需要你自己的
> 表现层——这里的技能只保证逻辑正确（一个正确的白模）。
