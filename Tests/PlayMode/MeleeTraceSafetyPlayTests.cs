// PlayMode 测试：MeleeAttackTrace 禁用兜底 —— B1 / P2-24 回归（框架侧）。
// 修复前：组件禁用不清 _active，_active 残留会让 LateUpdate 继续球扫；且技能被取消时若 trace 未关会持续扫伤。
// 示例技能已加 OnEndAbility 兜底关判定；框架侧再加 OnDisable 作双保险，此处覆盖框架侧。
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Likeon.GAS;

namespace Likeon.GAS.PlayTests
{
    public class MeleeTraceSafetyPlayTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();
        private readonly List<Object> _assets = new List<Object>();

        [TearDown]
        public void Cleanup()
        {
            foreach (var go in _spawned) if (go != null) Object.Destroy(go);
            foreach (var a in _assets) if (a != null) Object.Destroy(a);
            _spawned.Clear(); _assets.Clear();
        }

        [UnityTest]
        public IEnumerator OnDisable_StopsTracing()
        {
            var go = new GameObject("Attacker"); _spawned.Add(go);
            var trace = go.AddComponent<MeleeAttackTrace>();
            var atk = ScriptableObject.CreateInstance<AttackDefinition>(); _assets.Add(atk);
            trace.Entries.Add(new MeleeAttackTrace.AttackTraceEntry
            {
                Attack = atk,
                Trace = new CollisionTraceDefinition
                {
                    SocketTransforms = new List<Transform> { go.transform },
                    TraceRadius = 0.5f
                }
            });
            yield return null;

            trace.BeginAttackTrace(0);
            Assert.IsTrue(trace.IsTracing, "开判定后应在扫");

            trace.enabled = false; // 组件禁用兜底
            Assert.IsFalse(trace.IsTracing, "禁用组件应停止判定（修复前 _active 残留会持续扫伤）");
        }
    }
}
