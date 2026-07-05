// Copyright (c) 2026 Likeon. Licensed under the MIT License.
// 闪现技能：向移动输入方向（无输入则朝角色面朝方向）瞬移一段固定距离。消耗/冷却走 GAS Cost/Cooldown 效果。
// 瞬移经移动组件 Teleport（禁用 CC → 改位置 → 启用 CC），并可选前向 SphereCast 防穿墙。

using UnityEngine;

namespace Likeon.GAS.Sample.CombatDemo
{
    /// <summary>闪现（Blink）：朝移动输入方向瞬移固定距离。用移动组件的 Teleport 入口，避免被 CharacterController 覆盖。</summary>
    [CreateAssetMenu(fileName = "GA_Flash", menuName = "Sigil/GAS/CombatDemo/Flash")]
    public class GA_Flash : GameplayAbility
    {
        [Header("闪现")]
        [Tooltip("瞬移距离（米）")]
        public float FlashDistance = 5f;
        [Tooltip("前向障碍检测半径（>0 时用 SphereCast 在障碍前停下，0=不检测）")]
        public float ObstacleCheckRadius = 0.4f;
        [Tooltip("障碍检测层（默认所有层，忽略触发器）")]
        public LayerMask ObstacleMask = ~0;

        protected override void OnActivateAbility(GameplayEventData triggerData)
        {
            if (!CommitAbility()) { EndAbility(true); return; }

            var mover = ASC.GetComponent<CharacterMovementSystemComponent>();
            if (mover != null)
            {
                // 方向：优先移动输入方向，其次角色面朝方向
                Vector3 dir = mover.GetInputDirection(); dir.y = 0f;
                if (dir.sqrMagnitude < 0.01f) dir = mover.transform.forward;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    dir.Normalize();
                    float dist = FlashDistance;

                    // 可选：前方有障碍就在障碍前停下。检测前临时禁用自身 CharacterController，
                    // 否则 SphereCast 会命中自己的碰撞体（hit.distance≈0）把位移削成 0。
                    if (ObstacleCheckRadius > 0f)
                    {
                        var cc = mover.GetComponent<CharacterController>();
                        bool ccWasEnabled = cc != null && cc.enabled;
                        if (cc != null) cc.enabled = false;

                        Vector3 origin = mover.transform.position + Vector3.up * 1f;
                        if (Physics.SphereCast(origin, ObstacleCheckRadius, dir, out var hit, FlashDistance, ObstacleMask, QueryTriggerInteraction.Ignore))
                            dist = Mathf.Max(0f, hit.distance - ObstacleCheckRadius);

                        if (cc != null) cc.enabled = ccWasEnabled;
                    }

                    Vector3 dest = mover.transform.position + dir * dist;
                    dest.y = mover.transform.position.y; // 平地 demo：保持高度
                    mover.Teleport(dest);
                    ASC.ExecuteGameplayCue(CombatDemoTags.Cue_FlashBlink);
                }
            }

            EndAbility();
        }
    }
}
