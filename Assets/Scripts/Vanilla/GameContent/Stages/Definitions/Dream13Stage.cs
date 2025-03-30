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
            level.ReplaceSeedPacks(new NamespaceID[]
            {
                VanillaContraptionID.furnace,
                VanillaContraptionID.lilyPad,
                VanillaContraptionID.drivenser,
                VanillaContraptionID.gravityPad,
                VanillaContraptionID.pistenser,
                VanillaContraptionID.totenser,
                VanillaContraptionID.dreamCrystal,
                VanillaContraptionID.dreamSilk,
            });
        }
    }
}
