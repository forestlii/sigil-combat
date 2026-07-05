// Copyright (c) 2026 Likeon. Licensed under the MIT License.
// Combat 示例总编排：锁鼠标 + 自解释 HUD。
// HUD 全靠订阅/读取框架公开 API（属性/标签/锁定/武器），演示"GAS 广播数据、宿主画 UI"边界。
// 战斗流程（近战/冲刺攻击/远程/闪现/火球/切武器/锁定/敌人 AI）都在各自技能与组件里，这里不驱动。

using UnityEngine;
using UnityEngine.InputSystem;

namespace Likeon.GAS.Sample.CombatDemo
{
    /// <summary>战斗 demo 编排：鼠标锁定 + HUD。</summary>
    [AddComponentMenu("Sigil/GAS/Samples/Combat Demo")]
    public class CombatDemo : MonoBehaviour
    {
        [Tooltip("玩家 ASC（builder 接好；HUD 读属性/标签）")]
        public AbilitySystemComponent PlayerASC;
        [Tooltip("玩家控制粘合层（HUD 读当前武器/锁定目标）")]
        public CombatDemoController Controller;

        private GameplayAttribute _health, _maxHealth, _stamina, _maxStamina, _poise, _maxPoise;

        private void Awake()
        {
            if (PlayerASC == null) PlayerASC = GetComponent<AbilitySystemComponent>();
            if (Controller == null) Controller = GetComponent<CombatDemoController>();
            _health = GameplayAttribute.From<AS_Health>("Health");
            _maxHealth = GameplayAttribute.From<AS_Health>("MaxHealth");
            _stamina = GameplayAttribute.From<AS_Stamina>("Stamina");
            _maxStamina = GameplayAttribute.From<AS_Stamina>("MaxStamina");
            _poise = GameplayAttribute.From<AS_Poise>("Poise");
            _maxPoise = GameplayAttribute.From<AS_Poise>("MaxPoise");
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = 13, richText = true };

            GUILayout.BeginArea(new Rect(12, 12, 520, 210), GUI.skin.box);
            GUILayout.Label("<b>Sigil Combat Demo</b>  —  movement 运动/输入 + GAS 战斗流程", style);
            GUILayout.Label("<b>WASD</b> 移动 · <b>鼠标</b> 视角 · <b>Shift</b> 冲刺 · <b>Esc</b> 释放鼠标", style);
            GUILayout.Label("<b>左键</b> 攻击（冲刺+近战=冲刺攻击 / 近战=普攻 / 远程=射击） · <b>Tab</b> 切近战/远程武器", style);
            GUILayout.Label("<b>右键长按</b> 火球（松手释放，蓄力越久越强，命中眩晕） · <b>Q</b> 闪现 · <b>E</b> 锁定", style);

            if (PlayerASC != null)
            {
                GUILayout.Space(4);
                GUILayout.Label($"生命 <b>{PlayerASC.GetAttributeValue(_health):0}</b>/{PlayerASC.GetAttributeValue(_maxHealth):0}" +
                                $"　体力 <b>{PlayerASC.GetAttributeValue(_stamina):0}</b>/{PlayerASC.GetAttributeValue(_maxStamina):0}" +
                                $"　韧性 <b>{PlayerASC.GetAttributeValue(_poise):0.0}</b>/{PlayerASC.GetAttributeValue(_maxPoise):0.0}", style);

                string weapon = Controller != null && Controller.CurrentWeapon != null
                    ? (Controller.CurrentWeapon == Controller.RangedWeapon ? "远程" : "近战") : "-";
                string target = Controller != null && Controller.Targeting != null && Controller.Targeting.IsLockedOn
                    ? Controller.Targeting.TargetedActor?.name : "无";
                bool stunned = PlayerASC.HasMatchingGameplayTag(CombatDemoTags.State_Stunned);
                bool staggered = PlayerASC.HasMatchingGameplayTag(CombatDemoTags.State_Staggered);
                GUILayout.Label($"武器 <b>{weapon}</b>　锁定 <b>{target}</b>" +
                                (stunned ? "　<color=#f55><b>眩晕中</b></color>" : "") +
                                (staggered ? "　<color=#fa0><b>破防硬直</b></color>" : ""), style);
            }
            GUILayout.EndArea();
        }
    }
}
