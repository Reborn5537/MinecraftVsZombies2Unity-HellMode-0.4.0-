﻿using System;
using System.Collections.Generic;
using System.Linq;
using MukioI18n;
using MVZ2.GameContent.Buffs.Enemies;
using MVZ2.GameContent.Buffs.Level;
using MVZ2.GameContent.Buffs.SeedPacks;
using MVZ2.GameContent.Contraptions;
using MVZ2.GameContent.Difficulties;
using MVZ2.GameContent.Effects;
using MVZ2.GameContent.Enemies;
using MVZ2.GameContent.Seeds;
using MVZ2.Vanilla.Audios;
using MVZ2.Vanilla.Contraptions;
using MVZ2.Vanilla.Entities;
using MVZ2.Vanilla.Grids;
using MVZ2.Vanilla.Properties;
using MVZ2.Vanilla.SeedPacks;
using MVZ2Logic;
using MVZ2Logic.Level;
using MVZ2Logic.SeedPacks;
using PVZEngine;
using PVZEngine.Buffs;
using PVZEngine.Callbacks;
using PVZEngine.Damages;
using PVZEngine.Entities;
using PVZEngine.Level;
using Tools;
using Tools.Mathematics;
using UnityEngine;

namespace MVZ2.GameContent.Bosses
{
    [EntityBehaviourDefinition(VanillaBossNames.rebornslenderman)]
    public class RebornSlenderMan : BossBehaviour
    {
        public RebornSlenderMan(string nsp, string name) : base(nsp, name)
        {
        }

        #region 回调
        public override void Init(Entity entity)
        {
            base.Init(entity);
            SetMoveTimer(entity, new FrameTimer(250));
            SetPortalTimer(entity, new FrameTimer(300));
            SetMindSwapTimer(entity, new FrameTimer(200));

            SetMoveRNG(entity, new RandomGenerator(entity.RNG.Next()));
            SetPortalRNG(entity, new RandomGenerator(entity.RNG.Next()));
            SetMindSwapRNG(entity, new RandomGenerator(entity.RNG.Next()));
            SetFateOptionRNG(entity, new RandomGenerator(entity.RNG.Next()));
            SetEventRNG(entity, new RandomGenerator(entity.RNG.Next()));

            var flyBuff = entity.AddBuff<FlyBuff>();
            flyBuff.SetProperty(FlyBuff.PROP_FLY_SPEED, 0.2f);
            flyBuff.SetProperty(FlyBuff.PROP_FLY_SPEED_FACTOR, 0.5f);
            flyBuff.SetProperty(FlyBuff.PROP_TARGET_HEIGHT, 80f);
        }
        protected override void UpdateAI(Entity entity)
        {
            base.UpdateAI(entity);
            if (entity.IsDead)
                return;
            MoveUpdate(entity);
            PortalUpdate(entity);
            MindSwapUpdate(entity);
        }
        protected override void UpdateLogic(Entity entity)
        {
            base.UpdateLogic(entity);
            if (entity.IsDead)
            {
                entity.Timeout--;
                if (entity.Timeout <= 0)
                {
                    entity.Remove();
                }
            }
            else
            {
                var readyTimes = GetReadyFateTimes(entity);
                if (readyTimes > 0)
                {
                    ChooseFate(entity);
                    readyTimes--;
                    SetReadyFateTimes(entity, readyTimes);
                }
            }
            entity.SetAnimationBool("IsDead", entity.IsDead);
        }
        public override void PreTakeDamage(DamageInput damage, CallbackResult result)
        {
            base.PreTakeDamage(damage, result);
            if (damage.Amount > 500)
            {
                damage.SetAmount(500);
            }
        }
        public override void PostTakeDamage(DamageOutput damage)
        {
            base.PostTakeDamage(damage);
            var boss = damage.Entity;
            if (boss.IsDead)
                return;
            if (boss.Health <= boss.GetMaxHealth() * 0.5f &&
        !boss.Level.HasBuff<ReverseSatelliteBuffRage>())
            {
                boss.Level.AddBuff<ReverseSatelliteBuffRage>();
            }

            var maxFateTimes = GetMaxFateTimes(boss.Level);
            float stageHP = boss.GetMaxHealth() / (float)(maxFateTimes + 2);
            int newStage = Mathf.FloorToInt(maxFateTimes + 2 - boss.Health / stageHP);
            var selectedFateTimes = GetSelectedFateTimes(boss);
            if (newStage > selectedFateTimes && newStage >= 0)
            {
                SetSelectedFateTimes(boss, newStage);
                var readyTimes = GetReadyFateTimes(boss);
                SetReadyFateTimes(boss, readyTimes + newStage - selectedFateTimes);
            }
        }
        public override void PostDeath(Entity entity, DeathInfo deathInfo)
        {
            base.PostDeath(entity, deathInfo);
            entity.PlaySound(VanillaSoundID.slendermanDeath);

            var darkMatter = entity.Spawn(VanillaEffectID.darkMatterParticles, entity.GetCenter());
            darkMatter.SetParent(entity);

            entity.SetAnimationBool("IsDead", true);
            entity.Timeout = 180;
        }
        #endregion

