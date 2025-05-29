using MVZ2.Vanilla.Audios;
using MVZ2.Vanilla.Enemies;
using MVZ2.Vanilla.Entities;
using MVZ2.Vanilla.Properties;
using PVZEngine;
using PVZEngine.Damages;
using PVZEngine.Entities;
using PVZEngine.Level;
using Tools;
using UnityEngine;
using MVZ2.GameContent.Buffs.Enemies;
using MVZ2.GameContent.Damages;
using MVZ2.GameContent.Effects;
using MVZ2.GameContent.Models;
using MVZ2.Vanilla;
using MVZ2.Vanilla.Level;
using MVZ2Logic;
using System.Collections.Generic;
using System;
using System.Linq;
using MukioI18n;
using MVZ2.GameContent.Buffs.Level;
using MVZ2.GameContent.Buffs.SeedPacks;
using MVZ2.GameContent.Contraptions;
using MVZ2.GameContent.Difficulties;
using MVZ2.GameContent.Enemies;
using MVZ2.GameContent.Seeds;
using MVZ2.Vanilla.Contraptions;
using MVZ2.Vanilla.Grids;
using MVZ2.Vanilla.SeedPacks;
using MVZ2Logic.Level;
using MVZ2Logic.SeedPacks;
using PVZEngine.Buffs;
using Tools.Mathematics;

namespace MVZ2.GameContent.Enemies
{
    [EntityBehaviourDefinition(VanillaEnemyNames.nightmarefollower)]
    public class NightMareFollower : MeleeEnemy
    {
        public NightMareFollower(string nsp, string name) : base(nsp, name)
        {
        }

        public override void Init(Entity entity)
        {
            base.Init(entity);
            SetStateTimer(entity, new FrameTimer(CAST_COOLDOWN));
            SetPortalTimer(entity, new FrameTimer(PORTAL_COOLDOWN));
            SetPortalRNG(entity, new RandomGenerator(entity.RNG.Next()));
            var level = entity.Level;
            if (level.IsWaterLane(entity.GetLane()))
            {
                entity.AddBuff<BoatBuff>();
                entity.SetAnimationBool("HasBoat", true);
            }
        }

        protected override int GetActionState(Entity enemy)
        {
            var state = base.GetActionState(enemy);
            if (state == VanillaEntityStates.WALK && IsCasting(enemy))
            {
                return VanillaEntityStates.NECROMANCER_CAST;
            }
            return state;
        }

        protected override void UpdateLogic(Entity entity)
        {
            base.UpdateLogic(entity);
            entity.SetAnimationInt("HealthState", entity.GetHealthState(8));
            entity.SetAnimationBool("HasBoat", entity.HasBuff<BoatBuff>());
        }

        protected override void UpdateAI(Entity entity)
        {
            base.UpdateAI(entity);

            if (entity.IsDead)
                return;

            var portalTimer = GetPortalTimer(entity);
            portalTimer.Run(entity.GetAttackSpeed());
            if (portalTimer.Expired && !IsCasting(entity))
            {
                if (entity.Level.FindEntities(VanillaEffectID.nightmarePortal).Length < MAX_PORTAL_COUNT)
                {
                    CreatePortals(entity);
                }
                portalTimer.ResetTime(PORTAL_COOLDOWN);
            }

            if (entity.State == VanillaEntityStates.ATTACK)
                return;

            var stateTimer = GetStateTimer(entity);
            if (entity.State == VanillaEntityStates.NECROMANCER_CAST)
            {
                stateTimer.Run(entity.GetAttackSpeed());
                if (stateTimer.Expired)
                {
                    EndCasting(entity);
                }
            }
            else
            {
                stateTimer.Run(entity.GetAttackSpeed());
                if (stateTimer.Expired)
                {
                    if (!CheckBuildable(entity))
                    {
                        stateTimer.ResetTime(BUILD_DETECT_TIME);
                    }
                    else
                    {
                        StartCasting(entity);
                        BuildBoneWall(entity);
                    }
                }
            }
        }

