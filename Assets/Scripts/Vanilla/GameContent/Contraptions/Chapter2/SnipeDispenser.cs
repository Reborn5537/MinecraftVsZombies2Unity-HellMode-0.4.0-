using MVZ2.Vanilla.Entities;
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
                return;
            }
            EvokedUpdate(entity);
            UpdateArrowTracking(entity); // 新增：更新所有活跃箭矢的追踪逻辑
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
                    direction = ApplyTrackingError(direction, 10f); // 应用误差
                    arrow.Velocity = direction * arrow.Velocity.magnitude;
                }
            }
        }

        private Entity FindClosestTarget(Entity arrow, Entity dispenser)
        {
            var level = arrow.Level;
            Vector3 centerPos = arrow.Position;

            var candidates = level.FindEntities(e =>
                (e.Type == EntityTypes.ENEMY ||
                 e.Type == EntityTypes.BOSS ||
                 e.Type == EntityTypes.OBSTACLE) &&
                e.IsHostile(dispenser.GetFaction())
            );

            if (candidates.Length == 0)
                return null;

            return candidates
                .OrderBy(e => Vector3.Distance(centerPos, e.Position))
                .FirstOrDefault();
        }

        private Vector3 ApplyTrackingError(Vector3 direction, float maxAngle)
        {
            float error = UnityEngine.Random.Range(-maxAngle, maxAngle);
            return Quaternion.Euler(0, error, 0) * direction;
        }

        protected override void OnEvoke(Entity entity)
        {
            base.OnEvoke(entity);
            var evocationTimer = GetEvocationTimer(entity);
            evocationTimer.Reset();
            entity.SetEvoked(true);
        }

        private void EvokedUpdate(Entity entity)
        {
            var evocationTimer = GetEvocationTimer(entity);
            evocationTimer.Run();
            if (evocationTimer.PassedInterval(2))
            {
                for (int i = 0; i < 2; i++)
                {
                    var projectile = Shoot(entity);
                    projectile.Velocity *= 2;
                    _activeArrows.Add(projectile);
                }
            }
            if (evocationTimer.Expired)
            {
                entity.SetEvoked(false);
                var shootTimer = GetShootTimer(entity);
                shootTimer.Reset();
            }
        }

        /*public override Entity Shoot(Entity entity)
        {
            Entity projectile = base.Shoot(entity);
            projectile.SetProperty("IsTracking", true);
            return projectile;
        }*/

        public static FrameTimer GetEvocationTimer(Entity entity) => entity.GetBehaviourField<FrameTimer>(ID, PROP_EVOCATION_TIMER);
        public static void SetEvocationTimer(Entity entity, FrameTimer timer) => entity.SetBehaviourField(ID, PROP_EVOCATION_TIMER, timer);
        private static readonly NamespaceID ID = VanillaContraptionID.snipedispenser;
        public static readonly VanillaEntityPropertyMeta PROP_EVOCATION_TIMER = new VanillaEntityPropertyMeta("EvocationTimer");
    }
}