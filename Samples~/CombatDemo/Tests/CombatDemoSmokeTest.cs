// PlayMode 冒烟测试：验证烘出的 CombatDemo prefab 接线 + 战斗流程端到端（近战掉血/切武器/火球发弹/闪现位移）。
// 需先运行 Sigil ▸ GAS ▸ Samples ▸ Build Combat Demo Scene 生成 Resources prefab。
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Likeon.GAS;

namespace Likeon.GAS.Sample.CombatDemo.Tests
{
    public class CombatDemoSmokeTest
    {
        // PlayMode 测试共享同一场景；每条测试后清掉战斗对象（玩家/敌人/子弹/地面），
        // 避免残留敌人/子弹污染下一条（否则 flash 被残留物挡、玩家开局已被残留敌人打过）。
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var b in Object.FindObjectsOfType<BulletInstance>()) Object.Destroy(b.gameObject);
            foreach (var a in Object.FindObjectsOfType<AbilitySystemComponent>()) Object.Destroy(a.gameObject);
            foreach (var m in Object.FindObjectsOfType<MeshRenderer>()) if (m != null) Object.Destroy(m.gameObject);
            yield return null;
            yield return null;
        }

        private static GameObject LoadPlayer() => SpawnFromResources("CombatDemoPlayer");
        private static GameObject LoadEnemy() => SpawnFromResources("CombatDemoEnemy");

        private static GameObject SpawnFromResources(string name)
        {
            var prefab = Resources.Load<GameObject>(name);
            Assert.IsNotNull(prefab, $"应能从 Resources 加载 {name} prefab（先运行 Sigil ▸ GAS ▸ Samples ▸ Build Combat Demo Scene）");
            return Object.Instantiate(prefab);
        }

        // 测试场景没有 builder 烘的地面 → 角色受重力下坠会干扰判定；补一块地面。
        private static GameObject SpawnGround()
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Plane);
            g.transform.localScale = new Vector3(5f, 1f, 5f);
            Physics.SyncTransforms();
            return g;
        }

        private static float Health(AbilitySystemComponent asc)
            => asc.GetAttributeValue(GameplayAttribute.From<AS_Health>("Health"));

        [UnityTest]
        public IEnumerator A_PlayerPrefab_Wired()
        {
            var player = LoadPlayer();
            yield return null; // Awake：initialLoadouts 授予

            Assert.IsNotNull(player.GetComponent<CharacterController>(), "应含 CharacterController");
            var asc = player.GetComponent<AbilitySystemComponent>();
            Assert.IsNotNull(asc, "应含 ASC");
            Assert.IsNotNull(player.GetComponent<CharacterMovementSystemComponent>(), "应含移动组件");
            Assert.IsNotNull(player.GetComponent<InputSystemComponent>(), "应含输入组件");
            Assert.IsNotNull(player.GetComponent<CombatCore>(), "应含 CombatCore");
            Assert.IsNotNull(player.GetComponent<CombatDemoController>(), "应含 CombatDemoController");
            Assert.IsNotNull(player.GetComponent<CombatDemoFireballReticle>(), "应含火球光标");

            // loadout 授予了属性集 + 技能
            Assert.Greater(Health(asc), 0f, "应授予 AS_Health");
            Assert.Greater(asc.GetAttributeValue(GameplayAttribute.From<AS_Stamina>("Stamina")), 0f, "应授予 AS_Stamina");
            Assert.Greater(asc.GetGrantedAbilities().Count, 0, "应授予技能");

            Object.Destroy(player);
            yield return null;
        }

        [UnityTest]
        public IEnumerator B_EnemyPrefab_Wired()
        {
            var enemy = LoadEnemy();
            yield return null;

            var asc = enemy.GetComponent<AbilitySystemComponent>();
            Assert.IsNotNull(asc, "应含 ASC");
            Assert.IsNotNull(enemy.GetComponent<CombatDemoEnemyAI>(), "应含敌人 AI");
            Assert.IsNotNull(enemy.GetComponent<PoiseComponent>(), "应含削韧组件");
            Assert.Greater(Health(asc), 0f, "应授予 AS_Health");
            Assert.Greater(asc.GetAttributeValue(GameplayAttribute.From<AS_Poise>("Poise")), 0f, "应授予 AS_Poise");

            Object.Destroy(enemy);
            yield return null;
        }

        [UnityTest]
        public IEnumerator C_MeleeAttack_ReducesEnemyHealth()
        {
            var ground = SpawnGround();
            var player = LoadPlayer();
            player.transform.position = Vector3.zero;
            var enemy = LoadEnemy();
            enemy.transform.position = new Vector3(0f, 0f, 1.4f); // 玩家正前方，近战判定半径内
            enemy.GetComponent<CombatDemoEnemyAI>().enabled = false; // 静止靶：AI 追击/反击会移动敌人+击退玩家，扰乱判定；AI 正确性靠可玩场景人肉验
            yield return null; // Awake 授予 + 控制器 Start 装备近战武器
            yield return null;
            Physics.SyncTransforms();

            var enemyASC = enemy.GetComponent<AbilitySystemComponent>();
            float before = Health(enemyASC);

            // 走完整分发链：攻击键 → 输入控制集 FirstOnly（装近战→GA_Melee）→ 判定命中
            var input = player.GetComponent<InputSystemComponent>();
            input.ReceiveInput(CombatDemoTags.Input_Attack, InputTriggerEvent.Started, InputActionData.Empty);
            yield return new WaitForSeconds(0.6f); // 等判定窗口 LateUpdate 命中

            Assert.Less(Health(enemyASC), before, "近战攻击应让正前方敌人掉血");

            Object.Destroy(ground);
            Object.Destroy(player);
            Object.Destroy(enemy);
            yield return null;
        }

        [UnityTest]
        public IEnumerator D_SwitchWeapon_TogglesWeaponTag()
        {
            var player = LoadPlayer();
            yield return null;
            yield return null; // 控制器 Start 装备近战

            var asc = player.GetComponent<AbilitySystemComponent>();
            var input = player.GetComponent<InputSystemComponent>();
            Assert.IsTrue(asc.HasMatchingGameplayTag(CombatDemoTags.Weapon_Melee), "初始应装备近战武器");

            input.ReceiveInput(CombatDemoTags.Input_SwitchWeapon, InputTriggerEvent.Started, InputActionData.Empty);
            yield return null;

            Assert.IsTrue(asc.HasMatchingGameplayTag(CombatDemoTags.Weapon_Ranged), "切换后应装备远程武器");
            Assert.IsFalse(asc.HasMatchingGameplayTag(CombatDemoTags.Weapon_Melee), "切换后近战标签应移除");

            Object.Destroy(player);
            yield return null;
        }

        [UnityTest]
        public IEnumerator E_Fireball_ChargeRelease_SpawnsBullet()
        {
            var player = LoadPlayer();
            yield return null;
            yield return null;

            var input = player.GetComponent<InputSystemComponent>();
            // 按下开始蓄力（激活火球技能）
            input.ReceiveInput(CombatDemoTags.Input_Fireball, InputTriggerEvent.Started, InputActionData.Empty);
            yield return null; // 一帧蓄力（AbilityTick 更新瞄准）
            // 松手释放
            input.ReceiveInput(CombatDemoTags.Input_Fireball, InputTriggerEvent.Canceled, InputActionData.Empty);
            yield return null;

            Assert.IsNotNull(Object.FindObjectOfType<BulletInstance>(), "松手应发射火球（生成 BulletInstance）");

            Object.Destroy(player);
            yield return null;
        }

        // 敌人 AI 进入攻击距离后应主动激活近战技能（"敌人会反击"）。
        // 只断言 AI 的攻击决策（激活技能）——命中几何见 C，追击位移见可玩场景人肉验：
        // batchmode 无渲染循环时 CharacterController 逐帧积分被饿死，位移远小于速度（vel 实测满速朝玩家），
        // 是计时假象非逻辑问题，故不在测试里依赖"移动一段距离"。
        [UnityTest]
        public IEnumerator G_EnemyAI_ActivatesMeleeInRange()
        {
            var ground = SpawnGround();
            var player = LoadPlayer();
            player.transform.position = Vector3.zero;
            var enemy = LoadEnemy();
            enemy.transform.position = new Vector3(0f, 0f, 1.8f); // 已在攻击范围内（AttackRange 2.2）

            var easc = enemy.GetComponent<AbilitySystemComponent>();
            bool attacked = false;
            easc.OnAbilityActivated += a => { if (a.GetAbilityTags().HasTag(CombatDemoTags.Ability_EnemyMelee)) attacked = true; };

            yield return null;
            yield return new WaitForSeconds(1f); // AI 每帧检查：在范围内即按冷却激活近战

            Assert.IsTrue(attacked, "敌人 AI 进入攻击距离应激活近战技能反击");

            Object.Destroy(ground);
            Object.Destroy(player);
            Object.Destroy(enemy);
            yield return null;
        }

        [UnityTest]
        public IEnumerator F_Flash_TeleportsPlayer()
        {
            var ground = SpawnGround();
            var player = LoadPlayer();
            player.transform.position = Vector3.zero;
            yield return null;
            yield return null;

            Vector3 before = player.transform.position;
            var input = player.GetComponent<InputSystemComponent>();
            input.ReceiveInput(CombatDemoTags.Input_Flash, InputTriggerEvent.Started, InputActionData.Empty);
            yield return null;

            float moved = Vector3.Distance(new Vector3(before.x, 0f, before.z),
                                           new Vector3(player.transform.position.x, 0f, player.transform.position.z));
            Assert.Greater(moved, 2f, "闪现应让玩家水平位移一段距离");

            Object.Destroy(ground);
            Object.Destroy(player);
            yield return null;
        }

        // 回归：死亡路径会禁用全部 Collider（含 CharacterController 本身也是 Collider），
        // 移动组件不得再对禁用的 CC 调 Move——修复前每帧报
        // "CharacterController.Move called on inactive controller"（Test Framework 见 Error 日志即失败，等几帧即断言）。
        [UnityTest]
        public IEnumerator H_DisabledCharacterController_DoesNotSpamErrors()
        {
            var enemy = LoadEnemy();
            yield return null;

            foreach (var col in enemy.GetComponentsInChildren<Collider>()) col.enabled = false;
            yield return null;
            yield return null;
            yield return null;

            Object.Destroy(enemy);
            yield return null;
        }
    }
}