        #region Move
        private void MoveUpdate(Entity entity)
        {
            var timer = GetMoveTimer(entity);
            timer.Run();
            if (timer.Expired)
            {
                timer.Reset();
                StartMove(entity);
            }
            var motionTime = GetMoveTimeout(entity);
            if (motionTime <= 0)
                return;
            float lastPercent = Mathf.Clamp01(1 - motionTime / (float)MAX_MOVE_TIMEOUT);
            float lastMovePercent = MathTool.EaseInAndOut(lastPercent);

            motionTime--;

            SetMoveTimeout(entity, motionTime);

            float percent = Mathf.Clamp01(1 - motionTime / (float)MAX_MOVE_TIMEOUT);
            float movePercent = MathTool.EaseInAndOut(percent);
            var displacement = GetMoveDisplacement(entity);

            float addedPercent = movePercent - lastMovePercent;
            entity.Position += addedPercent * displacement;

            // End Moving.
            if (motionTime <= 0)
            {
                motionTime = 0;
                SetMoveDisplacement(entity, Vector3.zero);
            }
        }
        private void StartMove(Entity entity)
        {
            SetMoveTimeout(entity, MAX_MOVE_TIMEOUT);
            int endLane;
            int endColumn;
            var level = entity.Level;
            var moveRNG = GetMoveRNG(entity);
            do
            {
                endLane = moveRNG.Next(0, level.GetMaxLaneCount());
                endColumn = moveRNG.Next(0, level.GetMaxColumnCount());
            }
            while (endLane == entity.GetLane() && endColumn == entity.GetColumn());

            float endX = level.GetEntityColumnX(endColumn);
            float endZ = level.GetEntityLaneZ(endLane);
            SetMoveDisplacement(entity, new Vector3(endX - entity.Position.x, 0, endZ - entity.Position.z));
        }
        #endregion

        #region Portal
        private void PortalUpdate(Entity entity)
        {
            var portalTimer = GetPortalTimer(entity);
            portalTimer.Run();
            var level = entity.Level;
            if (portalTimer.Expired && !level.HasBuff<ReverseSatelliteBuffRage>())
            {
                portalTimer.Reset();
                CreatePortals(entity);
            }
            if (portalTimer.Expired && level.HasBuff<ReverseSatelliteBuffRage>())
            {
                portalTimer.Reset();
                CreateRagePortals(entity);
            }
        }

