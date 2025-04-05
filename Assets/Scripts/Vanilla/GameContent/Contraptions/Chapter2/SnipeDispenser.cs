using MVZ2.Vanilla.Entities;
using MVZ2.Vanilla.Properties;
using PVZEngine;
using PVZEngine.Entities;
using PVZEngine.Level;
using Tools;
using MVZ2.GameContent.Buffs.Armors;
using System.Collections.Generic;
using UnityEngine;

namespace MVZ2.GameContent.Contraptions.Chapter2
{
    [EntityBehaviourDefinition(VanillaContraptionNames.snipedispenser)]
    public class SnipeDispenser : DispenserFamily
    {
        private class ArrowTracker
        {
            public Entity Target;
            public float AngleOffset;
        }

        private static Dictionary<Entity, ArrowTracker> _trackingArrows = new Dictionary<Entity, ArrowTracker>();

        public SnipeDispenser(string nsp, string name) : base(nsp, name)
        {
        }

        public override void Init(Entity entity)
        {
            base.Init(entity);
            var evocationTimer = new FrameTimer(115);
            SetEvocationTimer(entity, evocationTimer);
        }

        protected override void UpdateAI(Entity entity)
        {
            base.UpdateAI(entity);
            var arrowsToRemove = new List<Entity>();
            foreach (var pair in _trackingArrows)
            {
                var arrow = pair.Key;
                var tracker = pair.Value;

                if (!arrow.ExistsAndAlive() || !tracker.Target.ExistsAndAlive())
                {
                    arrowsToRemove.Add(arrow);
                    continue;
                }

                UpdateArrowTracking(arrow, tracker);
            }

            foreach (var arrow in arrowsToRemove)
            {
                _trackingArrows.Remove(arrow);
            }

            if (entity.IsEvoked())
            {
                var timer = GetEvocationTimer(entity);
                timer.Run();
                if (timer.Expired)
                {
                    entity.SetEvoked(false);
                    GetShootTimer(entity).Reset();
                }
            }
        }

        public override Entity Shoot(Entity entity)
        {
            entity.TriggerAnimation("Shoot");
            var target = GetNearestEnemy(entity);
            return target != null ? FireTwinTrackingArrows(entity, target) : base.Shoot(entity);
        }

        private Entity FireTwinTrackingArrows(Entity entity, Entity target)
        {
            var arrow1 = entity.ShootProjectile();
            InitializeTrackingArrow(arrow1, target, -10f);

            var arrow2 = entity.ShootProjectile();
            InitializeTrackingArrow(arrow2, target, 10f);

            return arrow1;
        }

        private void InitializeTrackingArrow(Entity arrow, Entity target, float angleOffset)
        {
            Vector3 direction = (target.GetCenter() - arrow.GetCenter()).normalized;
            direction = Quaternion.Euler(0, angleOffset, 0) * direction;
            arrow.Velocity = direction * arrow.Velocity.magnitude;

            _trackingArrows[arrow] = new ArrowTracker
            {
                Target = target,
                AngleOffset = angleOffset
            };
        }

        private void UpdateArrowTracking(Entity arrow, ArrowTracker tracker)
        {
            const float maxAngle = 5f;
            Vector3 currentDir = arrow.Velocity.normalized;
            Vector3 targetDir = (tracker.Target.GetCenter() - arrow.GetCenter()).normalized;

            targetDir = Quaternion.Euler(0, tracker.AngleOffset, 0) * targetDir;

            float angle = Vector3.Angle(currentDir, targetDir);
            if (angle > maxAngle)
            {
                targetDir = Vector3.RotateTowards(currentDir, targetDir, maxAngle * Mathf.Deg2Rad, 0);
            }

            arrow.Velocity = targetDir * arrow.Velocity.magnitude;
        }

        private Entity GetNearestEnemy(Entity entity)
        {
            var detected = detector.Detect(entity);

            if (detected?.Entity != null &&
               (detected.Entity.Type == EntityTypes.ENEMY ||
                detected.Entity.Type == EntityTypes.BOSS))
            {
                return detected.Entity;
            }
            return null;
        }

        private static readonly NamespaceID ID = VanillaContraptionID.snipedispenser;
        public static readonly VanillaEntityPropertyMeta PROP_EVOCATION_TIMER =
            new VanillaEntityPropertyMeta("EvocationTimer");

        public static FrameTimer GetEvocationTimer(Entity entity) =>
            entity.GetBehaviourField<FrameTimer>(ID, PROP_EVOCATION_TIMER);

        public static void SetEvocationTimer(Entity entity, FrameTimer timer) =>
            entity.SetBehaviourField(ID, PROP_EVOCATION_TIMER, timer);
    }
}