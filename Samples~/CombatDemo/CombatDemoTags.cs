// Copyright (c) 2026 Likeon. Licensed under the MIT License.
// Combat Demo 用到的全部 GameplayTag 常量（运行时脚本与 Editor 生成器共用）。
// 命名沿用项目既有域词：Movement.* 复用移动包，Weapon.* / State.* / Data.* / Ability.* / Input.* / Event.* / GameplayCue.* 自定。

namespace Likeon.GAS.Sample.CombatDemo
{
    /// <summary>战斗 demo 的 GameplayTag 常量集中定义。</summary>
    public static class CombatDemoTags
    {
        // ---- 输入标签（InputConfig 把 .inputactions 动作绑到这些逻辑标签）----
        public static readonly GameplayTag Input_Move = GameplayTag.RequestTag("InputTag.Move");
        public static readonly GameplayTag Input_Look = GameplayTag.RequestTag("InputTag.Look");
        public static readonly GameplayTag Input_Sprint = GameplayTag.RequestTag("Input.Movement.Sprint");
        public static readonly GameplayTag Input_Attack = GameplayTag.RequestTag("Input.Combat.Attack");
        public static readonly GameplayTag Input_SwitchWeapon = GameplayTag.RequestTag("Input.Combat.SwitchWeapon");
        public static readonly GameplayTag Input_Fireball = GameplayTag.RequestTag("Input.Combat.Fireball");
        public static readonly GameplayTag Input_Flash = GameplayTag.RequestTag("Input.Combat.Flash");
        public static readonly GameplayTag Input_LockOn = GameplayTag.RequestTag("Input.Combat.LockOn");

        // ---- 技能身份标签 ----
        public static readonly GameplayTag Ability_Sprint = GameplayTag.RequestTag("Ability.Movement.Sprint");
        public static readonly GameplayTag Ability_Melee = GameplayTag.RequestTag("Ability.Combat.Melee");
        public static readonly GameplayTag Ability_DashAttack = GameplayTag.RequestTag("Ability.Combat.DashAttack");
        public static readonly GameplayTag Ability_Ranged = GameplayTag.RequestTag("Ability.Combat.Ranged");
        public static readonly GameplayTag Ability_Flash = GameplayTag.RequestTag("Ability.Combat.Flash");
        public static readonly GameplayTag Ability_Fireball = GameplayTag.RequestTag("Ability.Combat.Fireball");
        public static readonly GameplayTag Ability_EnemyMelee = GameplayTag.RequestTag("Ability.Enemy.Melee");

        // ---- 武器标签（装备时注入持有者 ASC，供攻击键多态门控）----
        public static readonly GameplayTag Weapon_Melee = GameplayTag.RequestTag("Weapon.Melee");
        public static readonly GameplayTag Weapon_Ranged = GameplayTag.RequestTag("Weapon.Ranged");

        // ---- 状态标签 ----
        // 冲刺状态 = 移动包镜像到 ASC 的 Movement.State.Sprint（冲刺攻击门控用它，不另造）
        public static readonly GameplayTag State_Sprint = MovementTags.MovementState_Sprint;
        // 眩晕：火球命中授予的控制标签（挡住技能激活 + AI 冻结）
        public static readonly GameplayTag State_Stunned = GameplayTag.RequestTag("State.Stunned");
        // 破防硬直（PoiseComponent 削韧归零时挂）
        public static readonly GameplayTag State_Staggered = GameplayTag.RequestTag("State.Staggered");

        // ---- SetByCaller 数值标签（伤害管线）----
        public static readonly GameplayTag Data_Damage = GameplayTag.RequestTag("Data.Damage");
        public static readonly GameplayTag Data_PoiseDamage = GameplayTag.RequestTag("Data.PoiseDamage");

        // ---- 游戏事件标签（输入→事件→监听者）----
        public static readonly GameplayTag Event_SwitchWeapon = GameplayTag.RequestTag("Event.Combat.SwitchWeapon");
        public static readonly GameplayTag Event_ToggleLock = GameplayTag.RequestTag("Event.Combat.ToggleLock");

        // ---- 表现 Cue 标签 ----
        public static readonly GameplayTag Cue_Hit = GameplayTag.RequestTag("GameplayCue.Combat.Hit");
        public static readonly GameplayTag Cue_FireballExplode = GameplayTag.RequestTag("GameplayCue.Combat.FireballExplode");
        public static readonly GameplayTag Cue_FlashBlink = GameplayTag.RequestTag("GameplayCue.Combat.FlashBlink");
        public static readonly GameplayTag Cue_Stunned = GameplayTag.RequestTag("GameplayCue.Combat.Stunned");
    }
}
