// Copyright (c) 2026 Likeon. Licensed under the MIT License.
// 近战攻击技能：CommitAbility 扣消耗/进冷却 → 开 MeleeAttackTrace 判定窗口 → 等窗口结束关判定 + 结束技能。
// 玩家与敌人共用（敌人授予时也用它，判定下标不同即可）。

using UnityEngine;

namespace Likeon.GAS.Sample.CombatDemo
{
    /// <summary>普通近战攻击：走完整 GAS 路径（消耗/冷却由 Cost/Cooldown 效果驱动）并开启挥砍判定。</summary>
    [CreateAssetMenu(fileName = "GA_MeleeAttack", menuName = "Sigil/GAS/CombatDemo/Melee Attack")]
    public class GA_MeleeAttack : GameplayAbility
    {
        [Header("近战判定")]
        [Tooltip("用 MeleeAttackTrace.Entries 的哪一条（不同攻击=不同攻击定义）")]
        public int TraceEntryIndex = 0;
        [Tooltip("判定窗口时长（秒）")]
        public float TraceWindow = 0.3f;

        protected override void OnActivateAbility(GameplayEventData triggerData)
        {
            if (!CommitAbility()) { EndAbility(true); return; }

            var trace = ASC.GetComponent<MeleeAttackTrace>();
            if (trace == null) { EndAbility(); return; }

            trace.BeginAttackTrace(TraceEntryIndex);
            var wait = AbilityTask_WaitDelay.WaitDelay(this, TraceWindow);
            wait.OnFinish += () =>
            {
                trace.EndAttackTrace();
                EndAbility();
            };
            wait.Activate();
        }
    }
}
