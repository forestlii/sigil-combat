// Copyright (c) 2026 Likeon. Licensed under the MIT License.
// 便利工具：给选中的 GameObject 一键补齐"战斗角色"常用组件。
// ⚠️ 刻意**不**合成一个 God 组件——保持每个组件单一职责、可按角色需要增删（纯远程敌人不必挂近战判定等），
//    也不偏离"照真实 GAS 框架移植"的模块边界。这里只是"把常用一套一次性加上"，组件仍各自独立。
// 加的一套（幂等，缺才加）：AbilitySystemComponent（基座）+ CombatTeamAgent（阵营）+
//   CombatSystemComponent（战斗中枢）+ PoiseComponent（削韧）。
// 角色专属的锁定 / 近战判定 / 武器（TargetingSystemComponent / MeleeAttackTrace / WeaponComponent）
//   按需另加——它们要 socket / 武器等逐角色配置，不宜盲目自动加。

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Likeon.GAS.Combat.Editor
{
    /// <summary>一键给角色补齐常用战斗组件（各组件独立，不合并）。</summary>
    public static class CombatSetupMenu
    {
        private const string GoMenu = "GameObject/Sigil/Combat Character Setup";
        private const string TopMenu = "Sigil/GAS/Setup/Add Combat Character Components";

        [MenuItem(GoMenu, false, 10)]
        [MenuItem(TopMenu)]
        public static void AddCombatSetup()
        {
            var targets = Selection.gameObjects;
            if (targets == null || targets.Length == 0)
            {
                Debug.LogWarning("[Sigil] 先在 Hierarchy 选中一个或多个 GameObject，再执行「Combat Character Setup」。");
                return;
            }
            foreach (var go in targets) AddTo(go);
        }

        // GameObject 菜单项仅在选中对象时可用
        [MenuItem(GoMenu, true)]
        private static bool ValidateGo() => Selection.gameObjects.Length > 0;

        private static void AddTo(GameObject go)
        {
            var added = new List<string>();
            // 顺序：先 ASC（PoiseComponent 的 RequireComponent 依赖它），再其余
            Ensure<AbilitySystemComponent>(go, added);
            Ensure<CombatTeamAgent>(go, added);
            Ensure<CombatSystemComponent>(go, added);
            Ensure<PoiseComponent>(go, added);

            if (added.Count > 0)
                Debug.Log($"[Sigil] «{go.name}» 已补齐战斗组件：{string.Join(", ", added)}。" +
                          " 锁定(TargetingSystemComponent) / 近战判定(MeleeAttackTrace) / 武器(WeaponComponent) 按角色需要另加。", go);
            else
                Debug.Log($"[Sigil] «{go.name}» 已具备常用战斗组件，无需补充。", go);
        }

        private static void Ensure<T>(GameObject go, List<string> added) where T : Component
        {
            if (go.GetComponent<T>() != null) return;
            Undo.AddComponent<T>(go); // 支持撤销
            added.Add(typeof(T).Name);
        }
    }
}
