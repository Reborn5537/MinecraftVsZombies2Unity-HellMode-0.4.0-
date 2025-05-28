using System.Collections.Generic;
using MVZ2.GameContent.Buffs.Enemies;
using MVZ2.GameContent.Damages;
using MVZ2.GameContent.Effects;
using MVZ2.GameContent.Projectiles;
using MVZ2.Vanilla.Audios;
using MVZ2.Vanilla.Entities;
using MVZ2.Vanilla.Level;
using MVZ2Logic.Level;
using PVZEngine.Damages;
using PVZEngine.Entities;
using UnityEngine;
using System.Linq;
using PVZEngine.Buffs;
using PVZEngine.Level;
using Tools;
using MVZ2.GameContent.Armors;
using MVZ2.GameContent.Detections;
using MVZ2.Vanilla.Contraptions;
using MVZ2.Vanilla.Detections;
using MVZ2.Vanilla.Enemies;
using MVZ2.Vanilla.Properties;
using PVZEngine;

namespace MVZ2.GameContent.Bosses
{
    public partial class Seija
    {
        #region 状态机
        private class SeijaStateMachine : EntityStateMachine
        {
            public SeijaStateMachine()
            {
                AddState(new AppearState());
                AddState(new IdleState());
                AddState(new BackflipState());
                AddState(new FrontflipState());
                AddState(new DanmakuState());
                AddState(new HammerState());
                AddState(new GapBombState());
                AddState(new CameraState());
                AddState(new FabricState());
                AddState(new FaintState());
                AddState(new ReverseDanceState());
            }
        }
        #endregion

