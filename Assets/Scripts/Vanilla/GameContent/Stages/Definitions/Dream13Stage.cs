using MVZ2.GameContent.Buffs.Enemies;
using MVZ2.GameContent.Effects;
using MVZ2.GameContent.HeldItems;
using MVZ2.GameContent.Seeds;
using MVZ2.Vanilla.HeldItems;
using MVZ2.Vanilla.Level;
using MVZ2Logic.Level;
using PVZEngine;
using PVZEngine.Definitions;
using PVZEngine.Entities;
using PVZEngine.Level;
using UnityEngine;
using MVZ2.GameContent.Artifacts;
using MVZ2.GameContent.Bosses;
using MVZ2.GameContent.Contraptions;
using MVZ2.GameContent.Enemies;
using MVZ2.GameContent.ProgressBars;
using MVZ2.Vanilla;

namespace MVZ2.GameContent.Stages
{
    [StageDefinition(VanillaStageNames.dream13)]
    public partial class Dream13Stage : StageDefinition
    {
        public Dream13Stage(string nsp, string name) : base(nsp, name)
        {
            AddBehaviour(new WaveStageBehaviour(this));
            AddBehaviour(new FinalWaveClearBehaviour(this));
            AddBehaviour(new GemStageBehaviour(this));
            AddBehaviour(new StarshardStageBehaviour(this));
        }
        public override void OnStart(LevelEngine level)
        {
            base.OnStart(level);
            ClassicStart(level);
        }
        private void ClassicStart(LevelEngine level)
        {
            level.SetStarshardSlotCount(3);
            level.SetStarshardCount(1);
            level.SetSeedSlotCount(8);
            level.ReplaceSeedPacks(new NamespaceID[]
            {
                VanillaContraptionID.furnace,
                VanillaContraptionID.lilyPad,
                VanillaContraptionID.dispenser,
                VanillaContraptionID.gravityPad,
                VanillaContraptionID.teslaCoil,
                VanillaContraptionID.totenser,
                VanillaContraptionID.dreamCrystal,
                VanillaContraptionID.punchton
            });
            level.SetArtifactSlotCount(3);
            level.ReplaceArtifacts(new NamespaceID[]
            {
                VanillaArtifactID.netherStar,
                VanillaArtifactID.dreamKey,
                VanillaArtifactID.theCreaturesHeart,
            });
        }
    }
}
