using MVZ2.GameContent.Bosses;
using MVZ2.GameContent.Enemies;
using MVZ2.GameContent.Buffs.Enemies;
using MVZ2.GameContent.Buffs.Level;
using MVZ2.GameContent.Pickups;
using MVZ2.GameContent.Effects;
using MVZ2.Vanilla.Entities;
using MVZ2.Vanilla.Level;
using MVZ2Logic.Level;
using PVZEngine.Definitions;
using PVZEngine.Entities;
using PVZEngine.Level;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using PVZEngine.Armors;
using PVZEngine.Auras;
using PVZEngine.Buffs;
using PVZEngine.Callbacks;
using PVZEngine.Damages;
using PVZEngine.Grids;
using PVZEngine.Models;
using PVZEngine.Modifiers;
using PVZEngine.Triggers;
using Tools;
using MVZ2.GameContent.Buffs.Contraptions;
using MVZ2.Vanilla.Audios;

namespace MVZ2.GameContent.Stages
{
    public class RebornmareStageBehaviour : BossStageBehaviour
    {
        public RebornmareStageBehaviour(StageDefinition stageDef) : base(stageDef)
        {
        }

        protected override void AfterFinalWaveUpdate(LevelEngine level)
        {
            base.AfterFinalWaveUpdate(level);
            RebornSlendermanTransitionUpdate(level);
        }
        protected override void BossFightWaveUpdate(LevelEngine level)
        {
            base.BossFightWaveUpdate(level);
            var state = GetBossState(level);
            switch (state)
            {
                case BOSS_STATE_SLENDERMAN:
                    RebornSlendermanUpdate(level);
                    break;
                case BOSS_STATE_SLENDERMAN_RAGE:
                    RebornSlendermanRageUpdate(level);
                    break;
                case BOSS_STATE_NIGHTMAREAPER_TRANSITION:
                    RebornNightmareaperTransitionUpdate(level);
                    break;
                case BOSS_STATE_NIGHTMAREAPER:
                    RebornNightmareaperUpdate(level);
                    break;
            }
        }
        private void RebornSlendermanTransitionUpdate(LevelEngine level)
        {
            if (level.EntityExists(e => e.Type == EntityTypes.BOSS && e.IsHostileEntity() && !e.IsDead))
            {
                // 瘦长鬼影出现
                level.WaveState = VanillaLevelStates.STATE_BOSS_FIGHT;
                return;
            }
            if (!level.HasBuff<RebornSlendermanTransitionBuff>())
            {
                level.AddBuff<RebornSlendermanTransitionBuff>();
            }
        }
        private void RebornSlendermanUpdate(LevelEngine level)
        {
            // 瘦长鬼影战斗
            // 如果不存在Boss，或者所有Boss死亡，进入BOSS后阶段。
            // 如果有Boss存活，不停生成怪物。
            var targetBosses = level.FindEntities(e => e.Type == EntityTypes.BOSS && e.IsHostileEntity() && !e.IsDead);
            if (targetBosses.Length <= 0)
            {
                SetBossState(level, BOSS_STATE_NIGHTMAREAPER_TRANSITION);
                level.AddBuff<RebornNightmareaperTransitionBuff>();

                // 隐藏UI，关闭输入
                level.ResetHeldItem();
                level.SetUIAndInputDisabled(true);
                level.StopMusic();
            }
            else
            {
                RunBossWave(level);
            }
        }
        private void RebornSlendermanRageUpdate(LevelEngine level)
        {
            // 如果瘦长鬼影的血量到达最大血量的一半时
            if (level.HasBuff<ReverseSatelliteBuffRage>())
            {
                SetBossState(level, BOSS_STATE_SLENDERMAN_RAGE);
            }
            level.ShakeScreen(10, 0.2f, 30);
        }
        private void RebornNightmareaperTransitionUpdate(LevelEngine level)
        {
            if (level.HasBuff<ReverseSatelliteBuffRage>())
            {
                level.RemoveBuffs<ReverseSatelliteBuffRage>();
            }
            ClearEnemies(level);
            if (level.EntityExists(e => e.Type == EntityTypes.BOSS && e.IsHostileEntity() && !e.IsDead))
            {
                // 梦魇收割者出现
                level.SetUIAndInputDisabled(false);
                SetBossState(level, BOSS_STATE_NIGHTMAREAPER);
                return;
            }
            if (!level.HasBuff<RebornNightmareaperTransitionBuff>())
            {
                level.AddBuff<RebornNightmareaperTransitionBuff>();
                level.StartRain();
            }
        }
        private void RebornNightmareaperUpdate(LevelEngine level)
        {
            // 梦魇收割者战斗
            // 如果不存在Boss，或者所有Boss死亡，进入BOSS后阶段。
            // 如果有Boss存活，不停生成怪物。
            if (!level.EntityExists(e => e.Type == EntityTypes.BOSS && e.IsHostileEntity() && !e.IsDead))
            {
                level.WaveState = VanillaLevelStates.STATE_AFTER_BOSS;
                level.StopMusic();
                if (!level.IsRerun)
                {
                    // 隐藏UI，关闭输入
                    level.ResetHeldItem();
                    level.SetUIAndInputDisabled(true);
                }
                else
                {
                    var reaper = level.FindFirstEntity(VanillaBossID.rebornnightmareaper);
                    Vector3 position;
                    if (reaper != null)
                    {
                        position = reaper.Position;
                    }
                    else
                    {
                        var x = (level.GetGridLeftX() + level.GetGridRightX()) * 0.5f;
                        var z = (level.GetGridTopZ() + level.GetGridBottomZ()) * 0.5f;
                        var y = level.GetGroundY(x, z);
                        position = new Vector3(x, y, z);
                    }
                    level.Produce(VanillaPickupID.clearPickup, position, null);
                }
            }
            else
            {
                RunBossWave(level);
            }
        }
        protected override void AfterBossWaveUpdate(LevelEngine level)
        {
            base.AfterBossWaveUpdate(level);
            ClearEnemies(level);
            //优化帧数考虑
            if (!level.IsRerun)
            {
                if (!level.IsCleared)
                {
                    if (!level.EntityExists(e => e.Type == EntityTypes.BOSS && e.IsHostileEntity()))
                    {
                        if (!level.HasBuff<RebornNightmareClearedBuff>())
                        {
                            level.AddBuff<RebornNightmareClearedBuff>();
                        }
                    }
                }
            }
        }

        public const int BOSS_STATE_SLENDERMAN = 0;
        public const int BOSS_STATE_SLENDERMAN_RAGE = 1;
        public const int BOSS_STATE_NIGHTMAREAPER_TRANSITION = 2;
        public const int BOSS_STATE_NIGHTMAREAPER = 3;
    }
}
