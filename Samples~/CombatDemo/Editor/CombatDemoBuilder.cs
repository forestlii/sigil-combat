// Copyright (c) 2026 Likeon. Licensed under the MIT License.
// Combat Demo 生成器：程序化产出全部数据资产（效果/攻击/子弹/技能/输入/装载/移动定义）+
// 玩家 & 敌人 prefab（Resources/）+ 一个可直接 Play 的 CombatDemo.unity 场景（玩家 + 3 敌人 + 第三人称相机 + 地面）。
// 复用 movement 的运动/输入链路（Move/Look/Sprint）+ core 的战斗流程（近战判定/子弹/伤害 GE/削韧/锁定）。
// 菜单：Sigil ▸ GAS ▸ Samples ▸ Build Combat Demo Scene（重跑幂等）。

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using Likeon.GAS.Sample.CombatDemo;

namespace Likeon.GAS.Sample.CombatDemo.Editor
{
    public static class CombatDemoBuilder
    {
        private const string Menu = "Sigil/GAS/Samples/Build Combat Demo Scene";

        [MenuItem(Menu)]
        public static void Build()
        {
            // 1) 定位示例目录（以 .inputactions 为锚）
            var guids = AssetDatabase.FindAssets("CombatDemoControls t:InputActionAsset");
            if (guids.Length == 0) { Debug.LogError("[CombatDemo] 找不到 CombatDemoControls.inputactions"); return; }
            string actionsPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            string dir = System.IO.Path.GetDirectoryName(actionsPath).Replace('\\', '/');
            _buildDir = dir; // 供 SetColor 把材质存成资产（见其注释）

            var refs = AssetDatabase.LoadAllAssetsAtPath(actionsPath).OfType<InputActionReference>().ToList();
            InputActionReference Ref(string n) => refs.FirstOrDefault(r => r.action != null && r.action.name == n);
            var moveRef = Ref("Move"); var lookRef = Ref("Look"); var sprintRef = Ref("Sprint");
            var attackRef = Ref("Attack"); var switchRef = Ref("SwitchWeapon"); var fireballRef = Ref("Fireball");
            var flashRef = Ref("Flash"); var lockRef = Ref("LockOn");
            if (new[] { moveRef, lookRef, sprintRef, attackRef, switchRef, fireballRef, flashRef, lockRef }.Any(r => r == null))
            { Debug.LogError("[CombatDemo] .inputactions 缺少必要动作引用"); return; }

            // 2) 效果（GE）——伤害 / 眩晕 / 消耗 / 冷却 / 体力回复
            var damage = MakeGE("GE_CombatDemo_Damage", ge =>
            {
                ge.DurationType = EGameplayEffectDurationType.Instant;
                AddMod(ge, GameplayAttribute.From<AS_Health>("IncomingDamage"), GameplayModifierMagnitude.SetByCaller(CombatDemoTags.Data_Damage));
                AddMod(ge, GameplayAttribute.From<AS_Poise>("IncomingPoiseDamage"), GameplayModifierMagnitude.SetByCaller(CombatDemoTags.Data_PoiseDamage));
                ge.GameplayCues.Add(CombatDemoTags.Cue_Hit);
            });
            var stun = MakeGE("GE_CombatDemo_Stun", ge =>
            {
                ge.DurationType = EGameplayEffectDurationType.HasDuration;
                ge.Duration = 1.5f; // 运行时按蓄力克隆覆盖
                ge.GrantedTags.Add(CombatDemoTags.State_Stunned);
                ge.GameplayCues.Add(CombatDemoTags.Cue_Stunned);
            });
            var staminaRegen = MakeGE("GE_CombatDemo_StaminaRegen", ge =>
            {
                ge.DurationType = EGameplayEffectDurationType.Infinite;
                ge.Period = 1f;
                AddMod(ge, GameplayAttribute.From<AS_Stamina>("Stamina"), GameplayModifierMagnitude.ScalableFloat(10f));
            });
            GameplayEffect Cost(string n, float amount) => MakeGE(n, ge =>
            {
                ge.DurationType = EGameplayEffectDurationType.Instant;
                AddMod(ge, GameplayAttribute.From<AS_Stamina>("Stamina"), GameplayModifierMagnitude.ScalableFloat(-amount));
            });
            GameplayEffect Cooldown(string n, float seconds, string cdTag) => MakeGE(n, ge =>
            {
                ge.DurationType = EGameplayEffectDurationType.HasDuration;
                ge.Duration = seconds;
                ge.GrantedTags.Add(GameplayTag.RequestTag(cdTag));
            });
            var flashCost = Cost("GE_CombatDemo_FlashCost", 20f);
            var fireballCost = Cost("GE_CombatDemo_FireballCost", 25f);
            var meleeCd = Cooldown("GE_CombatDemo_MeleeCd", 0.5f, "Cooldown.Combat.Melee");
            var dashCd = Cooldown("GE_CombatDemo_DashCd", 1.2f, "Cooldown.Combat.Dash");
            var rangedCd = Cooldown("GE_CombatDemo_RangedCd", 0.4f, "Cooldown.Combat.Ranged");
            var flashCd = Cooldown("GE_CombatDemo_FlashCd", 3f, "Cooldown.Combat.Flash");
            var fireballCd = Cooldown("GE_CombatDemo_FireballCd", 4f, "Cooldown.Combat.Fireball");
            foreach (var ge in new[] { damage, stun, staminaRegen, flashCost, fireballCost, meleeCd, dashCd, rangedCd, flashCd, fireballCd })
                CreateAsset(ge, $"{dir}/{ge.name}.asset");

            // 3) 攻击定义（AttackDefinition）
            var atkLight = MakeAttack("Attack_CombatDemo_MeleeLight", damage, 18f, 1.5f);
            var atkHeavy = MakeAttack("Attack_CombatDemo_DashSlash", damage, 34f, 3f);
            var atkRanged = MakeAttack("Attack_CombatDemo_Ranged", damage, 12f, 1f);
            var atkFireball = MakeAttack("Attack_CombatDemo_Fireball", damage, 22f, 2f);
            var atkEnemy = MakeAttack("Attack_CombatDemo_Enemy", damage, 8f, 1f);
            foreach (var a in new[] { atkLight, atkHeavy, atkRanged, atkFireball, atkEnemy })
                CreateAsset(a, $"{dir}/{a.name}.asset");

            // 4) 子弹（BulletDefinition）
            var bulletRanged = MakeBullet("Bullet_CombatDemo_Ranged", atkRanged, speed: 26f, radius: 0.35f, penetrateMap: false);
            var bulletFireball = MakeBullet("Bullet_CombatDemo_Fireball", atkFireball, speed: 16f, radius: 0.7f, penetrateMap: true);
            CreateAsset(bulletRanged, $"{dir}/{bulletRanged.name}.asset");
            CreateAsset(bulletFireball, $"{dir}/{bulletFireball.name}.asset");

            // 5) 技能（GameplayAbility 子类）
            var gaMelee = MakeAbility<GA_MeleeAttack>("GA_CombatDemo_Melee", CombatDemoTags.Ability_Melee, a =>
            { a.TraceEntryIndex = 0; a.CooldownEffect = meleeCd; });
            var gaDash = MakeAbility<GA_DashAttack>("GA_CombatDemo_DashAttack", CombatDemoTags.Ability_DashAttack, a =>
            { a.TraceEntryIndex = 1; a.LungeDistance = 2.5f; a.CooldownEffect = dashCd; });
            var gaRanged = MakeAbility<GA_RangedAttack>("GA_CombatDemo_Ranged", CombatDemoTags.Ability_Ranged, a =>
            { a.Bullet = bulletRanged; a.CooldownEffect = rangedCd; });
            var gaFlash = MakeAbility<GA_Flash>("GA_CombatDemo_Flash", CombatDemoTags.Ability_Flash, a =>
            { a.FlashDistance = 5f; a.CostEffect = flashCost; a.CooldownEffect = flashCd; });
            var gaFireball = MakeAbility<GA_Fireball>("GA_CombatDemo_Fireball", CombatDemoTags.Ability_Fireball, a =>
            {
                a.FireballBullet = bulletFireball; a.StunEffect = stun; a.FireInputTag = CombatDemoTags.Input_Fireball;
                a.CostEffect = fireballCost; a.CooldownEffect = fireballCd; a.EnableTick = true;
            });
            var gaSprint = MakeAbility<GA_M_Sprint>("GA_CombatDemo_Sprint", CombatDemoTags.Ability_Sprint, _ => { });
            var gaEnemyMelee = MakeAbility<GA_MeleeAttack>("GA_CombatDemo_EnemyMelee", CombatDemoTags.Ability_EnemyMelee, a =>
            { a.TraceEntryIndex = 0; a.TraceWindow = 0.3f; });
            // 玩家技能被眩晕/破防挡住（CC 对玩家也生效）
            foreach (var a in new GameplayAbility[] { gaMelee, gaDash, gaRanged, gaFlash, gaFireball, gaEnemyMelee })
            {
                a.ActivationBlockedTags.Add(CombatDemoTags.State_Stunned);
                a.ActivationBlockedTags.Add(CombatDemoTags.State_Staggered);
            }
            foreach (var a in new ScriptableObject[] { gaMelee, gaDash, gaRanged, gaFlash, gaFireball, gaSprint, gaEnemyMelee })
                CreateAsset(a, $"{dir}/{a.name}.asset");

            // 6) 输入配置 + 控制集（FirstOnly：攻击键据冲刺状态/武器标签多态）
            var config = ScriptableObject.CreateInstance<InputConfig>();
            void Map(GameplayTag t, InputActionReference r, bool value) =>
                config.InputActionMappings.Add(new InputActionMapping { InputTag = t, Action = r, ValueBinding = value });
            Map(CombatDemoTags.Input_Move, moveRef, true);
            Map(CombatDemoTags.Input_Look, lookRef, true);
            Map(CombatDemoTags.Input_Sprint, sprintRef, false);
            Map(CombatDemoTags.Input_Attack, attackRef, false);
            Map(CombatDemoTags.Input_SwitchWeapon, switchRef, false);
            Map(CombatDemoTags.Input_Fireball, fireballRef, false);
            Map(CombatDemoTags.Input_Flash, flashRef, false);
            Map(CombatDemoTags.Input_LockOn, lockRef, false);
            CreateAsset(config, $"{dir}/CombatDemo_InputConfig.asset");

            var setup = ScriptableObject.CreateInstance<InputControlSetup>();
            setup.ExecutionType = EInputProcessorExecutionType.FirstOnly;
            // 移动 / 视角（复用 movement 处理器）
            setup.AddProcessor(Proc(new InputProcessor_Move { CameraRelative = true }, CombatDemoTags.Input_Move,
                InputTriggerEvent.Started, InputTriggerEvent.Triggered, InputTriggerEvent.Canceled));
            setup.AddProcessor(Proc(new InputProcessor_Look { Sensitivity = 1f }, CombatDemoTags.Input_Look,
                InputTriggerEvent.Started, InputTriggerEvent.Triggered));
            // 冲刺开关
            setup.AddProcessor(Proc(new InputProcessor_ToggleAbilityByTag { AbilityTag = CombatDemoTags.Ability_Sprint },
                CombatDemoTags.Input_Sprint, InputTriggerEvent.Started));
            // 攻击多态（顺序=优先级；FirstOnly 取首个 StateQuery 通过者）
            setup.AddProcessor(Proc(new InputProcessor_ActivateAbilityByTag
            {
                StateQuery = GameplayTagQuery.MakeQuery_MatchAllTags(CombatDemoTags.State_Sprint, CombatDemoTags.Weapon_Melee),
                AbilityTag = CombatDemoTags.Ability_DashAttack
            }, CombatDemoTags.Input_Attack, InputTriggerEvent.Started));
            setup.AddProcessor(Proc(new InputProcessor_ActivateAbilityByTag
            {
                StateQuery = GameplayTagQuery.MakeQuery_MatchAllTags(CombatDemoTags.Weapon_Melee),
                AbilityTag = CombatDemoTags.Ability_Melee
            }, CombatDemoTags.Input_Attack, InputTriggerEvent.Started));
            setup.AddProcessor(Proc(new InputProcessor_ActivateAbilityByTag
            {
                StateQuery = GameplayTagQuery.MakeQuery_MatchAllTags(CombatDemoTags.Weapon_Ranged),
                AbilityTag = CombatDemoTags.Ability_Ranged
            }, CombatDemoTags.Input_Attack, InputTriggerEvent.Started));
            // 火球（激活；蓄力/松手在技能内处理）
            setup.AddProcessor(Proc(new InputProcessor_ActivateAbilityByTag { AbilityTag = CombatDemoTags.Ability_Fireball },
                CombatDemoTags.Input_Fireball, InputTriggerEvent.Started));
            // 闪现
            setup.AddProcessor(Proc(new InputProcessor_ActivateAbilityByTag { AbilityTag = CombatDemoTags.Ability_Flash },
                CombatDemoTags.Input_Flash, InputTriggerEvent.Started));
            // 切武器 / 锁定（广播事件，控制器落地）
            setup.AddProcessor(Proc(new InputProcessor_SendGameplayEvent { EventTag = CombatDemoTags.Event_SwitchWeapon },
                CombatDemoTags.Input_SwitchWeapon, InputTriggerEvent.Started));
            setup.AddProcessor(Proc(new InputProcessor_SendGameplayEvent { EventTag = CombatDemoTags.Event_ToggleLock },
                CombatDemoTags.Input_LockOn, InputTriggerEvent.Started));
            CreateAsset(setup, $"{dir}/CombatDemo_InputControlSetup.asset");

            // 7) 移动定义
            var def = ScriptableObject.CreateInstance<MovementDefinition>();
            var moveSet = new MovementSetSetting { MovementSet = GameplayTag.None };
            moveSet.States.Add(MovementStateSetting.Default(MovementTags.MovementState_Walk, 1.5f));
            moveSet.States.Add(MovementStateSetting.Default(MovementTags.MovementState_Jog, 3.75f));
            moveSet.States.Add(MovementStateSetting.Default(MovementTags.MovementState_Sprint, 6.5f));
            def.MovementSets.Add(moveSet);
            CreateAsset(def, $"{dir}/CombatDemo_MovementDef.asset");

            // 8) 装载
            var playerLoadout = ScriptableObject.CreateInstance<AbilityLoadout>();
            playerLoadout.GrantedAttributeSets.Add(new AS_Health());
            playerLoadout.GrantedAttributeSets.Add(new AS_Stamina());
            playerLoadout.GrantedAttributeSets.Add(new AS_Poise()); // 玩家也有韧性，可被破防
            playerLoadout.GrantedEffects.Add(staminaRegen);
            foreach (var a in new GameplayAbility[] { gaMelee, gaDash, gaRanged, gaFlash, gaFireball, gaSprint })
                playerLoadout.GrantedAbilities.Add(new AbilityLoadout.GrantedAbility { Ability = a, Level = 1 });
            CreateAsset(playerLoadout, $"{dir}/CombatDemo_PlayerLoadout.asset");

            var enemyLoadout = ScriptableObject.CreateInstance<AbilityLoadout>();
            enemyLoadout.GrantedAttributeSets.Add(new AS_Health());
            enemyLoadout.GrantedAttributeSets.Add(new AS_Poise());
            enemyLoadout.GrantedAbilities.Add(new AbilityLoadout.GrantedAbility { Ability = gaEnemyMelee, Level = 1 });
            CreateAsset(enemyLoadout, $"{dir}/CombatDemo_EnemyLoadout.asset");

            // 9) prefab（Resources/）
            string resDir = dir + "/Resources";
            if (!AssetDatabase.IsValidFolder(resDir)) AssetDatabase.CreateFolder(dir, "Resources");

            var playerObj = BuildPlayer(config, setup, def, playerLoadout, atkLight, atkHeavy);
            var playerPrefab = PrefabUtility.SaveAsPrefabAsset(playerObj, resDir + "/CombatDemoPlayer.prefab");
            Object.DestroyImmediate(playerObj);

            var enemyObj = BuildEnemy(def, enemyLoadout, atkEnemy);
            var enemyPrefab = PrefabUtility.SaveAsPrefabAsset(enemyObj, resDir + "/CombatDemoEnemy.prefab");
            Object.DestroyImmediate(enemyObj);

            // 10) 场景
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(6f, 1f, 6f);

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.position = new Vector3(0f, 0.1f, 0f);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            var camSys = camGo.AddComponent<CameraSystemComponent>();
            camSys.Configure(cam, player.transform);
            camGo.transform.position = new Vector3(0f, 3f, -5f);
            WireField(player.GetComponent<CharacterMovementSystemComponent>(), "viewReference", camGo.transform);

            Vector3[] enemyPos = { new Vector3(4f, 0.1f, 4f), new Vector3(-4f, 0.1f, 4f), new Vector3(0f, 0.1f, 8f) };
            foreach (var pos in enemyPos)
            {
                var enemy = (GameObject)PrefabUtility.InstantiatePrefab(enemyPrefab);
                enemy.transform.position = pos;
                var ai = enemy.GetComponent<CombatDemoEnemyAI>();
                if (ai != null) ai.Target = player.transform; // 跨实例引用，场景级接
            }

            string scenePath = dir + "/CombatDemo.unity";
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[CombatDemo] 已生成 prefab + 场景 + 资产：{scenePath}");
        }

