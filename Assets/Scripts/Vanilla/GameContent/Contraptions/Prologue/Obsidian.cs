﻿using MVZ2.GameContent.Buffs.Contraptions;
using MVZ2.Vanilla.Audios;
using MVZ2.Vanilla.Entities;
using MVZ2Logic.Level;
using PVZEngine.Entities;
using PVZEngine.Level;

namespace MVZ2.GameContent.Contraptions
{
    [EntityBehaviourDefinition(VanillaContraptionNames.obsidian)]
    public class Obsidian : ContraptionBehaviour
    {
        public Obsidian(string nsp, string name) : base(nsp, name)
        {
        }
        protected override void UpdateLogic(Entity contraption)
        {
            base.UpdateLogic(contraption);
            var maxHP = contraption.GetMaxHealth();
            bool netherite = contraption.HasBuff<ObsidianArmorBuff>();
            if (netherite)
            {
                if (contraption.Health <= maxHP * 0.4f)
                {
                    var hp = contraption.Health;
                    contraption.RemoveBuffs<ObsidianArmorBuff>();
                    netherite = false;
                    contraption.Health = hp;
                }
            }

            if (netherite)
            {
                var percent = GetArmoredDamagePercent(contraption, maxHP);
                contraption.SetModelDamagePercent(percent);
            }
            else
            {
                contraption.SetModelDamagePercent();
            }
            contraption.SetModelProperty("Netherite", netherite);
        }

        public override bool CanEvoke(Entity entity)
        {
            if (entity.HasBuff<ObsidianArmorBuff>())
                return false;
            return base.CanEvoke(entity);
        }

        protected override void OnEvoke(Entity contraption)
        {
            base.OnEvoke(contraption);
            contraption.AddBuff<ObsidianArmorBuff>();
            contraption.Health = contraption.GetMaxHealth();
            contraption.Level.PlaySound(VanillaSoundID.armorUp);
        }
        private float GetArmoredDamagePercent(Entity contraption, float maxHP)
        {
            var percent = contraption.Health / maxHP;
            var armorPercent = (percent - 0.4f) / 0.6f;
            return 1 - armorPercent;
        }
    }
}
