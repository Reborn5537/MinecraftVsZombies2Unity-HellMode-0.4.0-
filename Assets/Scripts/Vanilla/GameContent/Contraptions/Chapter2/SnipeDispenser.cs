﻿using MVZ2.Vanilla.Entities;
using MVZ2.Vanilla.Properties;
using PVZEngine;
using PVZEngine.Entities;
using PVZEngine.Level;
using Tools;
using UnityEngine;
using System.Collections.Generic;
using MVZ2.GameContent.Buffs.Projectiles;
using MVZ2.Vanilla.Audios;
using System.Linq;

namespace MVZ2.GameContent.Contraptions
{
    [EntityBehaviourDefinition(VanillaContraptionNames.snipedispenser)]
    public class SnipeDispenser : DispenserFamily
    {
        private List<Entity> _activeArrows = new List<Entity>();

        public SnipeDispenser(string nsp, string name) : base(nsp, name)
        {
            detector.ignoreHighEnemy = false;
            detector.ignoreLowEnemy = false;
            detector.colliderFilter = (self, collider) =>
                collider.Entity.Type == EntityTypes.ENEMY ||
                collider.Entity.Type == EntityTypes.OBSTACLE;
        }

        public override void Init(Entity entity)
        {
            base.Init(entity);
            InitShootTimer(entity);
            SetEvocationTimer(entity, new FrameTimer(150));
        }

        protected override void UpdateAI(Entity entity)
        {
            base.UpdateAI(entity);
            if (!entity.IsEvoked())
            {
                ShootTick(entity);
            }
            else
            {
                EvokedUpdate(entity);
            }
            UpdateArrowTracking(entity);
        }

        private void UpdateArrowTracking(Entity dispenser)
        {
            for (int i = _activeArrows.Count - 1; i >= 0; i--)
            {
                Entity arrow = _activeArrows[i];
                if (arrow.IsDead || !arrow.Exists())
                {
                    _activeArrows.RemoveAt(i);
                    continue;
                }

                Entity target = FindClosestTarget(arrow, dispenser);
                if (target != null)
                {
                    Vector3 direction = (target.GetCenter() - arrow.Position).normalized;

                    float xDiff = arrow.Position.x - dispenser.Position.x;
                    float facingSign = Mathf.Sign(xDiff);

                    direction.x *= facingSign;

                    direction = ApplyTrackingError(direction, 10f);
                    arrow.Velocity = direction * arrow.Velocity.magnitude;
                }
            }
        }

        private Entity FindClosestTarget(Entity arrow, Entity dispenser)
        {
            var level = arrow.Level;
            return level.FindEntities(e =>
                (e.Type == EntityTypes.ENEMY ||
                 e.Type == EntityTypes.BOSS ||
                 e.Type == EntityTypes.OBSTACLE) &&
                !e.IsDead &&
                e.IsHostile(dispenser.GetFaction())
            ).OrderBy(e => Vector3.Distance(arrow.Position, e.Position))
             .FirstOrDefault();
            /*if (!dispenser.IsFriendlyEntity())
                return level.FindEntities(e =>
                (e.Type == EntityTypes.PLANT &&
                !e.IsDead )
            ).OrderBy(e => Vector3.Distance(arrow.Position, e.Position))
             .FirstOrDefault();*/
        }

        private Vector3 ApplyTrackingError(Vector3 direction, float maxAngle)
        {
            float error = Random.Range(-maxAngle, maxAngle);
            return Quaternion.Euler(0, error, 0) * direction;
        }

        protected override void OnEvoke(Entity entity)
        {
            base.OnEvoke(entity);
            GetEvocationTimer(entity).Reset();
            entity.SetEvoked(true);
        }

        private void EvokedUpdate(Entity entity)
        {
            var evocationTimer = GetEvocationTimer(entity);
            evocationTimer.Run();

            if (evocationTimer.PassedInterval(2))
            {
                Entity projectile = base.Shoot(entity);
                projectile.Velocity *= 2;
                projectile.SetProperty(PROP_TRACKING_MARKER, true);
                _activeArrows.Add(projectile);
            }

            if (evocationTimer.Expired)
            {
                entity.SetEvoked(false);
                GetShootTimer(entity).Reset();
            }
        }

        public override Entity Shoot(Entity entity)
        {
            Entity projectile = base.Shoot(entity);
            if (!_activeArrows.Contains(projectile))
            {
                _activeArrows.Add(projectile);
            }
            return projectile;
        }

        public new void ShootTick(Entity entity)
        {
            var shootTimer = GetShootTimer(entity);
            shootTimer.Run(entity.GetAttackSpeed());
            if (shootTimer.Expired)
            {
                var level = entity.Level;
                bool hasValidTarget = level.FindEntities(e =>
            (e.Type == EntityTypes.ENEMY ||
             e.Type == EntityTypes.OBSTACLE) &&
            !e.IsDead // 确保实体未死亡
        ).Any(); // 检查是否存在至少一个有效目标

                if (hasValidTarget)
                {
                    OnShootTick(entity);
                }
                shootTimer.ResetTime(GetTimerTime(entity));
            }
        }

        public override void OnShootTick(Entity entity)
        {
            base.OnShootTick(entity);
        }

        private bool IsValidTargetType(Entity target) =>
            target.Type == EntityTypes.ENEMY || target.Type == EntityTypes.OBSTACLE;

        private static readonly VanillaEntityPropertyMeta<bool> PROP_TRACKING_MARKER =
            new VanillaEntityPropertyMeta<bool>("IsTrackingArrow");

        public static FrameTimer GetEvocationTimer(Entity entity) =>
            entity.GetBehaviourField<FrameTimer>(ID, PROP_EVOCATION_TIMER);

        public static void SetEvocationTimer(Entity entity, FrameTimer timer) =>
            entity.SetBehaviourField(ID, PROP_EVOCATION_TIMER, timer);

        private static readonly NamespaceID ID = VanillaContraptionID.snipedispenser;
        public static readonly VanillaEntityPropertyMeta<FrameTimer> PROP_EVOCATION_TIMER =
            new VanillaEntityPropertyMeta<FrameTimer>("EvocationTimer");
    }
}