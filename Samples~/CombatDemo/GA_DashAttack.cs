// Copyright (c) 2026 Likeon. Licensed under the MIT License.
// 冲刺攻击技能：冲刺状态下按攻击键触发（由输入控制集 StateQuery=含 Movement.State.Sprint 门控多态到本技能）。
// 表现 = 向前一个短突进（经移动组件 Teleport）+ 一次更重的挥砍判定。

using UnityEngine;

namespace Likeon.GAS.Sample.CombatDemo
{
    /// <summary>冲刺攻击：向前突进 + 重挥砍。仅在冲刺状态由攻击键多态激活（见 CombatDemoBuilder 的输入控制集）。</summary>
    [CreateAssetMenu(fileName = "GA_DashAttack", menuName = "Sigil/GAS/CombatDemo/Dash Attack")]
    public class GA_DashAttack : GameplayAbility
    {
        [Header("突进")]
        [Tooltip("向前突进距离（米）")]
        public float LungeDistance = 2.5f;

        [Header("近战判定")]
        [Tooltip("用 MeleeAttackTrace.Entries 的哪一条（冲刺攻击=更重的一条）")]
        public int TraceEntryIndex = 1;
        [Tooltip("判定窗口时长（秒）")]
        public float TraceWindow = 0.35f;

        protected override void OnActivateAbility(GameplayEventData triggerData)
        {
            if (!CommitAbility()) { EndAbility(true); return; }

            // 向前突进（沿角色朝向），用移动组件的瞬移入口（会禁用 CC 再改位置，避免被碰撞覆盖）
            var mover = ASC.GetComponent<CharacterMovementSystemComponent>();
            if (mover != null)
            {
                Vector3 fwd = mover.transform.forward; fwd.y = 0f;
                if (fwd.sqrMagnitude > 0.0001f)
                    mover.Teleport(mover.transform.position + fwd.normalized * LungeDistance);
            }

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