        // ===================== 玩家 / 敌人结构 =====================

        private static GameObject BuildPlayer(InputConfig config, InputControlSetup setup, MovementDefinition def,
            AbilityLoadout loadout, AttackDefinition lightAtk, AttackDefinition heavyAtk)
        {
            var player = new GameObject("Player");
            var cc = player.AddComponent<CharacterController>();
            cc.height = 2f; cc.radius = 0.4f; cc.center = new Vector3(0f, 1f, 0f);

            var asc = player.AddComponent<AbilitySystemComponent>();
            var mover = player.AddComponent<CharacterMovementSystemComponent>();
            var ic = player.AddComponent<InputSystemComponent>();
            player.AddComponent<CombatCore>();
            player.AddComponent<CombatTeamAgent>().SetTeamId(0);
            player.AddComponent<CombatSystemComponent>();
            player.AddComponent<PoiseComponent>(); // 玩家也可被削韧破防（攻防对称）
            var targeting = player.AddComponent<TargetingSystemComponent>();
            var melee = player.AddComponent<MeleeAttackTrace>();

            // 可视胶囊（无碰撞，玩家用 CharacterController 作碰撞）
            var vis = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            vis.name = "Visual";
            Object.DestroyImmediate(vis.GetComponent<Collider>());
            vis.transform.SetParent(player.transform, false);
            vis.transform.localPosition = new Vector3(0f, 1f, 0f);
            SetColor(vis, new Color(0.25f, 0.5f, 0.9f), "Mat_CombatDemo_Player");

            // 近战判定 socket（角色前方）
            var socket = new GameObject("WeaponSocket").transform;
            socket.SetParent(player.transform, false);
            socket.localPosition = new Vector3(0f, 1f, 1.2f);
            melee.Entries.Add(MakeTraceEntry(lightAtk, socket, 1.0f)); // index 0 普攻
            melee.Entries.Add(MakeTraceEntry(heavyAtk, socket, 1.2f)); // index 1 冲刺攻击

            // 枪口（远程/火球起点）
            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(player.transform, false);
            muzzle.localPosition = new Vector3(0f, 1.4f, 0.6f);

            // 火球光标
            var reticle = player.AddComponent<CombatDemoFireballReticle>();

            // 两把武器（子物体，注入 Weapon.* 标签供攻击多态）
            var meleeWeapon = MakeWeapon(player.transform, "MeleeWeapon", CombatDemoTags.Weapon_Melee);
            var rangedWeapon = MakeWeapon(player.transform, "RangedWeapon", CombatDemoTags.Weapon_Ranged);

            // 接线
            WireField(asc, "initialLoadouts", new List<Object> { loadout });
            WireField(mover, "movementDefinitions", new List<Object> { def });
            WireInputSystem(ic, config, setup);

            var ctrl = player.AddComponent<CombatDemoController>();
            ctrl.ASC = asc; ctrl.Targeting = targeting; ctrl.Mover = mover; ctrl.Reticle = reticle;
            ctrl.Muzzle = muzzle; ctrl.MeleeWeapon = meleeWeapon; ctrl.RangedWeapon = rangedWeapon;

            var demo = player.AddComponent<CombatDemo>();
            demo.PlayerASC = asc; demo.Controller = ctrl;
            return player;
        }

