using System.Linq;
using MVZ2.GameContent.Buffs.Contraptions;
using MVZ2.GameContent.Damages;
using MVZ2.Vanilla.Audios;
using MVZ2.Vanilla.Entities;
using MVZ2Logic.Level;
using PVZEngine.Damages;
using PVZEngine.Entities;
using PVZEngine.Level;
using UnityEngine;
using System.Collections.Generic;
using MVZ2.GameContent.Areas;
using MVZ2.GameContent.Artifacts;
using MVZ2.GameContent.Buffs;
using MVZ2.GameContent.Detections;
using MVZ2.GameContent.Pickups;
using MVZ2.Vanilla;
using MVZ2.Vanilla.Detections;
using MVZ2Logic;
using MVZ2Logic.Models;
using PVZEngine.Auras;
using PVZEngine.Buffs;

namespace MVZ2.GameContent.Contraptions
{
    [EntityBehaviourDefinition(VanillaContraptionNames.anvil)]
    public class Anvil : ContraptionBehaviour
    {
        public Anvil(string nsp, string name) : base(nsp, name)
        {
        }
        public override void Init(Entity entity)
        {
            base.Init(entity);
            entity.CollisionMaskFriendly |= EntityCollisionHelper.MASK_PLANT | EntityCollisionHelper.MASK_ENEMY | EntityCollisionHelper.MASK_OBSTACLE | EntityCollisionHelper.MASK_BOSS;
            entity.CollisionMaskHostile |= EntityCollisionHelper.MASK_PLANT | EntityCollisionHelper.MASK_ENEMY | EntityCollisionHelper.MASK_OBSTACLE | EntityCollisionHelper.MASK_BOSS;

            var pos = entity.Position + Vector3.up * 600;
            var level = entity.Level;
            if (level.AreaID == VanillaAreaID.castle && !Global.Game.IsUnlocked(VanillaUnlockID.brokenLantern))
            {
                if (!level.EntityExists(e => e.IsEntityOf(VanillaPickupID.artifactPickup) && ArtifactPickup.GetArtifactID(e) == VanillaArtifactID.brokenLantern))
                {
                    var lantern = level.Spawn(VanillaPickupID.artifactPickup, pos + Vector3.up * 100, entity);
                    ArtifactPickup.SetArtifactID(lantern, VanillaArtifactID.brokenLantern);
                }
            }
            entity.Position = pos;
            entity.SetFactionAndDirection(entity.GetFaction());
        }
        protected override void UpdateLogic(Entity contraption)
        {
            base.UpdateLogic(contraption);
            contraption.SetAnimationInt("HealthState", contraption.GetHealthState(3));
        }
        public override void PostCollision(EntityCollision collision, int state)
        {
            base.PostCollision(collision, state);
            if (state != EntityCollisionHelper.STATE_ENTER)
                return;
            if (!collision.Collider.IsMain())
                return;
            var anvil = collision.Entity;
            if (anvil.Velocity == Vector3.zero)
                return;
            var other = collision.Other;
            if (!CanSmash(anvil, other))
                return;
            float damageModifier = Mathf.Clamp(anvil.Velocity.magnitude, 0, 1);
            collision.OtherCollider.TakeDamage(1800 * damageModifier, new DamageEffectList(VanillaDamageEffects.PUNCH, VanillaDamageEffects.MUTE, VanillaDamageEffects.DAMAGE_BOTH_ARMOR_AND_BODY), anvil);
        }
        public override void PostContactGround(Entity anvil, Vector3 velocity)
        {
            base.PostContactGround(anvil, velocity);
            anvil.PlaySound(VanillaSoundID.anvil);

            var grid = anvil.GetGrid();
            if (grid == null)
                return;
            var selfGridLayers = anvil.GetGridLayersToTake();
            foreach (var layer in selfGridLayers)
            {
                var ent = grid.GetLayerEntity(layer);
                if (CanSmash(anvil, ent))
                {
                    ent.Die(new DamageEffectList(VanillaDamageEffects.PUNCH, VanillaDamageEffects.SELF_DAMAGE), anvil, null);
                }
            }
        }

        protected override void OnEvoke(Entity contraption)
        {
            base.OnEvoke(contraption);
            contraption.Health = contraption.GetMaxHealth();
            contraption.PlaySound(VanillaSoundID.sparkle);

            // 获取场上所有存活的植物
            var plants = contraption.Level.GetEntities()
                .Where(e => e.Type == EntityTypes.PLANT && !e.IsDead)
                .ToList();

            foreach (var plant in plants)
            {
                /*
                var crystalBuff = plant.AddBuff<DreamCrystalEvocationBuff>();
                if (crystalBuff != null)
                {
                    crystalBuff.SetProperty(DreamCrystalEvocationBuff.PROP_TIMEOUT,
                                          DreamCrystalEvocationBuff.MAX_TIMEOUT);
                }*/

                // DreamButterflyShieldBuff
                var shieldBuff = plant.AddBuff<DreamButterflyShieldBuff>();
                if (shieldBuff != null)
                {
                    shieldBuff.SetProperty(DreamButterflyShieldBuff.PROP_TIMEOUT,
                                          DreamButterflyShieldBuff.MAX_TIMEOUT);
                }
            }
        }

        public static bool CanSmash(Entity anvil, Entity other)
        {
            if (anvil == null || other == null)
                return false;
            if (other == anvil)
                return false;
            if (!other.IsVulnerableEntity())
                return false;
            if (anvil.IsHostile(other))
                return true;
            var selfGridLayers = anvil.GetGridLayersToTake();
            var otherGridLayers = other.GetGridLayersToTake();
            if (selfGridLayers == null || otherGridLayers == null)
                return false;
            return selfGridLayers.Any(l => otherGridLayers.Contains(l));
        }
    }
}