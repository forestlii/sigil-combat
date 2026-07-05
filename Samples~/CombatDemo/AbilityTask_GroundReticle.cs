// Copyright (c) 2026 Likeon. Licensed under the MIT License.
// 地面 AoE 光标任务（demo 表现层）：激活时建一个贴地扁圆盘，每帧按 sampler 给的 (落点, 半径) 更新；
// 任务结束时销毁圆盘。因为技能 EndAbility 会自动 ExternalCancel 所有未结束任务 → 调 OnDestroy，
// 圆盘生命周期铁定跟着技能走、零泄漏，技能里不用再手写 Hide/清理。
//
// ⚠️ 这是"优先用框架原语（AbilityTask）而非新写持久组件"的示范：
//    圆盘 GameObject 的 建/更新/销毁 全交给 AbilityTask 的生命周期。
//    瞄准/半径怎么算是 demo 专属，由调用方经 sampler 传入，故本任务留在示例里。

using System;
using System.Collections;
using UnityEngine;

namespace Likeon.GAS.Sample.CombatDemo
{
    /// <summary>蓄力时显示地面 AoE 光标的 AbilityTask。每帧调 sampler 取 (世界落点, AoE 半径)。</summary>
    public class AbilityTask_GroundReticle : AbilityTask
    {
        private Color _color;
        private Func<(Vector3 pos, float radius)> _sample;
        private Transform _disk;

        /// <summary>创建并绑定到技能。订阅后（本任务无回调）调 <see cref="AbilityTask.Activate"/> 启动。</summary>
        public static AbilityTask_GroundReticle Show(GameplayAbility ability, Color color, Func<(Vector3, float)> sample)
        {
            var task = new AbilityTask_GroundReticle { _color = color, _sample = sample };
            task.InitTask(ability); // protected internal：示例程序集经 protected 通道绑定
            return task;
        }

        protected override void OnActivate() => RunCoroutine(Loop());

        private IEnumerator Loop()
        {
            EnsureDisk();
            while (true)
            {
                if (_disk != null && _sample != null)
                {
                    var (pos, radius) = _sample();
                    _disk.position = pos + Vector3.up * 0.02f;
                    // 圆柱默认直径=1（半径 0.5），故 X/Z 缩放 = radius*2
                    _disk.localScale = new Vector3(radius * 2f, 0.02f, radius * 2f);
                }
                yield return null;
            }
        }

        private void EnsureDisk()
        {
            if (_disk != null) return;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "FireballReticleDisk";
            var col = go.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.Destroy(col); // 光标不参与物理
            go.transform.localScale = new Vector3(1f, 0.02f, 1f);

            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                // 运行时材质：本对象每次激活现建、结束即销毁，不入 prefab，故 new Material 安全
                var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
                if (shader != null) mr.sharedMaterial = new Material(shader) { color = _color };
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }
            _disk = go.transform;
        }

        protected override void OnDestroy(bool abilityEnded)
        {
            if (_disk != null) UnityEngine.Object.Destroy(_disk.gameObject);
            _disk = null;
        }
    }
}