        private void CreatePortals(Entity entity)
        {
            var level = entity.Level;
            List<Vector2Int> placePool = new List<Vector2Int>();

            int maxColumnCount = level.GetMaxColumnCount();
            int maxRowCount = level.GetMaxLaneCount();
            for (int c = maxColumnCount - 3; c < maxColumnCount; c++)
            {
                for (int r = 0; r < maxRowCount; r++)
                {
                    placePool.Add(new Vector2Int(c, r));
                }
            }
            var portalRNG = GetPortalRNG(entity);
            for (int i = 0; i < 4; i++)
            {
                var index = portalRNG.Next(0, placePool.Count);
                Vector2Int place = placePool[index];
                placePool.Remove(place);

                var x = level.GetEntityColumnX(place.x);
                var z = level.GetEntityLaneZ(place.y);
                var y = level.GetGroundY(x, z);
                Vector3 pos = new Vector3(x, y, z);
                var enemyID = GetRandomPortalEnemyID(portalRNG);
                SpawnPortal(entity, pos, enemyID);
            }
            entity.PlaySound(VanillaSoundID.nightmarePortal);
        }
        private void CreateRagePortals(Entity entity)
        {
            var level = entity.Level;
            List<Vector2Int> placePool = new List<Vector2Int>();

            int maxColumnCount = level.GetMaxColumnCount();
            int maxRowCount = level.GetMaxLaneCount();
            for (int c = maxColumnCount - 3; c < maxColumnCount; c++)
            {
                for (int r = 0; r < maxRowCount; r++)
                {
                    placePool.Add(new Vector2Int(c, r));
                }
            }
            var portalRNG = GetPortalRNG(entity);
            for (int i = 0; i < 6; i++)
            {
                var index = portalRNG.Next(0, placePool.Count);
                Vector2Int place = placePool[index];
                placePool.Remove(place);

                var x = level.GetEntityColumnX(place.x);
                var z = level.GetEntityLaneZ(place.y);
                var y = level.GetGroundY(x, z);
                Vector3 pos = new Vector3(x, y, z);
                var enemyID = GetRandomRagePortalEnemyID(portalRNG);
                SpawnPortal(entity, pos, enemyID);
            }
            entity.PlaySound(VanillaSoundID.nightmarePortal);
        }
        private Entity SpawnPortal(Entity boss, Vector3 position, NamespaceID enemyID)
        {
            var portal = boss.SpawnWithParams(VanillaEffectID.nightmarePortal, position);
            NightmarePortal.SetEnemyID(portal, enemyID);
            return portal;
        }
        private NamespaceID GetRandomPortalEnemyID(RandomGenerator rng)
        {
            var index = rng.WeightedRandom(portalPoolWeights);
            return portalPool[index];
        }
        private NamespaceID GetRandomRagePortalEnemyID(RandomGenerator rng)
        {
            var index = rng.WeightedRandom(portalPoolRageWeights);
            return portalPoolRage[index];
        }
        #endregion

        #region Mind Swap
        private void MindSwapUpdate(Entity entity)
        {
            var mindSwapTimer = GetMindSwapTimer(entity);
            mindSwapTimer.Run();
            if (!mindSwapTimer.Expired)
                return;
            mindSwapTimer.Reset();
            var level = entity.Level;
            if (!level.IsConveyorMode())
                return;

            var rng = GetMindSwapRNG(entity);
            NamespaceID[] pool;
            if (level.Difficulty == VanillaDifficulties.hard)
            {
                pool = hardMindSwapPool;
            }
            else if (level.Difficulty == VanillaDifficulties.hell)
            {
                pool = hellMindSwapPool;
            }
            else
            {
                pool = mindSwapPool;
            }
            for (int i = 0; i < level.GetConveyorSeedPackCount(); i++)
            {
                var blueprint = level.GetConveyorSeedPackAt(i);
                var blueprintDef = blueprint?.Definition;
                if (blueprintDef == null)
                    continue;
                if (blueprintDef.GetSeedType() != SeedTypes.ENTITY)
                    continue;
                var entityID = blueprintDef.GetSeedEntityID();
                var entityDef = level.Content.GetEntityDefinition(entityID);
                if (entityDef == null)
                    continue;
                if (entityDef.Type != EntityTypes.PLANT)
                    continue;
                var targetID = pool.Random(rng);
                Buff buff = blueprint.AddBuff<SlenderManMindSwapBuff>();
                buff.SetProperty(SlenderManMindSwapBuff.PROP_TARGET_ID, targetID);
            }
        }
        #endregion