        private void BuildBoneWall(Entity entity)
        {
            var level = entity.Level;
            var x = entity.Position.x + level.GetGridWidth() * entity.GetFacingX();
            var z = entity.Position.z;
            var y = level.GetGroundY(x, z);
            Vector3 wallPos = new Vector3(x, y, z);
            var boneWall = level.Spawn(VanillaEnemyID.boneWall, wallPos, entity);
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
            int portalCount = Mathf.Min(4, placePool.Count);

            bool hasSpawnedNightmareFollower = false;

            for (int i = 0; i < portalCount; i++)
            {
                if (placePool.Count == 0) break;

                var index = portalRNG.Next(0, placePool.Count);
                Vector2Int place = placePool[index];
                placePool.RemoveAt(index);

                var x = level.GetEntityColumnX(place.x);
                var z = level.GetEntityLaneZ(place.y);
                var y = level.GetGroundY(x, z);
                Vector3 pos = new Vector3(x, y, z);

                NamespaceID enemyID;

                if (hasSpawnedNightmareFollower)
                {
                    enemyID = GetRandomNonNightmareFollowerEnemyID(portalRNG);
                }
                else
                {
                    enemyID = GetRandomPortalEnemyID(portalRNG);
                    if (enemyID == VanillaEnemyID.nightmarefollower)
                    {
                        hasSpawnedNightmareFollower = true;
                        entity.Die(entity);
                    }
                }

                SpawnPortal(entity, pos, enemyID);
            }
            entity.PlaySound(VanillaSoundID.nightmarePortal);
        }

        private NamespaceID GetRandomNonNightmareFollowerEnemyID(RandomGenerator rng)
        {
            var filteredPool = portalPool.Where(id => id != VanillaEnemyID.nightmarefollower).ToArray();
            var filteredWeights = portalPoolWeights
                .Select((weight, index) => new { weight, index })
                .Where(x => portalPool[x.index] != VanillaEnemyID.nightmarefollower)
                .Select(x => x.weight)
                .ToArray();

            var index = rng.WeightedRandom(filteredWeights);
            return filteredPool[index];
        }
        private void SpawnPortal(Entity entity, Vector3 pos, NamespaceID enemyID)
        {
            float currentHealth = entity.Health;

            var portal = entity.Level.Spawn(VanillaEffectID.nightmarePortal, pos, entity);
            NightmarePortal.SetEnemyID(portal, enemyID);
            portal.PlaySound(VanillaSoundID.nightmarePortal);

            if (enemyID == VanillaEnemyID.nightmarefollower)
            {
                var spawnedEnemy = entity.Level.FindEntities(e =>
                    e.IsEntityOf(VanillaEnemyID.nightmarefollower) &&
                    Vector3.Distance(e.Position, pos) < 0.1f
                ).FirstOrDefault();

                if (spawnedEnemy != null)
                {
                    spawnedEnemy.Health = currentHealth;
                }
            }
        }

        private NamespaceID GetRandomPortalEnemyID(RandomGenerator rng)
        {
            var index = rng.WeightedRandom(portalPoolWeights);
            return portalPool[index];
        }

        public override void PostDeath(Entity entity, DeathInfo info)
        {
            base.PostDeath(entity, info);
            if (entity.HasBuff<BoatBuff>())
            {
                entity.RemoveBuffs<BoatBuff>();
                // 掉落碎船掉落物
                var effect = entity.Level.Spawn(VanillaEffectID.brokenArmor, entity.GetCenter(), entity);
                effect.Velocity = new Vector3(effect.RNG.NextFloat() * 20 - 10, 5, 0);
                effect.ChangeModel(VanillaModelID.boatItem);
                effect.SetDisplayScale(entity.GetDisplayScale());
            }
            if (entity.State == VanillaEntityStates.NECROMANCER_CAST)
            {
                EndCasting(entity);
            }
        }

