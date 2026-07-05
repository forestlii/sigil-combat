// Copyright (c) 2026 Likeon. Licensed under the MIT License.
// 极简敌人 AI（威胁型）：追击玩家 → 进近战距离按冷却激活近战技能 → 被眩晕/破防时冻结 → 血量归零死亡。
// 移动复用 CharacterMovementSystemComponent（AI 只喂输入方向），攻击复用 GAS 技能（TryActivateAbilitiesByTag）。

using UnityEngine;

namespace Likeon.GAS.Sample.CombatDemo
{
    /// <summary>敌人 AI：追击 + 近战反击 + 状态感知（眩晕/破防冻结、死亡）。</summary>
    [AddComponentMenu("Sigil/GAS/Samples/Combat Demo Enemy AI")]
    public class CombatDemoEnemyAI : MonoBehaviour
    {
        [Header("引用（builder 接好；留空自动查找）")]
        public AbilitySystemComponent ASC;
        public CharacterMovementSystemComponent Mover;
        [Tooltip("追击目标（玩家）；留空运行时找敌对 ASC")]
        public Transform Target;

        [Header("行为参数")]
        [Tooltip("进入该距离内停下并攻击（米）")]
        public float AttackRange = 2.2f;
        [Tooltip("超出该距离放弃追击（米，0=不放弃）")]
        public float LeashRange = 0f;
        [Tooltip("两次攻击最小间隔（秒）")]
        public float AttackCooldown = 2f;
        [Tooltip("死亡后多久移除（秒）")]
        public float DespawnDelay = 2f;

        private GameplayAttribute _healthAttr;
        private float _cooldownTimer;
        private bool _dead;

        private void Awake()
        {
            if (ASC == null) ASC = GetComponent<AbilitySystemComponent>();
            if (Mover == null) Mover = GetComponent<CharacterMovementSystemComponent>();
            _healthAttr = GameplayAttribute.From<AS_Health>("Health");
        }

        private void OnEnable()
        {
            if (ASC != null) ASC.OnAttributeChanged += HandleAttributeChanged;
        }

        private void OnDisable()
        {
            if (ASC != null) ASC.OnAttributeChanged -= HandleAttributeChanged;
        }

        private void HandleAttributeChanged(AttributeChangeData data)
        {
            if (_dead) return;
            if (data.Attribute.Equals(_healthAttr) && data.NewValue <= 0f) Die();
        }

        private void Update()
        {
            if (_dead || ASC == null || Mover == null) return;

            // 眩晕 / 破防 → 冻结（不移动、不攻击）
            if (ASC.HasMatchingGameplayTag(CombatDemoTags.State_Stunned) ||
                ASC.HasMatchingGameplayTag(CombatDemoTags.State_Staggered))
            {
                Mover.SetInputDirection(Vector3.zero);
                return;
            }

            if (Target == null) Target = FindHostileTarget();
            if (Target == null) { Mover.SetInputDirection(Vector3.zero); return; }

            if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;

            Vector3 to = Target.position - transform.position; to.y = 0f;
            float dist = to.magnitude;

            if (LeashRange > 0f && dist > LeashRange)
            {
                Mover.SetInputDirection(Vector3.zero);
                return;
            }

            if (dist > AttackRange)
            {
                // 追击：朝玩家喂输入方向（移动组件按当前速度移动 + 朝速度方向转身）
                Mover.SetInputDirection(to.sqrMagnitude > 1e-4f ? to.normalized : Vector3.zero);
            }
            else
            {
                // 到位：停下并按冷却发起近战
                Mover.SetInputDirection(Vector3.zero);
                if (_cooldownTimer <= 0f)
                {
                    if (ASC.TryActivateAbilitiesByTag(CombatDemoTags.Ability_EnemyMelee))
                        _cooldownTimer = AttackCooldown;
                }
            }
        }

        private Transform FindHostileTarget()
        {
            var self = GetComponent<CombatTeamAgent>();
            if (self == null) return null;
            foreach (var agent in FindObjectsOfType<CombatTeamAgent>())
            {
                if (agent == self) continue;
                if (self.GetAttitudeTowards(agent.gameObject) == ETeamAttitude.Hostile)
                    return agent.transform;
            }
            return null;
        }

        private void Die()
        {
            if (_dead) return;
            _dead = true;
            if (Mover != null)
            {
                Mover.SetInputDirection(Vector3.zero);
                Mover.enabled = false; // 死后不再移动（CC 下面也会被禁，别再驱动它）
            }

            // 死亡生命周期（若挂了 CombatCore）
            var combatCore = GetComponent<CombatCore>();
            if (combatCore != null) combatCore.StartDeath();

            // 不再是命中/锁定候选（注意 CharacterController 也是 Collider，会一并禁用）
            foreach (var col in GetComponentsInChildren<Collider>()) col.enabled = false;

            if (DespawnDelay > 0f) Destroy(gameObject, DespawnDelay);
        }
    }
}