        #region Fate Choose
        private void ChooseFate(Entity entity)
        {
            var level = entity.Level;
            level.PauseGame(100);
            var title = Global.Game.GetText(CHOOSE_FATE_TITLE);
            var desc = Global.Game.GetText(CHOOSE_FATE_DESCRIPTION);

            int count = 3;
            if (level.Difficulty == VanillaDifficulties.easy)
            {
                count = 4;
            }
            else if (level.Difficulty == VanillaDifficulties.hard)
            {
                count = 2;
            }
            else if (level.Difficulty == VanillaDifficulties.hell)
            {
                count = 1;
            }
            var rng = GetFateOptionRNG(entity);
            var selected = fateOptions.RandomTake(count, rng).ToArray();
            var options = selected.Select(i => GetFateOptionText(i)).ToArray();
            level.ShowDialog(title, desc, options, (i) =>
            {
                var option = selected[i];
                DoFate(entity, option);
                level.ResumeGame(120);
            });
        }
        private void DoFate(Entity boss, int option)
        {
            switch (option)
            {
                case FATE_PANDORAS_BOX:
                    PandorasBox(boss);
                    break;
                case FATE_BIOHAZARD:
                    Biohazard(boss);
                    break;
                case FATE_DECREPIFY:
                    Decrepify(boss);
                    break;
                case FATE_INSANITY:
                    Insanity(boss);
                    break;
                case FATE_COME_TRUE:
                    ComeTrue(boss);
                    break;
                case FATE_THE_LURKER:
                    TheLurker(boss);
                    break;
                case FATE_BLACK_SUN:
                    BlackSun(boss);
                    break;
                case FATE_MIRROR_WAR:
                    MirrorWar(boss);
                    break;
                case FATE_B_S:
                    BS(boss);
                    break;
            }
        }

        private void PandorasBox(Entity boss)
        {
            boss.PlaySound(VanillaSoundID.odd);

            var level = boss.Level;
            var eventRng = GetEventRNG(boss);
            var rng = new RandomGenerator(eventRng.Next());
            var contraptions = level.FindEntities(e => e.Type == EntityTypes.PLANT && e.IsHostile(boss));
            foreach (var contraption in contraptions)
            {
                contraption.ClearTakenGrids();
            }
            var grids = level.GetAllGrids();
            foreach (var contraption in contraptions)
            {
                var placementID = contraption.Definition.GetPlacementID();
                var placementDef = level.Content.GetPlacementDefinition(placementID);
                if (placementDef == null)
                    continue;
                var targetGrids = grids.Where(g => g.CanPlaceEntity(contraption.GetDefinitionID()));
                if (targetGrids.Count() <= 0)
                    continue;
                var grid = targetGrids.Random(rng);
                contraption.Position = grid.GetEntityPosition();
                contraption.UpdateTakenGrids();
            }
        }
        private void Biohazard(Entity boss)
        {
            boss.PlaySound(VanillaSoundID.biohazard);
            boss.PlaySound(VanillaSoundID.nightmarePortal);
            var level = boss.Level;
            for (int column = 0; column < 2; column++)
            {
                float x = level.GetEntityColumnX(level.GetMaxColumnCount() - 1 - column);
                for (int lane = 0; lane < level.GetMaxLaneCount(); lane++)
                {
                    var z = level.GetEntityLaneZ(lane);
                    var y = level.GetGroundY(x, z);
                    Vector3 pos = new Vector3(x, y, z);
                    SpawnPortal(boss, pos, VanillaEnemyID.ironHelmettedZombie);
                }
            }
        }

        private void Decrepify(Entity boss)
        {
            boss.PlaySound(VanillaSoundID.decrepify);
            boss.Level.AddBuff<NightmareDecrepifyBuff>();
        }

        private void Insanity(Entity boss)
        {
            boss.PlaySound(VanillaSoundID.confuse);

            var level = boss.Level;
            var rng = GetEventRNG(boss);
            var targets = level.FindEntities(e => e.Type == EntityTypes.PLANT && e.IsHostile(boss) && !e.IsLoyal()).RandomTake(5, rng);
            foreach (var target in targets)
            {
                target.Charm(boss.GetFaction());
            }
        }


        private void ComeTrue(Entity boss)
        {
            boss.PlaySound(VanillaSoundID.nyaightmareScream);

            var level = boss.Level;
            var targets = level.FindEntities(e => e.Type == EntityTypes.ENEMY && e.IsFriendly(boss) && !e.IsEntityOf(VanillaEnemyID.ghast));
            foreach (var enemy in targets)
            {
                var ghast = boss.SpawnWithParams(VanillaEnemyID.ghast, enemy.Position);
                ghast.AddBuff<NightmareComeTrueBuff>();
                enemy.Remove();
            }
        }

        private void TheLurker(Entity boss)
        {
            boss.PlaySound(VanillaSoundID.splashBig);
            boss.PlaySound(VanillaSoundID.lurker);

            var level = boss.Level;
            level.ShakeScreen(50, 0, 30);
            var targets = level.FindEntities(e => e.Type == EntityTypes.PLANT && e.IsOnWater());
            foreach (var target in targets)
            {
                target.Die(boss);
            }
        }