        #region 状态
        private class AppearState : EntityStateMachineState
        {
            public AppearState() : base(STATE_APPEAR) { }
            public override void OnEnter(EntityStateMachine stateMachine, Entity entity)
            {
                base.OnEnter(stateMachine, entity);
                var substateTimer = stateMachine.GetSubStateTimer(entity);
                substateTimer.ResetTime(30);
            }
            public override void OnUpdateAI(EntityStateMachine stateMachine, Entity entity)
            {
                base.OnUpdateAI(stateMachine, entity);
                var substateTimer = stateMachine.GetSubStateTimer(entity);
                substateTimer.Run(stateMachine.GetSpeed(entity));
                if (!substateTimer.Expired)
                    return;
                stateMachine.StartState(entity, STATE_IDLE);
            }
        }
        private class IdleState : EntityStateMachineState
        {
            public IdleState() : base(STATE_IDLE) { }
            public override void OnEnter(EntityStateMachine stateMachine, Entity entity)
            {
                base.OnEnter(stateMachine, entity);
                var stateTimer = stateMachine.GetStateTimer(entity);
                stateTimer.ResetTime(90);
            }
            public override void OnUpdateAI(EntityStateMachine stateMachine, Entity entity)
            {
                base.OnUpdateAI(stateMachine, entity);
                if (entity.IsOnGround)
                {
                    var pos = entity.Position;
                    var lane = Mathf.Clamp(entity.GetLane(), 0, entity.Level.GetMaxLaneCount() - 1);
                    var targetZ = entity.Level.GetEntityLaneZ(lane);
                    if (Mathf.Abs(targetZ - pos.z) > ADJUST_Z_THRESOLD)
                    {
                        pos.z = pos.z * 0.5f + targetZ * 0.5f;
                        entity.Position = pos;
                    }
                }

                var stateTimer = stateMachine.GetStateTimer(entity);
                stateTimer.Run(stateMachine.GetSpeed(entity));
                if (!stateTimer.Expired)
                    return;
                var nextState = GetNextState(stateMachine, entity);
                stateMachine.StartState(entity, nextState);
                stateMachine.SetPreviousState(entity, nextState);
            }
            private int GetNextState(EntityStateMachine stateMachine, Entity entity)
            {
                var lastState = stateMachine.GetPreviousState(entity);
                if (lastState == STATE_IDLE || lastState == STATE_BACKFLIP)
                {
                    lastState = STATE_DANMAKU;
                    return lastState;
                }

                bool attackAttempted = false;
                if (lastState == STATE_DANMAKU)
                {
                    lastState = STATE_CAMERA;
                    attackAttempted = true;
                    if (ShouldCamera(entity))
                    {
                        return lastState;
                    }
                }
                if (lastState == STATE_CAMERA)
                {
                    lastState = STATE_HAMMER;
                    attackAttempted = true;
                    entity.Target = FindHammerTarget(entity);
                    if (entity.Target.ExistsAndAlive())
                    {
                        return lastState;
                    }
                }
                if (lastState == STATE_HAMMER)
                {
                    lastState = STATE_GAP_BOMB;
                    if (ShouldGapBomb(entity))
                    {
                        return lastState;
                    }
                }
                if (lastState == STATE_GAP_BOMB)
                {
                    lastState = STATE_FRONTFLIP;
                    if (attackAttempted && ShouldFrontFlip(entity) && CanFrontflip(entity))
                    {
                        return lastState;
                    }
                }
                if (lastState == STATE_FRONTFLIP)
                {
                    lastState = STATE_BACKFLIP;
                    if (attackAttempted && ShouldBackflip(entity) && CanBackflip(entity))
                    {
                        return lastState;
                    }
                }
                if ((lastState == STATE_FRONTFLIP || lastState == STATE_BACKFLIP) &&
        ShouldReverseDance(entity))
                {
                    return STATE_REVERSE_DANCE;
                }

                return STATE_DANMAKU;
            }
        }
        private class DanmakuState : EntityStateMachineState
        {
            public DanmakuState() : base(STATE_DANMAKU)
            {
            }
            public override void OnEnter(EntityStateMachine stateMachine, Entity entity)
            {
                base.OnEnter(stateMachine, entity);
                var substateTimer = stateMachine.GetSubStateTimer(entity);
                substateTimer.ResetTime(30);
            }
            public override void OnUpdateAI(EntityStateMachine stateMachine, Entity entity)
            {
                base.OnUpdateAI(stateMachine, entity);
                var danmakuTimer = GetDanmakuTimer(entity);
                danmakuTimer.Run(stateMachine.GetSpeed(entity));
                if (danmakuTimer.Expired)
                {
                    danmakuTimer.Reset();
                    var substate = stateMachine.GetSubState(entity);
                    var bulletAngle = GetBulletAngle(entity);
                    Color color = Color.red;
                    switch (substate)
                    {
                        case SUBSTATE_ROTATE_1:
                        case SUBSTATE_ROTATE_3:
                            bulletAngle = (bulletAngle + 5) % 360;
                            break;
                        case SUBSTATE_ROTATE_2:
                            color = Color.blue;
                            bulletAngle = (bulletAngle - 5) % 360;
                            break;
                    }
                    for (int i = 0; i < 6; i++)
                    {
                        var angle = bulletAngle + i * 60;
                        var param = entity.GetShootParams();
                        param.projectileID = VanillaProjectileID.seijaBullet;
                        param.position = entity.GetCenter();
                        param.damage = entity.GetDamage() * 0.6f;
                        param.velocity = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0, Mathf.Sin(angle * Mathf.Deg2Rad)) * SeijaBullet.LIGHT_SPEED;
                        var bullet = entity.ShootProjectile(param);
                        bullet.SetHSVToColor(color);
                    }
                    SetBulletAngle(entity, bulletAngle);
                    entity.PlaySound(VanillaSoundID.danmaku, volume: 0.5f);
                }
                RunTimer(stateMachine, entity);
            }
            private void RunTimer(EntityStateMachine stateMachine, Entity entity)
            {
                var substate = stateMachine.GetSubState(entity);
                var substateTimer = stateMachine.GetSubStateTimer(entity);
                substateTimer.Run(stateMachine.GetSpeed(entity));

                switch (substate)
                {
                    case SUBSTATE_ROTATE_1:
                        if (substateTimer.Expired)
                        {
                            stateMachine.SetSubState(entity, SUBSTATE_ROTATE_2);
                            substateTimer.ResetTime(30);
                        }
                        break;
                    case SUBSTATE_ROTATE_2:
                        if (substateTimer.Expired)
                        {
                            stateMachine.StartState(entity, STATE_IDLE);
                        }
                        break;
                }
            }
            public const int SUBSTATE_ROTATE_1 = 0;
            public const int SUBSTATE_ROTATE_2 = 1;
            public const int SUBSTATE_ROTATE_3 = 2;
        }
        private class HammerState : EntityStateMachineState
        {
            public HammerState() : base(STATE_HAMMER) { }

