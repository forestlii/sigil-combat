// Copyright (c) 2026 Likeon. Licensed under the MIT License.
// 战斗接口的开箱即用实现：把 ICombatInterface 的查询/驱动桥接到同物体上的
// 移动 / 输入 / 战斗 / 锁定 / 武器组件。宿主不想逐个学习这些组件 API 时，挂这一个组件即可。
// combat 与 movement 是两个平级、互不依赖的包；本组件同时用到两侧，属于"集成/示范"代码，
// 故住在示例里（不进任何框架包）。搬到自己的工程后可作为 ICombatInterface 的模板实现参考。

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Likeon.GAS
{
    /// <summary>死亡阶段（配合 <see cref="ICombatInterface"/> 的死亡生命周期）。</summary>
    public enum ECombatDeathState
    {
        NotDead,
        DeathStarted,
        DeathFinished
    }

    /// <summary>
    /// 战斗核心组件：<see cref="ICombatInterface"/> 的模板实现。
    /// 移动三态（RotationMode / MovementSet / MovementState）转发给 <see cref="MovementSystemComponent"/>，
    /// 能力动作查询转发给 <see cref="CombatSystemComponent"/>，目标 / 武器 / 防御输入按组件在场自动接线。
    /// 不了解 movement 组件 API 的宿主可以只面向本组件（或 ICombatInterface）驱动移动与战斗查询。
    /// </summary>
    [AddComponentMenu("Sigil/GAS/Combat Core")]
    public class CombatCore : MonoBehaviour, ICombatInterface
    {
        [Header("依赖（留空自动在本物体/子物体查找）")]
        [SerializeField] private MovementSystemComponent mover;
        [SerializeField] private InputSystemComponent inputSystem;
        [SerializeField] private CombatSystemComponent combatSystem;
        [SerializeField] private TargetingSystemComponent targeting;
        [SerializeField] private WeaponComponent weapon;

        [Header("防御输入")]
        [Tooltip("防御键的 InputTag（IsHoldingBlockInput 读它的实时值）；留空恒 false")]
        [SerializeField] private GameplayTag blockInputTag;

        // 懒解析，不依赖组件 Awake 顺序
        private MovementSystemComponent Mover => mover != null ? mover : (mover = GetComponent<MovementSystemComponent>());
        private InputSystemComponent Input => inputSystem != null ? inputSystem : (inputSystem = GetComponent<InputSystemComponent>());
        private CombatSystemComponent Combat => combatSystem != null ? combatSystem : (combatSystem = GetComponent<CombatSystemComponent>());
        private TargetingSystemComponent Targeting => targeting != null ? targeting : (targeting = GetComponent<TargetingSystemComponent>());
        private WeaponComponent Weapon => weapon != null ? weapon : (weapon = GetComponentInChildren<WeaponComponent>(true));

        // ===================== 目标 =====================
        public GameObject GetCombatTargetActor() => Targeting != null ? Targeting.TargetedActor : null;

        public Transform GetCombatTargetObject()
        {
            var actor = GetCombatTargetActor();
            return actor != null ? actor.transform : null;
        }

        // ===================== 能力动作 / 武器 =====================
        public bool QueryAbilityActions(GameplayTagContainer abilityTags, GameplayTagContainer sourceTags,
            GameplayTagContainer targetTags, List<AbilityAction> outActions)
        {
            if (Combat != null) return Combat.QueryAbilityActions(abilityTags, sourceTags, targetTags, outActions);
            outActions?.Clear();
            return false;
        }

        public IWeapon GetCurrentWeapon() => Weapon;

        // ===================== 输入 =====================
        public bool IsHoldingBlockInput()
        {
            if (Input == null || !blockInputTag.IsValid) return false;
            return Input.GetInputActionValueOfInputTag(blockInputTag).Value.sqrMagnitude > 0.25f;
        }

        /// <summary>移动输入方向（世界空间）。经输入系统的 InputProcessor_Move 喂给移动组件后从这里读出。</summary>
        public Vector3 GetMovementInputDirection() => Mover != null ? Mover.GetInputDirection() : Vector3.zero;

        // ===================== 移动模式（转发给 MovementSystemComponent）=====================
        public void SetRotationMode(GameplayTag newRotationMode) => Mover?.SetDesiredRotationMode(newRotationMode);
        public GameplayTag GetRotationMode() => Mover != null ? Mover.GetRotationMode() : GameplayTag.None;

        public void SetMovementSet(GameplayTag newMovementSet) => Mover?.SetMovementSet(newMovementSet);
        public GameplayTag GetMovementSet() => Mover != null ? Mover.GetMovementSet() : GameplayTag.None;

        public void SetMovementState(GameplayTag newMovementState) => Mover?.SetDesiredMovement(newMovementState);
        public GameplayTag GetMovementState() => Mover != null ? Mover.GetMovementState() : GameplayTag.None;
        public GameplayTag GetDesiredMovementState() => Mover != null ? Mover.GetDesiredMovementState() : GameplayTag.None;

        // ===================== 死亡生命周期 =====================
        private ECombatDeathState _deathState = ECombatDeathState.NotDead;

        /// <summary>当前死亡阶段。</summary>
        public ECombatDeathState DeathState => _deathState;

        /// <summary>StartDeath 时触发（布娃娃/死亡动画的挂点）。</summary>
        public event Action OnDeathStarted;

        /// <summary>FinishDeath 时触发（回收/重生的挂点）。</summary>
        public event Action OnDeathFinished;

        public void StartDeath()
        {
            if (_deathState != ECombatDeathState.NotDead) return;
            _deathState = ECombatDeathState.DeathStarted;
            OnDeathStarted?.Invoke();
        }

        public void FinishDeath()
        {
            if (_deathState == ECombatDeathState.DeathFinished) return;
            _deathState = ECombatDeathState.DeathFinished;
            OnDeathFinished?.Invoke();
        }

        public bool IsDead() => _deathState != ECombatDeathState.NotDead;
    }
}
