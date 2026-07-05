// Copyright (c) 2026 Likeon. Licensed under the MIT License.
// 玩家场景粘合：集中暴露技能要用的场景引用（枪口/锁定/光标/移动组件/当前武器），
// 并把"切武器 / 切锁定"这类经输入系统广播的 GameplayEvent 落地。
//   切武器 = 卸下当前武器（移除其 Weapon.* 标签）+ 装上另一把（注入标签）→ 攻击键据武器标签多态。
//   切锁定 = 驱动 TargetingSystemComponent 搜索/解除锁定。

using UnityEngine;

namespace Likeon.GAS.Sample.CombatDemo
{
    /// <summary>玩家控制粘合层（技能/HUD 从这里取场景引用；处理切武器与锁定事件）。</summary>
    [AddComponentMenu("Sigil/GAS/Samples/Combat Demo Controller")]
    public class CombatDemoController : MonoBehaviour
    {
        [Header("引用（builder 接好；留空则在本物体/子物体查找）")]
        public AbilitySystemComponent ASC;
        public TargetingSystemComponent Targeting;
        public CharacterMovementSystemComponent Mover;
        public CombatDemoFireballReticle Reticle;
        [Tooltip("远程/火球发射起点")]
        public Transform Muzzle;
        [Tooltip("近战武器（Weapon.Melee 标签）")]
        public WeaponComponent MeleeWeapon;
        [Tooltip("远程武器（Weapon.Ranged 标签）")]
        public WeaponComponent RangedWeapon;

        /// <summary>当前装备的武器。</summary>
        public WeaponComponent CurrentWeapon { get; private set; }

        private bool _subscribed;

        private void Awake()
        {
            if (ASC == null) ASC = GetComponent<AbilitySystemComponent>();
            if (Targeting == null) Targeting = GetComponent<TargetingSystemComponent>();
            if (Mover == null) Mover = GetComponent<CharacterMovementSystemComponent>();
            if (Reticle == null) Reticle = GetComponentInChildren<CombatDemoFireballReticle>(true);
        }

        private void OnEnable() => Subscribe();
        private void OnDisable() => Unsubscribe();

        private void Start()
        {
            // 初始装备近战武器（注入 Weapon.Melee → 攻击键先走近战多态分支）
            if (CurrentWeapon == null && MeleeWeapon != null) Equip(MeleeWeapon);
        }

        private void Subscribe()
        {
            if (_subscribed || ASC == null) return;
            ASC.OnGameplayEvent += HandleGameplayEvent;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || ASC == null) return;
            ASC.OnGameplayEvent -= HandleGameplayEvent;
            _subscribed = false;
        }

        private void HandleGameplayEvent(GameplayTag eventTag, GameplayEventData data)
        {
            if (eventTag.MatchesTagExact(CombatDemoTags.Event_SwitchWeapon)) ToggleWeapon();
            else if (eventTag.MatchesTagExact(CombatDemoTags.Event_ToggleLock)) ToggleLock();
        }

        /// <summary>在近战/远程武器间切换。</summary>
        public void ToggleWeapon()
        {
            var next = CurrentWeapon == MeleeWeapon ? RangedWeapon : MeleeWeapon;
            if (next == null) return;
            Equip(next);
        }

        private void Equip(WeaponComponent weapon)
        {
            if (weapon == null) return;
            if (CurrentWeapon != null) CurrentWeapon.Unequip();
            CurrentWeapon = weapon;
            weapon.Equip(gameObject);
        }

        private void ToggleLock()
        {
            if (Targeting == null) return;
            if (Targeting.IsLockedOn) Targeting.ToggleLock();
            else Targeting.SearchForActorToTarget();
        }
    }
}