            public override void OnEnter(EntityStateMachine stateMachine, Entity entity)
            {
                base.OnEnter(stateMachine, entity);
                var substateTimer = stateMachine.GetSubStateTimer(entity);
                substateTimer.ResetTime(17);

                if (entity.Target.ExistsAndAlive())
                {
                    entity.Velocity = (entity.Target.Position - entity.Position) / 6.6667f;
                }
            }
            public override void OnUpdateAI(EntityStateMachine stateMachine, Entity entity)
            {
                base.OnUpdateAI(stateMachine, entity);
                var substate = stateMachine.GetSubState(entity);
                var substateTimer = stateMachine.GetSubStateTimer(entity);
                substateTimer.Run(stateMachine.GetSpeed(entity));

                switch (substate)
                {
                    case SUBSTATE_RAISE:
                        if (substateTimer.Expired)
                        {
                            stateMachine.SetSubState(entity, SUBSTATE_HAMMERED);
                            substateTimer.ResetTime(8);
                            smashDetectBuffer.Clear();
                            hammerSmashDetector.DetectMultiple(entity, smashDetectBuffer);
                            if (smashDetectBuffer.Count > 0)
                            {
                                entity.Level.ShakeScreen(10, 0, 15);
                                entity.PlaySound(VanillaSoundID.fling);
                            }

                            foreach (var collider in smashDetectBuffer)
                            {
                                var target = collider.Entity;
                                var damageResult = collider.TakeDamage(target.GetTakenCrushDamage(), new DamageEffectList(VanillaDamageEffects.PUNCH, VanillaDamageEffects.DAMAGE_BODY_AFTER_ARMOR_BROKEN), entity);
                                if (damageResult != null && damageResult.BodyResult != null && damageResult.BodyResult.Fatal && damageResult.BodyResult.Entity.Type == EntityTypes.PLANT)
                                {
                                    damageResult.Entity.PlaySound(VanillaSoundID.smash);
                                }
                            }
                        }
                        break;
                    case SUBSTATE_HAMMERED:
                        if (substateTimer.Expired)
                        {
                            int count = hammerPlaceBombDetector.DetectEntityCount(entity);
                            if (count >= BACKFLIP_ENEMY_COUNT && CanBackflip(entity))
                            {
                                stateMachine.StartState(entity, STATE_BACKFLIP);
                                var param = entity.GetSpawnParams();
                                param.SetProperty(VanillaEntityProps.DAMAGE, entity.GetDamage());
                                var bomb = entity.Spawn(VanillaProjectileID.seijaMagicBomb, entity.GetCenter(), param);
                                bomb.Velocity = new Vector3(0, 5, 0);
                            }
                            else
                            {
                                stateMachine.StartState(entity, STATE_IDLE);
                            }
                        }
                        break;
                }
            }