        private void BlackSun(Entity boss)
        {
            boss.PlaySound(VanillaSoundID.powerOff);
            boss.PlaySound(VanillaSoundID.reverseVampire);
            boss.PlaySound(VanillaSoundID.confuse);

            var level = boss.Level;
            var targets = level.FindEntities(e => e.Type == EntityTypes.PLANT && e.CanDeactive());
            foreach (var contraption in targets)
            {
                contraption.ShortCircuit(350);
            }
        }
        private void MirrorWar(Entity boss)
        {
            boss.PlaySound(VanillaSoundID.nightmarePortal);
            var level = boss.Level;
            int maxColumn = level.GetMaxColumnCount();

            var leftPlants = level.FindEntities(e =>
                e.Type == EntityTypes.PLANT &&
                e.GetColumn() <= 1 &&
                !e.IsCharmed()
            );

            foreach (var plant in leftPlants)
            {
                int mirrorColumn = maxColumn - 1 - plant.GetColumn();
                if (mirrorColumn < maxColumn - 2) continue;

                int targetLane = plant.GetLane();
                float x = level.GetEntityColumnX(mirrorColumn);
                float z = level.GetEntityLaneZ(targetLane);

                float y = level.GetGroundY(x, z);
                var mirrorPlant = level.Spawn(plant.GetDefinitionID(), new Vector3(x, y, z), boss);
                mirrorPlant.CharmWithSource(boss);

                Vector3 pos = new Vector3(x, y, z);
                var portal = SpawnPortal(boss, pos, VanillaEnemyID.boneWall);
            }

            level.ShakeScreen(20, 0.5f, 15);
        }
        private void BS(Entity boss)
        {
            boss.PlaySound(VanillaSoundID.decrepify);

            var level = boss.Level;
            var targets = level.FindEntities(e =>
                e.Type == EntityTypes.PLANT || e.Type == EntityTypes.ENEMY
            );
            var rng = GetEventRNG(boss);

            foreach (var target in targets)
            {
                // 如果目标已有任意一个Buff，则跳过
                if (target.HasBuff<BigTroubleBuff>() || target.HasBuff<LittleZombieBuff>())
                {
                    continue;
                }

                // 50%概率选中目标
                if (rng.Next(2) == 0)
                {
                    // 随机分配Buff（各50%）
                    if (rng.Next(2) == 0)
                    {
                        target.AddBuff<BigTroubleBuff>();
                    }
                    else
                    {
                        target.AddBuff<LittleZombieBuff>();
                    }
                }
            }
        }
        private bool IsGridOccupied(LevelEngine level, int column, int lane)
        {
            return level.FindEntities(e =>
                (e.Type == EntityTypes.PLANT || e.Type == EntityTypes.OBSTACLE) &&
                e.GetColumn() == column &&
                e.GetLane() == lane
            ).Any();
        }
        private static string GetFateOptionText(int option)
        {
            var index = Array.IndexOf(fateOptions, option);
            var text = fateTexts[index];
            return Global.Game.GetText(text);
        }
        #endregion

        private int GetMaxFateTimes(LevelEngine level)
        {
            if (level.Difficulty == VanillaDifficulties.easy)
            {
                return 3;
            }
            else if (level.Difficulty == VanillaDifficulties.hard)
            {
                return 5;
            }
            else if (level.Difficulty == VanillaDifficulties.hell)
            {
                return 9;
            }
            return 4;
        }

        #region 属性
        public static int GetSelectedFateTimes(Entity boss) => boss.GetBehaviourField<int>(ID, PROP_SELECTED_FATE_TIMES);
        public static void SetSelectedFateTimes(Entity boss, int value) => boss.SetBehaviourField(ID, PROP_SELECTED_FATE_TIMES, value);
        public static int GetReadyFateTimes(Entity boss) => boss.GetBehaviourField<int>(ID, PROP_READY_FATE_TIMES);
        public static void SetReadyFateTimes(Entity boss, int value) => boss.SetBehaviourField(ID, PROP_READY_FATE_TIMES, value);

