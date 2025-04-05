using MVZ2.GameContent.Damages;
using MVZ2.GameContent.Effects;
using MVZ2.Vanilla.Audios;
using MVZ2.Vanilla.Enemies;
using MVZ2.Vanilla.Entities;
using PVZEngine.Damages;
using PVZEngine.Entities;
using PVZEngine.Level;
using MVZ2.GameContent.Buffs.Enemies;
using MVZ2.GameContent.Models;
using MVZ2.Vanilla;
using MVZ2.Vanilla.Level;
using MVZ2Logic;
using UnityEngine;

namespace MVZ2.GameContent.Enemies
{
    [EntityBehaviourDefinition(VanillaEnemyNames.boneWall)]
    public class BoneWall : StateEnemy
    {
        private bool _hasBoat;

        public BoneWall(string nsp, string name) : base(nsp, name)
        {
        }

        public override void Init(Entity entity)
        {
            base.Init(entity);
            entity.AddBuff<GhostBuff>();
            var buff = entity.AddBuff<FlyBuff>();
            buff.SetProperty(FlyBuff.PROP_TARGET_HEIGHT, 0);
            entity.Timeout = entity.GetMaxTimeout();
            entity.PlaySound(VanillaSoundID.boneWallBuild);

            var level = entity.Level;
            _hasBoat = level.IsWaterLane(entity.GetLane());
            if (_hasBoat)
            {
                entity.AddBuff<BoatBuff>();
            }
            entity.SetAnimationBool("HasBoat", _hasBoat);
            entity.SetAnimationBool("InWater", false);
        }

        protected override void UpdateLogic(Entity entity)
        {
            base.UpdateLogic(entity);

            entity.SetAnimationBool("InWater", false);
            bool currentBoatState = entity.HasBuff<BoatBuff>();
            if (currentBoatState != _hasBoat)
            {
                _hasBoat = currentBoatState;
                entity.SetAnimationBool("HasBoat", _hasBoat);
            }

            if (!entity.HasBuff<GhostBuff>())
            {
                entity.AddBuff<GhostBuff>();
            }

            if (entity.Timeout >= 0)
            {
                entity.Timeout--;
                if (entity.Timeout <= 0)
                {
                    entity.Die(entity);
                }
            }
        }

        public override void PostDeath(Entity entity, DeathInfo info)
        {
            if (entity.HasBuff<BoatBuff>())
            {
                entity.RemoveBuffs<BoatBuff>();
                // 掉落碎船掉落物
                var effect = entity.Level.Spawn(VanillaEffectID.brokenArmor, entity.GetCenter(), entity);
                effect.Velocity = new Vector3(effect.RNG.NextFloat() * 20 - 10, 5, 0);
                effect.ChangeModel(VanillaModelID.boatItem);
                effect.SetDisplayScale(entity.GetDisplayScale());
            }
            base.PostDeath(entity, info);
            if (info.Effects.HasEffect(VanillaDamageEffects.REMOVE_ON_DEATH))
                return;
            entity.Level.Spawn(VanillaEffectID.boneParticles, entity.GetCenter(), entity);
            entity.Remove();
        }
    }
}