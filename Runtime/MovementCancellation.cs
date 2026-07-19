// Copyright (c) 2026 Likeon. Licensed under the MIT License.
// 移动取消窗口：攻击等动画带 root motion 位移时，若窗口期内玩家有移动意图，则取消动画 root motion、让移动系统接管
// （动作游戏常见的"位移可被移动打断"手感）。
//
// 本组件用 Animation Event 驱动 BeginWindow/EndWindow
// （同 MeleeAttackTrace 的判定窗口模式），窗口期切 Animator.applyRootMotion。
// 用 CharacterController 水平速度判断“在移动”；要更精准可重写 IsMoving。

using UnityEngine;

namespace Likeon.GAS
{
    [AddComponentMenu("Sigil/GAS/Movement Cancellation")]
    public class MovementCancellation : MonoBehaviour
    {
        [Tooltip("root motion 目标 Animator（留空在子物体查找）")]
        [SerializeField] private Animator animator;
        [Tooltip("移动判定来源（留空在父级查找）")]
        [SerializeField] private CharacterController characterController;
        [Tooltip("水平速度超过此值（米/秒）视为玩家在移动 → 取消动画 root motion")]
        [SerializeField] private float moveSpeedThreshold = 0.1f;
        [Tooltip("窗口最长持续秒数，超时自动关闭兜底：动画被受击/切技能打断致 EndWindow 事件不触发时，防止 _windowOpen 残留、root motion 被永久禁用。<=0 关闭超时。")]
        [SerializeField] private float maxWindowSeconds = 5f;

        private bool _windowOpen;
        private bool _rootMotionDisabled;
        private bool _savedApplyRootMotion;
        private float _windowElapsed;

        /// <summary>取消窗口是否打开。</summary>
        public bool IsWindowOpen => _windowOpen;
        /// <summary>当前是否已禁用 root motion。</summary>
        public bool IsRootMotionDisabled => _rootMotionDisabled;

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (characterController == null) characterController = GetComponentInParent<CharacterController>();
        }

        /// <summary>Animation Event 在取消窗口起点调用。</summary>
        public void BeginWindow()
        {
            _windowOpen = true;
            _rootMotionDisabled = false;
            _windowElapsed = 0f;
        }

        /// <summary>Animation Event 在取消窗口终点调用（恢复 root motion）。</summary>
        public void EndWindow()
        {
            _windowOpen = false;
            Restore();
        }

        private void Update()
        {
            if (!_windowOpen) return;
            if (maxWindowSeconds > 0f)
            {
                _windowElapsed += Time.deltaTime;
                if (_windowElapsed >= maxWindowSeconds) { EndWindow(); return; } // 超时兜底：动画被打断没等到 EndWindow
            }
            Tick(IsMoving());
        }

        // 组件禁用兜底：动画被打断/组件禁用致 EndWindow 不触发时，_windowOpen 会残留、若正处于禁 root motion
        // 则位移永久丢失。禁用时无条件恢复并关窗。
        private void OnDisable()
        {
            if (_windowOpen)
            {
                _windowOpen = false;
                Restore();
            }
        }

        /// <summary>窗口期状态机：moving → 禁 root motion，否则恢复。可被测试直接驱动（绕过 IsMoving）。</summary>
        public void Tick(bool moving)
        {
            if (!_windowOpen) return;
            if (moving && !_rootMotionDisabled) Disable();
            else if (!moving && _rootMotionDisabled) Restore();
        }

        /// <summary>是否有移动意图。默认按 CharacterController 水平速度；可重写做更精准判断。</summary>
        public virtual bool IsMoving()
        {
            if (characterController == null) return false;
            Vector3 v = characterController.velocity; v.y = 0f;
            return v.sqrMagnitude > moveSpeedThreshold * moveSpeedThreshold;
        }

        private void Disable()
        {
            if (animator == null) return;
            _savedApplyRootMotion = animator.applyRootMotion;
            animator.applyRootMotion = false;
            _rootMotionDisabled = true;
        }

        private void Restore()
        {
            if (_rootMotionDisabled && animator != null) animator.applyRootMotion = _savedApplyRootMotion;
            _rootMotionDisabled = false;
        }
    }
}
