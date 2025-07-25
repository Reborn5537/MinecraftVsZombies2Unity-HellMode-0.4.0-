﻿using MVZ2.Vanilla.Audios;
using MVZ2.Vanilla.Entities;
using PVZEngine.Entities;
using PVZEngine.Level;
using UnityEngine;

namespace MVZ2.GameContent.Contraptions
{
    [EntityBehaviourDefinition(VanillaContraptionNames.woodenDropper)]
    public class WoodenDropper : DispenserFamily
    {
        public WoodenDropper(string nsp, string name) : base(nsp, name)
        {
        }

        public override void Init(Entity entity)
        {
            base.Init(entity);
            InitShootTimer(entity);
        }
        protected override void UpdateAI(Entity entity)
        {
            base.UpdateAI(entity);
            if (!entity.IsEvoked())
            {
                ShootTick(entity);
                return;
            }
        }
        protected override void OnEvoke(Entity entity)
        {
            base.OnEvoke(entity);
            var rng = entity.RNG;
            for (int i = 0; i < 30; i++)
            {
                var xspeed = entity.GetFacingX() * rng.Next(10f, 18f);
                var yspeed = rng.Next(25f);
                var zspeed = rng.Next(-1.5f, 1.5f);
                var spawnParam = entity.GetSpawnParams();
                spawnParam.SetProperty(EngineEntityProps.SCALE, Vector3.one * 2);
                spawnParam.SetProperty(EngineEntityProps.DISPLAY_SCALE, Vector3.one * 2);
                spawnParam.SetProperty(VanillaEntityProps.SHADOW_SCALE, Vector3.one * 2);

                var param = entity.GetShootParams();
                param.damage *= 4;
                param.velocity = new Vector3(xspeed, yspeed, zspeed);
                param.spawnParam = spawnParam;
                var ball = entity.ShootProjectile(param);
            }
            entity.PlaySound(VanillaSoundID.launch);
        }
    }
}
