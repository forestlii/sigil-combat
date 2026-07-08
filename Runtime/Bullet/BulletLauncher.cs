// Copyright (c) 2026 Likeon. Licensed under the MIT License.
// 按定义批量生成子弹（含散射）。
// 单机取舍：这里用静态工具直接生成 GameObject+BulletInstance；
// 子弹失效时自销毁（对象池可作为后续优化，不影响 API）。

using System.Collections.Generic;
using UnityEngine;

namespace Likeon.GAS
{
    /// <summary>子弹发射器：按 <see cref="BulletDefinition"/> 生成 N 发带散射的子弹。</summary>
    public static class BulletLauncher
    {
        // 静态空闲子弹池：失效回池、Fire 时复用，避免频繁 new GameObject/Destroy 的 GC 分配。
        // 子弹 GameObject 是通用载体（Launch 时才配 def），故池不分 def。
        private static readonly Stack<BulletInstance> _pool = new Stack<BulletInstance>();

        // 从池取一发复用（跳过被场景卸载销毁的），没有则新建。
        private static BulletInstance RentOrCreate(BulletDefinition def, Vector3 origin)
        {
            while (_pool.Count > 0)
            {
                var pooled = _pool.Pop();
                if (pooled == null) continue; // 被外部/场景卸载销毁的跳过
                var pgo = pooled.gameObject;
                pgo.transform.position = origin;
                pgo.SetActive(true);
                pooled.Pooled = true;
                return pooled;
            }
            var go = new GameObject(def != null ? $"Bullet_{def.name}" : "Bullet");
            go.transform.position = origin;
            var bullet = go.AddComponent<BulletInstance>();
            bullet.Pooled = true;
            return bullet;
        }

        /// <summary>回收一发子弹进池（由 <see cref="BulletInstance"/> 失效时调用）。</summary>
        internal static void Recycle(BulletInstance bullet)
        {
            if (bullet == null) return;
            bullet.ResetForPool();
            if (bullet.gameObject != null) bullet.gameObject.SetActive(false);
            _pool.Push(bullet);
        }

        /// <summary>清空子弹池并销毁池中实例（测试 / 场景切换用）。</summary>
        public static void ClearPool()
        {
            while (_pool.Count > 0)
            {
                var b = _pool.Pop();
                if (b != null && b.gameObject != null)
                {
                    if (Application.isPlaying) Object.Destroy(b.gameObject);
                    else Object.DestroyImmediate(b.gameObject);
                }
            }
        }

        /// <summary>当前空闲池中的子弹数（调试/测试）。</summary>
        public static int PooledCount => _pool.Count;

        /// <summary>
        /// 以 origin 为起点、baseRotation 为基准朝向，按定义的数量/散射角发射子弹。返回生成的实例。
        ///（散射：每发在基准朝向上叠加 yaw 偏角 + 统一仰角）。
        /// </summary>
        public static List<BulletInstance> Fire(BulletDefinition def, GameObject owner,
            AbilitySystemComponent sourceASC, Vector3 origin, Quaternion baseRotation)
        {
            var result = new List<BulletInstance>();
            if (def == null) return result;

            int count = Mathf.Max(1, def.BulletCount);
            for (int i = 0; i < count; i++)
            {
                // 以中心对称分布：i 相对中心的偏移 * 间隔 + 基础偏角
                float yaw = def.LaunchAngle + (i - (count - 1) * 0.5f) * def.LaunchAngleInterval;
                Quaternion rot = baseRotation * Quaternion.Euler(-def.LaunchElevationAngle, yaw, 0f);
                Vector3 dir = rot * Vector3.forward;

                var bullet = RentOrCreate(def, origin); // 池化：复用或新建
                bullet.Launch(def, owner, sourceASC, origin, dir);
                result.Add(bullet);
            }
            return result;
        }

        /// <summary>便捷重载：用一个方向向量作为基准朝向。</summary>
        public static List<BulletInstance> Fire(BulletDefinition def, GameObject owner,
            AbilitySystemComponent sourceASC, Vector3 origin, Vector3 direction)
            => Fire(def, owner, sourceASC, origin,
                direction.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(direction.normalized) : Quaternion.identity);
    }
}
