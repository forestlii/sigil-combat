// Copyright (c) 2026 Likeon. Licensed under the MIT License.
// 火球技能（GAS 特性综合展示）：
//   按下激活 → 逐帧（AbilityTick）显示地面 AoE 光标并蓄力 → 用 AbilityTask_WaitInputPress 等松手 → 释放发射火球。
//   蓄力越久越强：伤害 / AoE 半径 / 眩晕时长随蓄力比例缩放（运行时克隆子弹/攻击/眩晕 SO，不改共享资产）。
//   命中：子弹的 AttackDefinition 施加伤害 + 削韧；额外对目标施加 Duration 眩晕效果（GrantedTags=State.Stunned）。
// 眩晕本身纯用 GAS 标签实现：被眩晕者的技能被 ActivationBlockedTags 挡住、AI 读标签冻结。

using UnityEngine;

namespace Likeon.GAS.Sample.CombatDemo
{
    /// <summary>蓄力火球：hold 显示光标蓄力、release 发射，命中造成伤害 + 眩晕。蓄力越久越强。</summary>
    [CreateAssetMenu(fileName = "GA_Fireball", menuName = "Sigil/GAS/CombatDemo/Fireball")]
    public class GA_Fireball : GameplayAbility
    {
        [Header("子弹 / 眩晕")]
        [Tooltip("火球子弹定义（其 AttackDefinition 承担伤害；本技能按蓄力克隆缩放）")]
        public BulletDefinition FireballBullet;
        [Tooltip("命中时对目标施加的眩晕效果（Duration，GrantedTags 应含 State.Stunned）")]
        public GameplayEffect StunEffect;

        [Header("蓄力")]
        [Tooltip("蓄满所需时长（秒）")]
        public float MaxChargeTime = 1.2f;
        [Tooltip("发射输入标签（等这个标签的 Canceled=松手）")]
        public GameplayTag FireInputTag = default;

        [Header("蓄力缩放（min=点按, max=蓄满）")]
        public float MinBlastRadius = 0.6f;
        public float MaxBlastRadius = 2.5f;
        public float MinStunDuration = 0.8f;
        public float MaxStunDuration = 2.5f;
        [Tooltip("蓄满时伤害/削韧倍率（1=不放大）")]
        public float MaxDamageMultiplier = 2.5f;

        [Header("瞄准")]
        [Tooltip("光标/发射的最大水平距离（米）")]
        public float MaxAimDistance = 14f;

        // ---- 运行时（技能为按持有者克隆的实例，存实例状态安全）----
        private float _charge;
        private Vector3 _aimPoint;
        private bool _fired;
        private CombatDemoFireballReticle _reticle;

        protected override void OnActivateAbility(GameplayEventData triggerData)
        {
            if (!CommitAbility()) { EndAbility(true); return; }

            _charge = 0f;
            _fired = false;
            _aimPoint = ASC.transform.position + ASC.transform.forward * (MaxAimDistance * 0.5f);

            var ctrl = ASC.GetComponent<CombatDemoController>();
            _reticle = ctrl != null ? ctrl.Reticle : ASC.GetComponentInChildren<CombatDemoFireballReticle>();
            if (_reticle != null) _reticle.Show();

            // 等松手（同一发射输入标签的 Canceled）→ 释放
            var release = AbilityTask_WaitInputPress.WaitInputPress(
                this, FireInputTag, null, null, InputTriggerEvent.Canceled, true);
            release.OnPress += _ => Release();
            release.Activate();

            EnableTick = true; // 逐帧更新光标 + 蓄力
        }

        public override void AbilityTick(float deltaTime)
        {
            _charge = Mathf.Min(MaxChargeTime, _charge + deltaTime);
            float f = ChargeFraction();

            _aimPoint = ComputeAimPoint();
            if (_reticle != null)
                _reticle.SetState(_aimPoint, Mathf.Lerp(MinBlastRadius, MaxBlastRadius, f));
        }

        private void Release()
        {
            if (_fired) { EndAbility(); return; }
            _fired = true;
            float f = ChargeFraction();

            if (FireballBullet != null)
            {
                var ctrl = ASC.GetComponent<CombatDemoController>();
                Transform muzzle = ctrl != null && ctrl.Muzzle != null ? ctrl.Muzzle : ASC.transform;

                // 克隆子弹 + 攻击定义，按蓄力缩放（不改共享资产）
                var bulletClone = Instantiate(FireballBullet);
                bulletClone.HitRadius = Mathf.Lerp(MinBlastRadius, MaxBlastRadius, f);
                AttackDefinition attackClone = null;
                if (FireballBullet.Attack != null)
                {
                    attackClone = Instantiate(FireballBullet.Attack);
                    float dmgMul = Mathf.Lerp(1f, MaxDamageMultiplier, f);
                    for (int i = 0; i < attackClone.SetByCallerMagnitudes.Count; i++)
                    {
                        var m = attackClone.SetByCallerMagnitudes[i];
                        m.Value *= dmgMul;
                        attackClone.SetByCallerMagnitudes[i] = m;
                    }
                    bulletClone.Attack = attackClone;
                }

                Vector3 dir = _aimPoint - muzzle.position;
                if (dir.sqrMagnitude < 0.0001f) dir = muzzle.forward;

                float stunDur = Mathf.Lerp(MinStunDuration, MaxStunDuration, f);
                var bullets = BulletLauncher.Fire(bulletClone, ASC.gameObject, ASC, muzzle.position, dir.normalized);

                foreach (var b in bullets)
                {
                    b.OnHit += (bi, targetASC, point) =>
                    {
                        if (targetASC != null && StunEffect != null)
                        {
                            var stun = Instantiate(StunEffect);
                            stun.Duration = stunDur;
                            targetASC.ApplyGameplayEffectToSelf(stun);
                            targetASC.ExecuteGameplayCue(CombatDemoTags.Cue_Stunned);
                        }
                    };
                    b.OnExpired += bi =>
                    {
                        if (bulletClone != null) Destroy(bulletClone);
                        if (attackClone != null) Destroy(attackClone);
                    };
                }
            }

            EndAbility();
        }

        protected override void OnEndAbility(bool wasCancelled)
        {
            EnableTick = false;
            if (_reticle != null) _reticle.Hide();
        }

        private float ChargeFraction() => MaxChargeTime > 0f ? Mathf.Clamp01(_charge / MaxChargeTime) : 1f;

        // 相机前向与角色所在水平面求交，得地面瞄准点；限制水平距离。纯几何、不依赖物理层。
        private Vector3 ComputeAimPoint()
        {
            var self = ASC.transform.position;
            var cam = Camera.main;
            if (cam == null)
            {
                Vector3 f = ASC.transform.forward; f.y = 0f;
                return self + (f.sqrMagnitude > 1e-4f ? f.normalized : Vector3.forward) * (MaxAimDistance * 0.5f);
            }

            Vector3 camPos = cam.transform.position;
            Vector3 camFwd = cam.transform.forward;
            Vector3 point;
            if (camFwd.y < -1e-4f)
            {
                float t = (self.y - camPos.y) / camFwd.y;
                point = camPos + camFwd * t;
            }
            else
            {
                Vector3 flat = camFwd; flat.y = 0f;
                point = self + (flat.sqrMagnitude > 1e-4f ? flat.normalized : Vector3.forward) * MaxAimDistance;
            }

            // 限制到玩家周围 MaxAimDistance 内
            Vector3 offset = point - self; offset.y = 0f;
            if (offset.magnitude > MaxAimDistance) point = self + offset.normalized * MaxAimDistance;
            point.y = self.y;
            return point;
        }
    }
}