        public static void SetCasting(Entity entity, bool timer)
        {
            entity.SetBehaviourField(ID, PROP_CASTING, timer);
        }

        public static bool IsCasting(Entity entity)
        {
            return entity.GetBehaviourField<bool>(ID, PROP_CASTING);
        }

        public static void SetStateTimer(Entity entity, FrameTimer timer)
        {
            entity.SetBehaviourField(ID, PROP_STATE_TIMER, timer);
        }

        public static FrameTimer GetStateTimer(Entity entity)
        {
            return entity.GetBehaviourField<FrameTimer>(ID, PROP_STATE_TIMER);
        }

        public static void SetPortalTimer(Entity entity, FrameTimer timer)
        {
            entity.SetBehaviourField(ID, PROP_PORTAL_TIMER, timer);
        }

        public static FrameTimer GetPortalTimer(Entity entity)
        {
            return entity.GetBehaviourField<FrameTimer>(ID, PROP_PORTAL_TIMER);
        }

        public static void SetPortalRNG(Entity entity, RandomGenerator rng)
        {
            entity.SetBehaviourField(ID, PROP_PORTAL_RNG, rng);
        }

        public static RandomGenerator GetPortalRNG(Entity entity)
        {
            return entity.GetBehaviourField<RandomGenerator>(ID, PROP_PORTAL_RNG);
        }

        private void StartCasting(Entity entity)
        {
            SetCasting(entity, true);
            entity.PlaySound(VanillaSoundID.reviveCast);
            var stateTimer = GetStateTimer(entity);
            stateTimer.ResetTime(CAST_TIME);
        }

        private void EndCasting(Entity entity)
        {
            SetCasting(entity, false);
            var stateTimer = GetStateTimer(entity);
            stateTimer.ResetTime(CAST_COOLDOWN);
        }

        private bool CheckBuildable(Entity entity)
        {
            return entity.Level.FindEntities(VanillaEnemyID.boneWall).Length < MAX_BONE_WALL_COUNT;
        }

        #region 常量
        private const int CAST_COOLDOWN = 200;
        private const int CAST_TIME = 30;
        private const int BUILD_DETECT_TIME = 25;
        private const int MAX_BONE_WALL_COUNT = 15;
        private const int PORTAL_COOLDOWN = 500;
        private const int MAX_PORTAL_COUNT = 4;

        private static NamespaceID[] portalPool = new NamespaceID[]
        {
            VanillaEnemyID.zombie,
            VanillaEnemyID.leatherCappedZombie,
            VanillaEnemyID.ironHelmettedZombie,
            VanillaEnemyID.necromancer,
            VanillaEnemyID.boneWall,
            VanillaEnemyID.berserkermax,
            VanillaEnemyID.nightmarefollower,
        };

        private static int[] portalPoolWeights = new int[]
        {
            10,
            5,
            3,
            2,
            6,
            1,
            2
        };


        public static readonly NamespaceID ID = VanillaEnemyID.nightmarefollower;
        public static readonly VanillaEntityPropertyMeta<FrameTimer> PROP_STATE_TIMER = new VanillaEntityPropertyMeta<FrameTimer>("StateTimer");
        public static readonly VanillaEntityPropertyMeta<bool> PROP_CASTING = new VanillaEntityPropertyMeta<bool>("Casting");
        public static readonly VanillaEntityPropertyMeta<FrameTimer> PROP_PORTAL_TIMER = new VanillaEntityPropertyMeta<FrameTimer>("PortalTimer");
        public static readonly VanillaEntityPropertyMeta<RandomGenerator> PROP_PORTAL_RNG = new VanillaEntityPropertyMeta<RandomGenerator>("PortalRNG");
        public static readonly VanillaEntityPropertyMeta<EntityID> PROP_PORTAL_ENEMY_ID = new VanillaEntityPropertyMeta<EntityID>("PortalEnemyID");
        #endregion 常量
    }
}