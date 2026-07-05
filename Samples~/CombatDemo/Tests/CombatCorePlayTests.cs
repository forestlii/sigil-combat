// PlayMode 测试：CombatCore（ICombatInterface 模板实现）+ GA_M_Sprint + 切换处理器。
// 验证：移动三态转发 / 输入方向 / 缺组件时安全默认 / 死亡生命周期 / 冲刺技能激活-取消 / 切换处理器开关。
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Likeon.GAS;

namespace Likeon.GAS.PlayTests
{
    public class CombatCorePlayTests
    {
        private static GameObject NewActor(out MovementSystemComponent mover, out CombatCore core, bool withAsc = false, bool withInput = false)
        {
            var go = new GameObject("CombatCoreActor");
            if (withAsc) go.AddComponent<AbilitySystemComponent>();
            mover = go.AddComponent<MovementSystemComponent>();
            if (withInput) go.AddComponent<InputSystemComponent>();
            core = go.AddComponent<CombatCore>();
            return go;
        }

        private static GA_M_Sprint NewSprintTemplate(GameplayTag abilityTag)
        {
            var ga = ScriptableObject.CreateInstance<GA_M_Sprint>();
            ga.AbilityTags.Add(abilityTag);
            return ga;
        }

        [UnityTest]
        public IEnumerator MovementCalls_DelegateToMover()
        {
            var go = NewActor(out var mover, out var core);
            yield return null;

            // MovementState：Set 走 SetDesiredMovement，Get 读实际/期望状态
            core.SetMovementState(MovementTags.MovementState_Walk);
            Assert.AreEqual(MovementTags.MovementState_Walk, mover.GetDesiredMovementState(), "SetMovementState 应落到 mover 的期望状态");
            Assert.AreEqual(MovementTags.MovementState_Walk, core.GetMovementState(), "GetMovementState 应读回 mover 实际状态");
            Assert.AreEqual(MovementTags.MovementState_Walk, core.GetDesiredMovementState());

            // RotationMode
            core.SetRotationMode(MovementTags.RotationMode_VelocityDirection);
            Assert.AreEqual(MovementTags.RotationMode_VelocityDirection, core.GetRotationMode(), "SetRotationMode 应落到 mover");

            // MovementSet
            var tArmed = GameplayTag.RequestTag("Movement.Set.Armed");
            core.SetMovementSet(tArmed);
            Assert.AreEqual(tArmed, core.GetMovementSet(), "SetMovementSet 应落到 mover");

            // 输入方向：InputProcessor_Move 喂给 mover 后经 CombatCore 读出
            mover.SetInputDirection(new Vector3(0f, 0f, 1f));
            Assert.AreEqual(new Vector3(0f, 0f, 1f), core.GetMovementInputDirection(), "GetMovementInputDirection 应读 mover 的输入方向");

            Object.Destroy(go);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MissingOptionalComponents_SafeDefaults()
        {
            var go = new GameObject("BareCombatCore");
            var core = go.AddComponent<CombatCore>(); // 无 mover/input/combat/targeting/weapon
            yield return null;

            Assert.AreEqual(Vector3.zero, core.GetMovementInputDirection(), "无 mover 输入方向应为零");
            Assert.IsFalse(core.GetMovementState().IsValid, "无 mover 移动状态应为无效标签");
            Assert.IsNull(core.GetCombatTargetActor(), "无锁定组件目标应为 null");
            Assert.IsNull(core.GetCurrentWeapon(), "无武器组件应为 null");
            Assert.IsFalse(core.IsHoldingBlockInput(), "无输入组件防御输入应为 false");

            var actions = new System.Collections.Generic.List<AbilityAction>();
            Assert.IsFalse(core.QueryAbilityActions(new GameplayTagContainer(), new GameplayTagContainer(), new GameplayTagContainer(), actions),
                "无战斗组件 QueryAbilityActions 应返回 false");
            Assert.AreEqual(0, actions.Count, "查询失败应清空输出列表");

            Object.Destroy(go);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DeathLifecycle_TransitionsAndEventsOnce()
        {
            var go = new GameObject("DeathActor");
            var core = go.AddComponent<CombatCore>();
            yield return null;

            int started = 0, finished = 0;
            core.OnDeathStarted += () => started++;
            core.OnDeathFinished += () => finished++;

            Assert.IsFalse(core.IsDead(), "初始不应为死亡");

            core.StartDeath();
            Assert.IsTrue(core.IsDead(), "StartDeath 后 IsDead 应为 true");
            Assert.AreEqual(ECombatDeathState.DeathStarted, core.DeathState);
            core.StartDeath(); // 重复调用不重入
            Assert.AreEqual(1, started, "OnDeathStarted 应只触发一次");

            core.FinishDeath();
            Assert.AreEqual(ECombatDeathState.DeathFinished, core.DeathState);
            core.FinishDeath();
            Assert.AreEqual(1, finished, "OnDeathFinished 应只触发一次");

            Object.Destroy(go);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SprintAbility_ActivateAndCancel_SwitchesMovementState()
        {
            var go = NewActor(out var mover, out _, withAsc: true);
            var asc = go.GetComponent<AbilitySystemComponent>();
            yield return null;

            var tAbility = GameplayTag.RequestTag("Ability.Movement.Sprint");
            var template = NewSprintTemplate(tAbility);
            var handle = asc.GiveAbility(template);

            Assert.AreEqual(MovementTags.MovementState_Jog, mover.GetMovementState(), "初始 Jog");

            Assert.IsTrue(asc.TryActivateAbilitiesByTag(tAbility), "冲刺技能应能激活");
            Assert.AreEqual(MovementTags.MovementState_Sprint, mover.GetMovementState(), "激活应切入 Sprint");

            var spec = asc.FindAbilitySpec(handle);
            spec.Ability.CancelAbility();
            Assert.AreEqual(MovementTags.MovementState_Jog, mover.GetMovementState(), "取消应切回 Jog");

            Object.Destroy(template);
            Object.Destroy(go);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ToggleProcessor_TogglesSprintOnAndOff()
        {
            var go = NewActor(out var mover, out _, withAsc: true, withInput: true);
            var asc = go.GetComponent<AbilitySystemComponent>();
            var ic = go.GetComponent<InputSystemComponent>();
            yield return null;

            var tAbility = GameplayTag.RequestTag("Ability.Movement.Sprint");
            var tInput = GameplayTag.RequestTag("Input.Movement.Sprint");
            var template = NewSprintTemplate(tAbility);
            var handle = asc.GiveAbility(template);

            var processor = new InputProcessor_ToggleAbilityByTag { AbilityTag = tAbility };
            processor.InputTags.AddTag(tInput);

            // 第一次按下：激活
            processor.HandleInput(ic, InputActionData.Empty, tInput, InputTriggerEvent.Started);
            Assert.IsTrue(asc.FindAbilitySpec(handle).IsActive, "第一次按下应激活冲刺");
            Assert.AreEqual(MovementTags.MovementState_Sprint, mover.GetMovementState());

            // 第二次按下：取消
            processor.HandleInput(ic, InputActionData.Empty, tInput, InputTriggerEvent.Started);
            Assert.IsFalse(asc.FindAbilitySpec(handle).IsActive, "第二次按下应取消冲刺");
            Assert.AreEqual(MovementTags.MovementState_Jog, mover.GetMovementState());

            Object.Destroy(template);
            Object.Destroy(go);
            yield return null;
        }
    }
}
