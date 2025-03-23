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

namespace MVZ2.GameContent.Enemies
{
    [EntityBehaviourDefinition(VanillaEnemyNames.necromancermax)]
    public class NecromancerMax : MeleeEnemy
    {
        public NecromancerMax(string nsp, string name) : base(nsp, name)
        {
        }

        public override void Init(Entity entity)
        {
            base.Init(entity);
            SetStateTimer(entity, new FrameTimer(CAST_COOLDOWN));
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
                        BuildBoneWalls(entity);
                    }
                }
            }
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

        private void BuildBoneWalls(Entity entity)
        {
            var level = entity.Level;
            int startLine = -5;
            int endLine = 5;
            var lane = entity.GetLane();
            if (lane == 0)
            {
                endLine = 0;
            }
            if (lane == level.GetMaxLaneCount() - 1)
            {
                startLine = 0;
            }

            for (int i = startLine; i <= endLine; i++)
            {
                var x = entity.Position.x + level.GetGridWidth() * 1.5f * entity.GetFacingX();
                var z = entity.Position.z + level.GetGridHeight() * i * 0.5f;
                var y = level.GetGroundY(x, z);
                Vector3 wallPos = new Vector3(x, y, z);
                var boneWall = level.Spawn(VanillaEnemyID.boneWall, wallPos, entity);
                boneWall.SetFactionAndDirection(entity.GetFaction());
            }
        }
        #region 常量
        private const int CAST_COOLDOWN = 200;
        private const int CAST_TIME = 30;
        private const int BUILD_DETECT_TIME = 25;
        private const int MAX_BONE_WALL_COUNT = 15;
        public static readonly NamespaceID ID = VanillaEnemyID.necromancermax;
        public static readonly VanillaEntityPropertyMeta PROP_STATE_TIMER = new VanillaEntityPropertyMeta("StateTimer");
        public static readonly VanillaEntityPropertyMeta PROP_CASTING = new VanillaEntityPropertyMeta("Casting");
        #endregion 常量
    }
}