            private List<IEntityCollider> smashDetectBuffer = new List<IEntityCollider>();
            public const int SUBSTATE_RAISE = 0;
            public const int SUBSTATE_HAMMERED = 1;
        }
        private class GapBombState : EntityStateMachineState
        {
            public GapBombState() : base(STATE_GAP_BOMB) { }
            public override void OnEnter(EntityStateMachine stateMachine, Entity entity)
            {
                base.OnEnter(stateMachine, entity);
                var substateTimer = stateMachine.GetSubStateTimer(entity);
                substateTimer.ResetTime(40);

                if (!entity.HasBuff<SeijaGapBuff>())
                {
                    entity.AddBuff<SeijaGapBuff>();
                }
                entity.PlaySound(VanillaSoundID.gapWarp);
            }
            public override void OnExit(EntityStateMachine machine, Entity entity)
            {
                base.OnExit(machine, entity);
                entity.RemoveBuffs<SeijaGapBuff>();
            }
            public override void OnUpdateAI(EntityStateMachine stateMachine, Entity entity)
            {
                base.OnUpdateAI(stateMachine, entity);
                var substateTimer = stateMachine.GetSubStateTimer(entity);
                substateTimer.Run(stateMachine.GetSpeed(entity));
                var substate = stateMachine.GetSubState(entity);

                switch (substate)
                {
                    case SUBSTATE_PREPARE:
                        if (substateTimer.Expired)
                        {
                            stateMachine.SetSubState(entity, SUBSTATE_WRAPPED);
                            substateTimer.ResetTime(12);
                            var level = entity.Level;
                            var pos = entity.Position;
                            pos.x = entity.IsFacingLeft() ? VanillaLevelExt.LEFT_BORDER + 40 : VanillaLevelExt.RIGHT_BORDER - 40;
                            pos.y = entity.Level.GetGroundY(pos.x, pos.z);
                            entity.Position = pos;
                            entity.PlaySound(VanillaSoundID.gapWarp);
                        }
                        break;

                    case SUBSTATE_WRAPPED:
                        if (substateTimer.Expired)
                        {
                            stateMachine.SetSubState(entity, SUBSTATE_BOMB_THROWN);
                            substateTimer.ResetTime(30);

                            var pos = entity.Position;
                            pos.y += 40;
                            var param = entity.GetSpawnParams();
                            param.SetProperty(VanillaEntityProps.DAMAGE, entity.GetDamage());
                            var bomb = entity.Spawn(VanillaProjectileID.seijaMagicBomb, pos, param);
                            bomb.Velocity = new Vector3(entity.GetFacingX() * -5, 10, 0);
                            entity.PlaySound(VanillaSoundID.fling);
                        }
                        break;

                    case SUBSTATE_BOMB_THROWN:
                        if (substateTimer.Expired)
                        {
                            var level = entity.Level;
                            stateMachine.SetSubState(entity, SUBSTATE_RETURN);
                            substateTimer.ResetTime(21);
                            var pos = entity.Position;
                            pos.x = level.GetEntityColumnX(entity.IsFacingLeft() ? level.GetMaxColumnCount() - 1 : 0);
                            var lane = entity.RNG.Next(level.GetMaxLaneCount());
                            pos.z = level.GetEntityLaneZ(lane);
                            pos.y = level.GetGroundY(pos.x, pos.z);
                            entity.Position = pos;
                            entity.PlaySound(VanillaSoundID.gapWarp);
                        }
                        break;

                    case SUBSTATE_RETURN:
                        if (entity.IsOnGround)
                        {
                            stateMachine.SetSubState(entity, SUBSTATE_LANDED);
                            substateTimer.ResetTime(15);
                        }
                        break;

                    case SUBSTATE_LANDED:
                        if (substateTimer.Expired)
                        {
                            stateMachine.StartState(entity, STATE_IDLE);
                        }
                        break;
                }
            }
            public const int SUBSTATE_PREPARE = 0;
            public const int SUBSTATE_WRAPPED = 1;
            public const int SUBSTATE_BOMB_THROWN = 2;
            public const int SUBSTATE_RETURN = 3;
            public const int SUBSTATE_LANDED = 4;
        }
        private class CameraState : EntityStateMachineState
        {
            public CameraState() : base(STATE_CAMERA) { }
            public override void OnEnter(EntityStateMachine stateMachine, Entity entity)
            {
                base.OnEnter(stateMachine, entity);
                var substateTimer = stateMachine.GetSubStateTimer(entity);
                substateTimer.ResetTime(12);
            }
            public override void OnUpdateAI(EntityStateMachine stateMachine, Entity entity)
            {
                base.OnUpdateAI(stateMachine, entity);
                var substateTimer = stateMachine.GetSubStateTimer(entity);
                substateTimer.Run(stateMachine.GetSpeed(entity));
                var substate = stateMachine.GetSubState(entity);
                switch (substate)
                {
                    case SUBSTATE_PREPARE:
                        if (substateTimer.Expired)
                        {
                            stateMachine.SetSubState(entity, SUBSTATE_JUMP);
                            var pos = entity.Position;
                            pos.x += entity.GetFacingX() * 80;
                            pos.y = entity.Level.GetGroundY(pos);
                            var frame = entity.SpawnWithParams(VanillaEffectID.seijaCameraFrame, pos);
                            frame.Velocity = new Vector3(entity.GetFacingX() * 30, 0, 0);
                            entity.Velocity = new Vector3(entity.GetFacingX() * 10, 10, GetChangeAdjacentLaneZSpeed(entity));
                        }
                        break;

                    case SUBSTATE_JUMP:
                        if (entity.IsOnGround)
                        {
                            if (CanBackflip(entity))
                            {
                                stateMachine.StartState(entity, STATE_BACKFLIP);
                            }
                            else
                            {
                                stateMachine.SetSubState(entity, SUBSTATE_LANDED);
                                substateTimer.ResetTime(17);
                            }
                        }
                        break;

                    case SUBSTATE_LANDED:
                        if (substateTimer.Expired)
                        {
                            stateMachine.StartState(entity, STATE_IDLE);
                        }
                        break;
                }
            }
            public const int SUBSTATE_PREPARE = 0;
            public const int SUBSTATE_JUMP = 1;
            public const int SUBSTATE_LANDED = 2;
        }
        private class BackflipState : EntityStateMachineState
        {
            public BackflipState() : base(STATE_BACKFLIP) { }

            private static readonly int ORB_COUNT = 5;
            private static readonly float ORB_SPREAD_ANGLE = 30f;