        #region 移动
        public static FrameTimer GetMoveTimer(Entity boss) => boss.GetBehaviourField<FrameTimer>(ID, PROP_MOVE_TIMER);
        public static void SetMoveTimer(Entity boss, FrameTimer value) => boss.SetBehaviourField(ID, PROP_MOVE_TIMER, value);
        public static int GetMoveTimeout(Entity boss) => boss.GetBehaviourField<int>(ID, PROP_MOVE_TIMEOUT);
        public static void SetMoveTimeout(Entity boss, int value) => boss.SetBehaviourField(ID, PROP_MOVE_TIMEOUT, value);
        public static Vector3 GetMoveDisplacement(Entity boss) => boss.GetBehaviourField<Vector3>(ID, PROP_MOVE_DISPLACEMENT);
        public static void SetMoveDisplacement(Entity boss, Vector3 value) => boss.SetBehaviourField(ID, PROP_MOVE_DISPLACEMENT, value);
        #endregion

        #region 传送门
        public static FrameTimer GetPortalTimer(Entity boss) => boss.GetBehaviourField<FrameTimer>(ID, PROP_PORTAL_TIMER);
        public static void SetPortalTimer(Entity boss, FrameTimer value) => boss.SetBehaviourField(ID, PROP_PORTAL_TIMER, value);
        #endregion

        #region 精神交换
        public static FrameTimer GetMindSwapTimer(Entity boss) => boss.GetBehaviourField<FrameTimer>(ID, PROP_MIND_SWAP_TIMER);
        public static void SetMindSwapTimer(Entity boss, FrameTimer value) => boss.SetBehaviourField(ID, PROP_MIND_SWAP_TIMER, value);
        #endregion

        #region RNG
        public static RandomGenerator GetMoveRNG(Entity boss) => boss.GetBehaviourField<RandomGenerator>(ID, PROP_MOVE_RNG);
        public static void SetMoveRNG(Entity boss, RandomGenerator value) => boss.SetBehaviourField(ID, PROP_MOVE_RNG, value);
        public static RandomGenerator GetPortalRNG(Entity boss) => boss.GetBehaviourField<RandomGenerator>(ID, PROP_PORTAL_RNG);
        public static void SetPortalRNG(Entity boss, RandomGenerator value) => boss.SetBehaviourField(ID, PROP_PORTAL_RNG, value);
        public static RandomGenerator GetMindSwapRNG(Entity boss) => boss.GetBehaviourField<RandomGenerator>(ID, PROP_MIND_SWAP_RNG);
        public static void SetMindSwapRNG(Entity boss, RandomGenerator value) => boss.SetBehaviourField(ID, PROP_MIND_SWAP_RNG, value);
        public static RandomGenerator GetFateOptionRNG(Entity boss) => boss.GetBehaviourField<RandomGenerator>(ID, PROP_FATE_OPTION_RNG);
        public static void SetFateOptionRNG(Entity boss, RandomGenerator value) => boss.SetBehaviourField(ID, PROP_FATE_OPTION_RNG, value);
        public static RandomGenerator GetEventRNG(Entity boss) => boss.GetBehaviourField<RandomGenerator>(ID, PROP_EVENT_RNG);
        public static void SetEventRNG(Entity boss, RandomGenerator value) => boss.SetBehaviourField(ID, PROP_EVENT_RNG, value);
        #endregion

        #endregion 属性

        #region 常量
        public static readonly NamespaceID ID = VanillaBossID.slenderman;
        //private static readonly string[] colors = { "<color=purple>???</color>", "<color=red>???</color>", "<color=yellow>???</color>", "<color=blue>???</color>", "<color=black>???</color>", "<color=yellow>???</color>"};

        [TranslateMsg("梦魇对话框标题")]
        public const string CHOOSE_FATE_TITLE = "欢迎回来<color=red>将死之人</color>";
        [TranslateMsg("梦魇对话框文本")]
        public const string CHOOSE_FATE_DESCRIPTION = "这一次你没有机会了<color=red>=)</color>";
        [TranslateMsg("梦魇选项")]
        public const string FATE_TEXT_PANDORAS_BOX = "<color=purple>打开那扇尘封的门</color>";
        [TranslateMsg("梦魇选项")]
        public const string FATE_TEXT_BIOHAZARD = "<color=red>唤醒沉睡的亡者</color>";
        [TranslateMsg("梦魇选项")]
        public const string FATE_TEXT_DECREPIFY = "<color=purple>接受时间的重量</color>";
        [TranslateMsg("梦魇选项")]
        public const string FATE_TEXT_INSANITY = "<color=red>窥视理智的彼端</color>";
        [TranslateMsg("梦魇选项")]
        public const string FATE_TEXT_COME_TRUE = "<color=yellow>让噩梦照进现实</color>";
        [TranslateMsg("梦魇选项")]
        public const string FATE_TEXT_THE_LURKER = "<color=blue>潜入无光的深渊</color>";
        [TranslateMsg("梦魇选项")]
        public const string FATE_TEXT_BLACK_SUN = "<color=black>凝视那永不落下的黑暗</color>";
        [TranslateMsg("梦魇选项")]
        public const string FATE_TEXT_MIRROR_WAR = "<color=black>熵寂齿轮的真相</color>";
        [TranslateMsg("梦魇选项")]
        public const string FATE_TEXT_B_S = "<color=black>面对未知的可能</color>";

