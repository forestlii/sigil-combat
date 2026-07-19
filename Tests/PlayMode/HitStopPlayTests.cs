// PlayMode 测试：顿帧（hit-stop）连续触发后 Animator 速度能正确恢复 —— P0-2 回归。
// 修复前：ApplyHitStop 用 string 版 StopCoroutine 停方法引用启动的协程（静默失败），
// 连续两次顿帧会把彼此已降速的 animator.speed 当成恢复基准 → 动画永久卡在慢速；
// 且组件在顿帧中途被禁用时协程被杀、speed 停在慢速无人复位。
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Likeon.GAS;

namespace Likeon.GAS.PlayTests
{
    public class HitStopPlayTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();

        [TearDown]
        public void Cleanup()
        {
            foreach (var go in _spawned) if (go != null) Object.Destroy(go);
            _spawned.Clear();
        }

        // 先加 Animator，供 CombatSystemComponent.Awake 自动查找。
        private CombatSystemComponent NewCombatWithAnimator(out Animator animator)
        {
            var go = new GameObject("Fighter"); _spawned.Add(go);
            animator = go.AddComponent<Animator>();
            return go.AddComponent<CombatSystemComponent>();
        }

        [UnityTest]
        public IEnumerator ConsecutiveHitStops_RestoreAnimatorSpeed()
        {
            var combat = NewCombatWithAnimator(out var animator);
            Assert.AreEqual(1f, animator.speed, 0.001f, "初始速度应为 1");

            // 连续两次顿帧（同帧内，第二次必须能停掉第一次的协程）
            combat.ApplyHitStop(0.1f, 0.3f);
            Assert.Less(animator.speed, 1f, "顿帧期间应被降速");
            combat.ApplyHitStop(0.1f, 0.3f);

            // 等两次顿帧都结束
            yield return new WaitForSeconds(0.25f);

            Assert.AreEqual(1f, animator.speed, 0.001f,
                "连续顿帧结束后速度应恢复为 1（修复前会永久卡在 <1）");
        }

        [UnityTest]
        public IEnumerator HitStop_RestoresSpeedOnDisable()
        {
            var combat = NewCombatWithAnimator(out var animator);
            combat.ApplyHitStop(1.0f, 0.3f); // 长顿帧，禁用时仍在进行中
            Assert.Less(animator.speed, 1f, "顿帧期间应被降速");

            combat.enabled = false; // 触发 OnDisable：协程被杀，speed 需无条件复位
            yield return null;

            Assert.AreEqual(1f, animator.speed, 0.001f, "组件禁用后速度应复位为 1");
        }
    }
}
