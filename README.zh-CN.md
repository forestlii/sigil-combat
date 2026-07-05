# Sigil Combat — 近战 / 远程 / 攻击配套包

[English](README.md) | [简体中文](README.zh-CN.md)

[Sigil](https://github.com/forestlii/sigil-gas)（`com.likeon.gas`）的**配套包**，提供近战与远程的**战斗**层。
刻意与 GAS 核心分开：战斗是搭在能力系统之上的一个*领域*，不是核心机制的一部分——所以你可以用 Sigil 核心配
自己的战斗，或用本包配 Sigil。它与移动配套包（`com.likeon.gas.movement`）**平级**：两者互相独立、互不依赖。

- **依赖：** `com.likeon.gas`（Sigil 核心）
- **命名空间：** `Likeon.GAS`（与核心一致，不会打断你的 `using`）
- **引擎：** Unity 6（6000.4）
- **许可证：** MIT

## 安装

本包依赖 Sigil 核心包，两个都要装：

1. 先加 `com.likeon.gas`（Sigil 核心）。
2. 再加 `com.likeon.gas.combat`（本包）。

（Package Manager → *Add package from disk…* → 分别选各自的 `package.json`。）

### 跑测试

包内 `Tests/` 带 EditMode + PlayMode 测试。把本包加入工程 `Packages/manifest.json` 的 `"testables"`，
再开 **Window → General → Test Runner**：

```json
"testables": [ "com.likeon.gas.combat" ]
```

## 功能

- **攻击定义与施加** — `AttackDefinition`（SO）描述命中后施加的效果/标签；`AttackApplication` 构建
  `GameplayEffectSpec` 并施加到目标。
- **命中检测** — `MeleeAttackTrace`（形状扫掠）与 `CollisionTrace`（通用碰撞 trace）产出命中。
- **战斗流程管线** — `CombatSystemComponent` + `CombatFlow/`（`AttackRequest` → `AttackResult` →
  `AttackResultProcessor` 系列）把一次攻击转成命中结果并路由（伤害/死亡等），配 `AbilityActionLibrary`
  做标签门控的能力动作。
- **韧性与破防** — `PoiseComponent` 按名跟踪韧性属性并广播破防事件。
- **锁定 / 目标切换** — `TargetingSystemComponent` 查找并轮换战斗目标（死亡目标按属性名过滤）。
- **武器与子弹** — `IWeapon` / `WeaponComponent`（标签注入、源对象）与 `Bullet/`
  （`BulletDefinition` / `BulletInstance` / `BulletLauncher`）投射物。
- **伤害模型** — `DamageExecutionCalculation`，一个按属性名实现减伤+格挡的
  `GameplayEffectExecutionCalculation`（一种可继承/替换的默认战斗模型）。
- **战斗契约** — `ICombatInterface` 是其它系统查询目标/武器/防御输入/移动输入方向/移动模式/死亡生命周期
  的接缝。由你的角色实现（或用示例里的桥接组件）。

### 按属性名解析、不写死属性集

战斗系统按**名字**读写属性（如 `Poise` / `MaxPoise` / `PoiseRecover`、`Damage` / `DamageNegation` /
`GuardDamageNegation` / `IncomingDamage`），不绑定具体 `AttributeSet` 类型。用核心的 **codegen** 工具生成
你自己的属性集、遵循推荐命名约定即可——combat 不附带、也不要求任何特定 `AS_*`。

### 编辑器速查

**在编辑器里配、零代码** —— *Create → Sigil → Combat → …*：**Attack Definition**、**Bullet Definition**、**Combat Settings**、**Ability Action Library**、**Damage Execution**。（属性集用核心的 Attribute Set Definition codegen 生成。）**工具：** *GameObject ▸ Sigil ▸ Combat Character Setup* 一键给角色补齐常用战斗组件（ASC + 阵营 + 战斗中枢 + 削韧）。完整表格见[使用文档](Documentation~/Usage.zh-CN.md) §6。

## 示例

- **Playable Demo**（`Samples~/PlayableDemo`）——可玩功能展示（玩家/敌人 prefab + 场景），演示
  输入 → 技能 → 近战/远程命中 → 扣血 → GameplayCue、锁定切换、削韧破防、buff 叠层，全由数据驱动的
  loadout 驱动。导入后打开 `PlayableDemo.unity` 按 Play。
- **Combat Demo**（`Samples~/CombatDemo`）——一个**集成 demo**，把本包与移动配套包
  （`com.likeon.gas.movement`）组合起来：第三人称运动 + 武器切换（Tab）、攻击多态（冲刺/普攻/远程）、
  闪现（Q）、长按蓄力火球（伤害+眩晕随蓄力缩放）、威胁型 AI 敌人。随附 `CombatCore` 桥接件
  （`ICombatInterface` 实现）。**需要 `com.likeon.gas.movement`。**

## 许可证

[MIT](LICENSE.md) —— 任何用途（含商用）免费，保留版权声明即可。