        public const int MAX_MOVE_TIMEOUT = 30;

        public static readonly VanillaEntityPropertyMeta<int> PROP_SELECTED_FATE_TIMES = new VanillaEntityPropertyMeta<int>("SelectedFateTimes");
        public static readonly VanillaEntityPropertyMeta<int> PROP_READY_FATE_TIMES = new VanillaEntityPropertyMeta<int>("ReadyFateTimes");

        public static readonly VanillaEntityPropertyMeta<FrameTimer> PROP_MOVE_TIMER = new VanillaEntityPropertyMeta<FrameTimer>("MoveTimer");
        public static readonly VanillaEntityPropertyMeta<int> PROP_MOVE_TIMEOUT = new VanillaEntityPropertyMeta<int>("MoveTimeout");
        public static readonly VanillaEntityPropertyMeta<Vector3> PROP_MOVE_DISPLACEMENT = new VanillaEntityPropertyMeta<Vector3>("MoveDisplacement");

        public static readonly VanillaEntityPropertyMeta<FrameTimer> PROP_PORTAL_TIMER = new VanillaEntityPropertyMeta<FrameTimer>("PortalTimer");

        public static readonly VanillaEntityPropertyMeta<FrameTimer> PROP_MIND_SWAP_TIMER = new VanillaEntityPropertyMeta<FrameTimer>("MindSwapTimer");

        public static readonly VanillaEntityPropertyMeta<RandomGenerator> PROP_MOVE_RNG = new VanillaEntityPropertyMeta<RandomGenerator>("MoveRNG");
        public static readonly VanillaEntityPropertyMeta<RandomGenerator> PROP_PORTAL_RNG = new VanillaEntityPropertyMeta<RandomGenerator>("PortalRNG");
        public static readonly VanillaEntityPropertyMeta<RandomGenerator> PROP_MIND_SWAP_RNG = new VanillaEntityPropertyMeta<RandomGenerator>("MindSwapRNG");
        public static readonly VanillaEntityPropertyMeta<RandomGenerator> PROP_FATE_OPTION_RNG = new VanillaEntityPropertyMeta<RandomGenerator>("FateOptionRNG");
        public static readonly VanillaEntityPropertyMeta<RandomGenerator> PROP_EVENT_RNG = new VanillaEntityPropertyMeta<RandomGenerator>("EventRNG");

        public const int FATE_PANDORAS_BOX = 0;
        public const int FATE_BIOHAZARD = 1;
        public const int FATE_DECREPIFY = 2;
        public const int FATE_INSANITY = 3;
        public const int FATE_COME_TRUE = 4;
        public const int FATE_THE_LURKER = 5;
        public const int FATE_BLACK_SUN = 6;
        public const int FATE_MIRROR_WAR = 7;
        public const int FATE_B_S = 8;

        private static NamespaceID[] portalPool = new NamespaceID[]
        {
            VanillaEnemyID.zombie,
            VanillaEnemyID.leatherCappedZombie,
            VanillaEnemyID.ironHelmettedZombie,
            VanillaEnemyID.necromancer,
            VanillaEnemyID.boneWall,
            VanillaEnemyID.ghast,
            VanillaEnemyID.reverseSatellite,
            VanillaEnemyID.nightmarefollower,
        };

        private static int[] portalPoolWeights = new int[]
        {
            10,
            5,
            3,
            2,
            6,
            2,
            1,
            5,
        };
        private static NamespaceID[] portalPoolRage = new NamespaceID[]
        {
            VanillaEnemyID.necromancermax,
            VanillaEnemyID.boneWall,
            VanillaEnemyID.ghast,
            VanillaEnemyID.skeletonHorse,
            VanillaEnemyID.mesmerizermax,
            VanillaEnemyID.megaMutantZombie,
            VanillaEnemyID.nightmarefollower,
        };

