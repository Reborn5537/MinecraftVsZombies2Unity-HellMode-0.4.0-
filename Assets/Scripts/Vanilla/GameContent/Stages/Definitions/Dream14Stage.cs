/*using MVZ2.GameContent.Stages;
using PVZEngine.Definitions;
using PVZEngine.Entities;
using PVZEngine.Level;
using UnityEngine;
using System.Collections.Generic;
using Tools;
using MVZ2.Vanilla;
using MVZ2.Vanilla.Enemies;

namespace MVZ2.GameContent.Stages
{
    [StageDefinition(VanillaStageNames.dream14)]
    public partial class Dream14Stage : StageDefinition
    {
        private class WaveConfig
        {
            public NamespaceID[] Enemies;
            public int[] Weights;
            public int PortalCount;
        }
        private static readonly Dictionary<int, WaveConfig> WAVE_CONFIGS = new Dictionary<int, WaveConfig>
        {
            {
                10, new WaveConfig
                {
                    Enemies = new[]
                    {
                        VanillaEnemyID.zombie,
                        VanillaEnemyID.leatherCappedZombie,
                        VanillaEnemyID.ironHelmettedZombie
                    },
                    Weights = new[] {8, 5, 2},
                    PortalCount = 4
                }
            },
            {
                20, new WaveConfig
                {
                    Enemies = new[]
                    {
                        VanillaEnemyID.zombie,
                        VanillaEnemyID.leatherCappedZombie,
                        VanillaEnemyID.ironHelmettedZombie,
                        VanillaEnemyID.necromancer,
                        VanillaEnemyID.boneWall
                    },
                    Weights = new[] {7, 5, 3, 5, 6},
                    PortalCount = 6
                }
            },
            {
                30, new WaveConfig
                {
                    Enemies = new[]
                    {
                        VanillaEnemyID.ghast,
                        VanillaEnemyID.parasiteTerror,
                        VanillaEnemyID.nightmarefollower,
                        VanillaEnemyID.mesmerizer,
                        VanillaEnemyID.boneWall,
                        VanillaEnemyID.skeletonHorse
                    },
                    Weights = new[] {7, 6, 5, 7, 7, 6},
                    PortalCount = 7
                }
            },
            {
                40, new WaveConfig
                {
                    Enemies = new[]
                    {
                        VanillaEnemyID.flagZombie,
                        VanillaEnemyID.parasiteTerror,
                        VanillaEnemyID.nightmarefollower,
                        VanillaEnemyID.mesmerizer,
                        VanillaEnemyID.boneWall,
                        VanillaEnemyID.skeletonHorse,
                        VanillaEnemyID.mutantZombie,
                        VanillaEnemyID.bedserker
                    },
                    Weights = new[] {3, 8, 7, 7, 6, 8, 5, 1},
                    PortalCount = 8
                }
            }
        };
        
public Dream14Stage(string nsp, string name) : base(nsp, name)
        {
            // 保留原有初始化逻辑
            AddBehaviour(new WaveStageBehaviour(this));
            AddBehaviour(new FinalWaveClearBehaviour(this));
            AddBehaviour(new GemStageBehaviour(this));
            AddBehaviour(new StarshardStageBehaviour(this));

            // 添加类型明确的字段
            AddBehaviourField("StageRNG", new RandomGenerator());
        }

        // 保留OnPostWave和GeneratePortals方法
        public override void OnPostWave(LevelEngine level, int wave)
        {
            base.OnPostWave(level, wave);
            if (WAVE_CONFIGS.TryGetValue(wave, out var config))
            {
                GeneratePortals(level, config);
            }
        }

        private void GeneratePortals(LevelEngine level, WaveConfig config)
        {
            // 保持坐标生成逻辑不变
            var positions = new List<Vector2Int>();
            for (int c = level.GetMaxColumnCount() - 3; c < level.GetMaxColumnCount(); c++)
            {
                for (int r = 0; r < level.GetMaxLaneCount(); r++)
                {
                    positions.Add(new Vector2Int(c, r));
                }
            }

            var rng = GetStageRNG(level);
            for (int i = 0; i < config.PortalCount && positions.Count > 0; i++)
            {
                // 保留位置选择逻辑
                var posIndex = rng.Next(positions.Count);
                var gridPos = positions[posIndex];
                positions.RemoveAt(posIndex);

                // 精确坐标换算
                var x = level.GetEntityColumnX(gridPos.x);
                var z = level.GetEntityLaneZ(gridPos.y);
                var y = level.GetGroundY(x, z);

                // 生成特效时添加类型转换
                var portal = level.Spawn(VanillaEffectID.nightmarePortal, pos);
                NightmarePortal.SetEnemyID(portal, config.Enemies[rng.WeightedRandom(config.Weights)]);
            }

            level.PlaySound(VanillaSoundID.nightmarePortal);
        }

        // 添加明确的字段访问器
        private static RandomGenerator GetStageRNG(LevelEngine level) =>
            level.GetBehaviourField<RandomGenerator>(ID, "StageRNG");
    }
}
*/