            public override void OnEnter(EntityStateMachine machine, Entity entity)
            {
                base.OnEnter(machine, entity);
                entity.Velocity = new Vector3(-10 * entity.GetFacingX(), 10, GetChangeAdjacentLaneZSpeed(entity));

                FireControlOrbs(entity);
            }

            private void FireControlOrbs(Entity entity)
            {
                var level = entity.Level;
                var centerPos = entity.GetCenter();

                var candidates = level.FindEntities(e =>
                    e.Type == EntityTypes.PLANT &&
                    !e.IsFloor() &&
                    CompellingOrb.CanControl(e));

                if (candidates.Length == 0) return;

                var sortedTargets = candidates
                    .OrderBy(e => Vector3.Distance(centerPos, e.Position))
                    .Take(ORB_COUNT)
                    .ToArray();

                for (int i = 0; i < Mathf.Min(ORB_COUNT, sortedTargets.Length); i++)
                {
                    var target = sortedTargets[i];
                    var param = entity.GetShootParams();
                    param.damage = 0;
                    param.projectileID = VanillaProjectileID.compellingOrb;

                    float angle = (i - 1) * ORB_SPREAD_ANGLE;
                    Vector3 direction = (target.Position - centerPos).normalized;
                    direction = Quaternion.Euler(0, angle, 0) * direction;

                    param.position = centerPos;
                    param.velocity = direction * 5f;

                    var orb = entity.ShootProjectile(param);
                    orb.Target = target;
                    orb.SetParent(entity);
                }
            }

            public override void OnUpdateAI(EntityStateMachine stateMachine, Entity entity)
            {
                base.OnUpdateAI(stateMachine, entity);
                var substateTimer = stateMachine.GetSubStateTimer(entity);
                substateTimer.Run(stateMachine.GetSpeed(entity));
                var substate = stateMachine.GetSubState(entity);

                switch (substate)
                {
                    case SUBSTATE_JUMP:
                        if (entity.IsOnGround)
                        {
                            stateMachine.SetSubState(entity, SUBSTATE_LANDED);
                            substateTimer.ResetTime(15);
                        }
                        break;

                    case SUBSTATE_LANDED:
                        if (substateTimer.Expired)
                        {
                            stateMachine.StartState(entity, STATE_IDLE);
                        }
                        break;
                }
            }

            public const int SUBSTATE_JUMP = 0;
            public const int SUBSTATE_LANDED = 1;
        }
        private class FrontflipState : EntityStateMachineState
        {
            public FrontflipState() : base(STATE_FRONTFLIP) { }
            public override void OnEnter(EntityStateMachine machine, Entity entity)
            {
                base.OnEnter(machine, entity);
                entity.Velocity = new Vector3(10 * entity.GetFacingX(), 10, GetChangeAdjacentLaneZSpeed(entity));
            }
            public override void OnUpdateAI(EntityStateMachine stateMachine, Entity entity)
            {
                base.OnUpdateAI(stateMachine, entity);
                var substateTimer = stateMachine.GetSubStateTimer(entity);
                substateTimer.Run(stateMachine.GetSpeed(entity));
                var substate = stateMachine.GetSubState(entity);
                switch (substate)
                {
                    case SUBSTATE_JUMP:
                        if (entity.IsOnGround)
                        {
                            stateMachine.SetSubState(entity, SUBSTATE_LANDED);
                            substateTimer.ResetTime(15);
                        }
                        break;

                    case SUBSTATE_LANDED:
                        if (substateTimer.Expired)
                        {
                            stateMachine.StartState(entity, STATE_IDLE);
                        }
                        break;
                }
            }
            public const int SUBSTATE_JUMP = 0;
            public const int SUBSTATE_LANDED = 1;

        }
        private class FabricState : EntityStateMachineState
        {
            public FabricState() : base(STATE_FABRIC) { }
            public override void OnEnter(EntityStateMachine stateMachine, Entity entity)
            {
                base.OnEnter(stateMachine, entity);
                var substateTimer = stateMachine.GetSubStateTimer(entity);
                substateTimer.ResetTime(30);
                if (!entity.HasBuff<SeijaFabricBuff>())
                {
                    entity.AddBuff<SeijaFabricBuff>();
                }
                entity.Velocity = Vector3.zero;
            }
            public override void OnExit(EntityStateMachine machine, Entity entity)
            {
                base.OnExit(machine, entity);
                entity.RemoveBuffs<SeijaFabricBuff>();
                entity.Velocity = Vector3.zero;
            }
            public override void OnUpdateAI(EntityStateMachine stateMachine, Entity entity)
            {
                base.OnUpdateAI(stateMachine, entity);
                var substateTimer = stateMachine.GetSubStateTimer(entity);
                substateTimer.Run(stateMachine.GetSpeed(entity));
                var substate = stateMachine.GetSubState(entity);
                switch (substate)
                {
                    case SUBSTATE_FABRICED:
                        if (substateTimer.Expired)
                        {
                            stateMachine.SetSubState(entity, SUBSTATE_OFF);
                            substateTimer.ResetTime(10);
                        }
                        break;

                    case SUBSTATE_OFF:
                        if (substateTimer.Expired)
                        {
                            if (CanBackflip(entity))
                            {
                                stateMachine.StartState(entity, STATE_BACKFLIP);
                            }
                            else
                            {
                                stateMachine.StartState(entity, STATE_IDLE);
                            }
                        }
                        break;
                }
            }
            public const int SUBSTATE_FABRICED = 0;
            public const int SUBSTATE_OFF = 1;

        }
        private class FaintState : EntityStateMachineState
        {
            public FaintState() : base(STATE_FAINT) { }
            public override void OnEnter(EntityStateMachine stateMachine, Entity entity)
            {
                base.OnEnter(stateMachine, entity);
            }
            public override void OnUpdateLogic(EntityStateMachine stateMachine, Entity entity)
            {
                base.OnUpdateLogic(stateMachine, entity);
            }
        }
        private class ReverseDanceState : EntityStateMachineState
        {
            private int jumpsRemaining;
            private FrameTimer jumpTimer;
            private List<Entity> allPlants = new List<Entity>();
            private List<Vector3> originalPositions = new List<Vector3>();
            private int swapCount = 0;
            private const int TOTAL_SWAPS = 5;