        private static int[] portalPoolRageWeights = new int[]
        {
            2,
            5,
            6,
            8,
            5,
            1,
            5,
        };
        private static NamespaceID[] mindSwapPool = new NamespaceID[]
        {
            VanillaBlueprintID.FromEntity(VanillaContraptionID.lilyPad),
            VanillaBlueprintID.FromEntity(VanillaContraptionID.drivenser),
            VanillaBlueprintID.FromEntity(VanillaContraptionID.gravityPad),
            VanillaBlueprintID.FromEntity(VanillaContraptionID.vortexHopper),
            VanillaBlueprintID.FromEntity(VanillaContraptionID.totenser),
            VanillaBlueprintID.FromEntity(VanillaContraptionID.dreamCrystal),
            VanillaBlueprintID.FromEntity(VanillaContraptionID.dreamSilk)
        };
        private static NamespaceID[] hardMindSwapPool = new NamespaceID[]
        {
            VanillaBlueprintID.FromEntity(VanillaContraptionID.lilyPad),
            VanillaBlueprintID.FromEntity(VanillaContraptionID.drivenser),
            VanillaBlueprintID.FromEntity(VanillaContraptionID.gravityPad),
            VanillaBlueprintID.FromEntity(VanillaContraptionID.vortexHopper),
            VanillaBlueprintID.FromEntity(VanillaContraptionID.pistenser),
            VanillaBlueprintID.FromEntity(VanillaContraptionID.totenser),
            VanillaBlueprintID.FromEntity(VanillaContraptionID.dreamCrystal),
            VanillaBlueprintID.FromEntity(VanillaContraptionID.dreamSilk),
            VanillaBlueprintID.FromEntity(VanillaEnemyID.zombie)
        };
        private static NamespaceID[] hellMindSwapPool = new NamespaceID[]
        {
            VanillaBlueprintID.FromEntity(VanillaContraptionID.lilyPad),
            VanillaBlueprintID.FromEntity(VanillaContraptionID.drivenser),
            VanillaBlueprintID.FromEntity(VanillaContraptionID.gravityPad),
            VanillaBlueprintID.FromEntity(VanillaContraptionID.vortexHopper),
            VanillaBlueprintID.FromEntity(VanillaEnemyID.ghast),
            VanillaBlueprintID.FromEntity(VanillaContraptionID.pistenser),
            VanillaBlueprintID.FromEntity(VanillaContraptionID.totenser),
            VanillaBlueprintID.FromEntity(VanillaContraptionID.dreamCrystal),
            VanillaBlueprintID.FromEntity(VanillaContraptionID.dreamSilk),
            VanillaBlueprintID.FromEntity(VanillaContraptionID.snipedispenser),
            VanillaBlueprintID.FromEntity(VanillaEnemyID.zombie),
            VanillaBlueprintID.FromEntity(VanillaEnemyID.boneWall),
            VanillaBlueprintID.FromEntity(VanillaEnemyID.necromancer),
            VanillaBlueprintID.FromEntity(VanillaEnemyID.skeletonHorse),
            VanillaBlueprintID.FromEntity(VanillaEnemyID.nightmarefollower)
        };
        private static int[] fateOptions = new int[]
        {
            FATE_PANDORAS_BOX,
            FATE_BIOHAZARD,
            FATE_DECREPIFY,
            FATE_INSANITY,
            FATE_COME_TRUE,
            FATE_THE_LURKER,
            FATE_BLACK_SUN,
            FATE_MIRROR_WAR,
            FATE_B_S,
        };
        private static string[] fateTexts = new string[]
        {
            FATE_TEXT_PANDORAS_BOX,
            FATE_TEXT_BIOHAZARD,
            FATE_TEXT_DECREPIFY,
            FATE_TEXT_INSANITY,
            FATE_TEXT_COME_TRUE,
            FATE_TEXT_THE_LURKER,
            FATE_TEXT_BLACK_SUN,
            FATE_TEXT_MIRROR_WAR,
            FATE_TEXT_B_S,
        };
        #endregion 常量
    }
}