        private static GameObject BuildEnemy(MovementDefinition def, AbilityLoadout loadout, AttackDefinition enemyAtk)
        {
            var enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemy.name = "Enemy";
            SetColor(enemy, new Color(0.85f, 0.3f, 0.25f), "Mat_CombatDemo_Enemy");
            // 删掉图元的 CapsuleCollider：否则它与 CharacterController 重叠，CC 会撞自己的胶囊卡住移动。
            // 命中/锁定改由 CharacterController（本身也是 Collider）承担。
            var primCol = enemy.GetComponent<Collider>();
            if (primCol != null) Object.DestroyImmediate(primCol);

            var asc = enemy.AddComponent<AbilitySystemComponent>();
            var mover = enemy.AddComponent<CharacterMovementSystemComponent>(); // 自动带 CharacterController
            enemy.AddComponent<CombatCore>();
            enemy.AddComponent<CombatTeamAgent>().SetTeamId(1);
            enemy.AddComponent<CombatSystemComponent>();
            enemy.AddComponent<PoiseComponent>();
            var melee = enemy.AddComponent<MeleeAttackTrace>();

            var socket = new GameObject("WeaponSocket").transform;
            socket.SetParent(enemy.transform, false);
            socket.localPosition = new Vector3(0f, 1f, 1.0f);
            melee.Entries.Add(MakeTraceEntry(enemyAtk, socket, 1.0f));

            WireField(asc, "initialLoadouts", new List<Object> { loadout });
            WireField(mover, "movementDefinitions", new List<Object> { def });
            mover.SetDesiredRotationMode(MovementTags.RotationMode_VelocityDirection); // 追击时朝移动方向

            var ai = enemy.AddComponent<CombatDemoEnemyAI>();
            ai.ASC = asc; ai.Mover = mover;
            return enemy;
        }

