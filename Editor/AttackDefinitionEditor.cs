// Copyright (c) 2026 Likeon. Licensed under the MIT License.
// 编辑器：AttackDefinition 的增强 Inspector（摘要 + 配置校验提示 + 默认绘制）。

using UnityEditor;
using UnityEngine;

namespace Likeon.GAS.Combat.Editor
{
    [CustomEditor(typeof(AttackDefinition))]
    public class AttackDefinitionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var atk = (AttackDefinition)target;
            EditorGUILayout.HelpBox("Attack Definition — 命中目标后施加的效果与表现。", MessageType.None);

            bool hasContainerEffect = atk.TargetEffectContainer.TargetGameplayEffects != null
                                      && atk.TargetEffectContainer.TargetGameplayEffects.Count > 0;
            if (atk.TargetEffect == null && !hasContainerEffect)
                EditorGUILayout.HelpBox("既没有 TargetEffect 也没有效果容器：命中将不产生任何效果。", MessageType.Warning);

            DrawDefaultInspector();
        }
    }
}