            public ReverseDanceState() : base(STATE_REVERSE_DANCE)
            {
                jumpTimer = new FrameTimer(30);
            }

            public override void OnEnter(EntityStateMachine stateMachine, Entity entity)
            {
                base.OnEnter(stateMachine, entity);
                jumpsRemaining = 5;
                swapCount = 0;
                jumpTimer.Reset();
                allPlants.Clear();
                originalPositions.Clear();

                allPlants = entity.Level.FindEntities(e =>
                    e.Type == EntityTypes.PLANT && !e.IsFloor())
                    .ToList();

                originalPositions = allPlants.Select(p => p.Position).ToList();
                entity.PlaySound(VanillaSoundID.gapWarp);
            }

            public override void OnUpdateAI(EntityStateMachine stateMachine, Entity entity)
            {
                base.OnUpdateAI(stateMachine, entity);

                jumpTimer.Run();
                if (jumpTimer.Expired && jumpsRemaining > 0)
                {
                    PerformDanceJump(entity);
                    jumpTimer.Reset();
                    jumpsRemaining--;
                }

                if (jumpsRemaining <= 0 && entity.IsOnGround)
                {
                    stateMachine.StartState(entity, STATE_IDLE);
                }
            }

            private void PerformDanceJump(Entity entity)
            {
                float xDir = jumpsRemaining % 2 == 0 ? 1 : -1;
                int currentLane = entity.GetLane();
                int maxLane = entity.Level.GetMaxLaneCount() - 1;
                int laneChange = entity.RNG.Next(-1, 2);
                int targetLane = Mathf.Clamp(currentLane + laneChange, 0, maxLane);

                float jumpHeight = entity.RNG.Next(8, 13);
                float xSpeed = entity.RNG.Next(8, 13) * xDir * entity.GetFacingX();

                entity.Velocity = new Vector3(
                    xSpeed,
                    jumpHeight,
                    GetChangeLaneZSpeed(entity, targetLane)
                );

                if (swapCount < TOTAL_SWAPS)
                {
                    SwapAllTowers(entity);
                    swapCount++;
                }
            }

            private void SwapAllTowers(Entity entity)
            {
                if (allPlants.Count <= 1) return;

                for (int i = 0; i < allPlants.Count; i++)
                {
                    int swapIndex = (i + 1) % allPlants.Count;
                    allPlants[i].Position = originalPositions[swapIndex];

                    if (i % 3 == 0) 
                    {
                        entity.Spawn(VanillaEffectID.magicBombExplosion, originalPositions[swapIndex]);
                    }
                }

                originalPositions = allPlants.Select(p => p.Position).ToList();

                entity.PlaySound(VanillaSoundID.fling, volume: 0.8f);
            }
        }
        #endregion

    }
}