        // ===================== 构造小工具 =====================

        private static GameplayEffect MakeGE(string name, System.Action<GameplayEffect> cfg)
        {
            var ge = ScriptableObject.CreateInstance<GameplayEffect>(); ge.name = name; cfg(ge); return ge;
        }

        private static void AddMod(GameplayEffect ge, GameplayAttribute attr, GameplayModifierMagnitude mag)
            => ge.Modifiers.Add(new GameplayModifierInfo { Attribute = attr, Operation = EAttributeModifierOp.Add, Magnitude = mag });

        private static AttackDefinition MakeAttack(string name, GameplayEffect damage, float dmg, float poise)
        {
            var a = ScriptableObject.CreateInstance<AttackDefinition>(); a.name = name;
            a.TargetEffect = damage;
            a.SetByCallerMagnitudes.Add(new SetByCallerMagnitude { Tag = CombatDemoTags.Data_Damage, Value = dmg });
            a.SetByCallerMagnitudes.Add(new SetByCallerMagnitude { Tag = CombatDemoTags.Data_PoiseDamage, Value = poise });
            return a;
        }

        private static BulletDefinition MakeBullet(string name, AttackDefinition attack, float speed, float radius, bool penetrateMap)
        {
            var b = ScriptableObject.CreateInstance<BulletDefinition>(); b.name = name;
            b.BulletCount = 1; b.InitialSpeed = speed; b.GravityScale = 0f; b.HitRadius = radius;
            b.Duration = 3f; b.HitLayers = ~0; b.PenetrateMap = penetrateMap; b.Attack = attack;
            return b;
        }

