// Copyright (c) 2026 Likeon. Licensed under the MIT License.
// 远程攻击技能：装远程武器时按攻击键触发（输入控制集 StateQuery=含 Weapon.Ranged 门控）。
// 从枪口发射一发子弹，朝锁定目标（有则）否则角色朝向；命中由子弹的 AttackDefinition 施伤。

using UnityEngine;

namespace Likeon.GAS.Sample.CombatDemo
{
    /// <summary>远程攻击：发射子弹（BulletLauncher）。伤害由子弹定义里的 AttackDefinition 承担。</summary>
    [CreateAssetMenu(fileName = "GA_RangedAttack", menuName = "Sigil/GAS/CombatDemo/Ranged Attack")]
    public class GA_RangedAttack : GameplayAbility
    {
        [Header("子弹")]
        [Tooltip("发射的子弹定义（命中施加其 AttackDefinition 的伤害）")]
        public BulletDefinition Bullet;

        protected override void OnActivateAbility(GameplayEventData triggerData)
        {
            if (!CommitAbility()) { EndAbility(true); return; }

            if (Bullet != null)
            {
                var ctrl = ASC.GetComponent<CombatDemoController>();
                Transform muzzle = ctrl != null && ctrl.Muzzle != null ? ctrl.Muzzle : ASC.transform;

                Vector3 dir = muzzle.forward;
                var target = ctrl != null && ctrl.Targeting != null ? ctrl.Targeting.TargetedActor : null;
                if (target != null)
                {
                    Vector3 to = target.transform.position - muzzle.position;
                    if (to.sqrMagnitude > 0.0001f) dir = to.normalized;
                }

                BulletLauncher.Fire(Bullet, ASC.gameObject, ASC, muzzle.position, dir);
            }

            EndAbility();
        }
    }
}
