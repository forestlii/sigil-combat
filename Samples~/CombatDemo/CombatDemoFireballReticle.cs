// Copyright (c) 2026 Likeon. Licensed under the MIT License.
// 火球地面光标：一个贴地的扁圆盘，蓄力时显示落点 AoE 范围。纯 demo 表现层（框架无 World Reticle）。

using UnityEngine;

namespace Likeon.GAS.Sample.CombatDemo
{
    /// <summary>火球蓄力时的地面 AoE 指示器。GA_Fireball 调 Show/SetState/Hide 驱动。</summary>
    [AddComponentMenu("Sigil/GAS/Samples/Combat Demo Fireball Reticle")]
    public class CombatDemoFireballReticle : MonoBehaviour
    {
        [Tooltip("光标颜色")]
        public Color Color = new Color(1f, 0.45f, 0.1f, 0.55f);

        private Transform _disk;

        private void Awake() => EnsureDisk();

        private void EnsureDisk()
        {
            if (_disk != null) return;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "FireballReticleDisk";
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col); // 光标不参与物理
            _disk = go.transform;
            _disk.SetParent(transform, false);
            _disk.localScale = new Vector3(1f, 0.02f, 1f); // 扁盘

            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
                if (shader != null) mr.sharedMaterial = new Material(shader) { color = Color };
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }
            go.SetActive(false);
        }

        public void Show()
        {
            EnsureDisk();
            if (_disk != null) _disk.gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (_disk != null) _disk.gameObject.SetActive(false);
        }

        /// <summary>设置光标世界位置与 AoE 半径。</summary>
        public void SetState(Vector3 worldPos, float radius)
        {
            EnsureDisk();
            if (_disk == null) return;
            _disk.position = worldPos + Vector3.up * 0.02f;
            // 圆柱默认直径=1（半径 0.5），故 X/Z 缩放 = radius*2
            _disk.localScale = new Vector3(radius * 2f, 0.02f, radius * 2f);
        }
    }
}