        private static T MakeAbility<T>(string name, GameplayTag tag, System.Action<T> cfg) where T : GameplayAbility
        {
            var a = ScriptableObject.CreateInstance<T>(); a.name = name; a.AbilityTags.Add(tag); cfg(a); return a;
        }

        private static MeleeAttackTrace.AttackTraceEntry MakeTraceEntry(AttackDefinition attack, Transform socket, float radius)
            => new MeleeAttackTrace.AttackTraceEntry
            {
                Attack = attack,
                Trace = new CollisionTraceDefinition { SocketTransforms = new List<Transform> { socket }, TraceRadius = radius }
            };

        private static WeaponComponent MakeWeapon(Transform parent, string label, GameplayTag weaponTag)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);
            var w = go.AddComponent<WeaponComponent>();
            w.WeaponTags.AddTag(weaponTag);
            return w;
        }

        private static InputProcessor Proc(InputProcessor p, GameplayTag inputTag, params InputTriggerEvent[] events)
        {
            p.InputTags.AddTag(inputTag);
            p.TriggerEvents = new List<InputTriggerEvent>(events);
            return p;
        }

        private static string _buildDir;

        // 给图元染色。⚠️ 材质必须存成**资产**再赋值：直接 `new Material(...)` 是内存对象，
        // SaveAsPrefabAsset 时无法序列化 → prefab 材质槽变 None → 加载后洋红"丢失材质"。
        private static void SetColor(GameObject go, Color c, string matName)
        {
            var mr = go.GetComponent<MeshRenderer>();
            if (mr == null) return;
            var shader = Shader.Find("Standard")
                      ?? Shader.Find("Universal Render Pipeline/Lit")
                      ?? Shader.Find("Sprites/Default");
            if (shader == null) return;
            var mat = new Material(shader) { color = c };

            string matDir = _buildDir + "/Materials";
            if (!AssetDatabase.IsValidFolder(matDir)) AssetDatabase.CreateFolder(_buildDir, "Materials");
            string matPath = $"{matDir}/{matName}.mat";
            AssetDatabase.DeleteAsset(matPath); // 重建幂等
            AssetDatabase.CreateAsset(mat, matPath);
            mr.sharedMaterial = mat;
        }

        // 用 SerializedObject 接私有序列化字段（对象引用 / 对象列表 / GameplayTag）
        private static void WireField(Object target, string prop, object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(prop);
            if (p != null)
            {
                if (value is List<Object> list)
                {
                    p.arraySize = list.Count;
                    for (int i = 0; i < list.Count; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
                }
                else if (value is Object obj) p.objectReferenceValue = obj;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireInputSystem(InputSystemComponent ic, InputConfig config, InputControlSetup setup)
        {
            var so = new SerializedObject(ic);
            so.FindProperty("inputConfig").objectReferenceValue = config;
            var setups = so.FindProperty("inputControlSetups");
            setups.arraySize = 1;
            setups.GetArrayElementAtIndex(0).objectReferenceValue = setup;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateAsset(Object asset, string path)
        {
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(asset, path);
        }
    }
}
