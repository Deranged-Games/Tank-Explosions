using System.Collections.Generic;
using System;
using VRageMath;

using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Game.ParticleEffects;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems;
using Sandbox.Game;
using Sandbox.Definitions;

using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.Library.Utils;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Dondelium.crittanks{
  [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
  public class CriticalExplode : MySessionComponentBase{
    bool hasinit = false;
    bool isServer = false;
    MyRandom rand = new MyRandom();
    
    public override void UpdateBeforeSimulation(){
      if (!hasinit){
        if (MyAPIGateway.Session == null) return;
        Init();
      }
    }
    
    private void Init(){
      hasinit = true;
      isServer = MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE || MyAPIGateway.Multiplayer.IsServer;
      if(isServer) MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, damageHook);
    }

    private void damageHook(object target, ref MyDamageInformation info){
      if (info.Type==MyDamageType.Grind) return;

      if (target is IMySlimBlock){
        var entity = target as IMySlimBlock;{
          if (entity.FatBlock is IMyGasTank){
            var tBlock = entity.FatBlock as IMyTerminalBlock;
            var def = entity.BlockDefinition as MyCubeBlockDefinition;
            var grid = tBlock.CubeGrid as IMyCubeGrid;
            var gasBlock = entity.FatBlock as IMyGasTank;
            Vector3D myPos = gasBlock.GetPosition();

            float critdmg = (1f - def.CriticalIntegrityRatio) * entity.MaxIntegrity;
            float curDmg = info.Amount + entity.CurrentDamage;

            if(tBlock.IsWorking && curDmg > critdmg){
              float dmg = (gasBlock.Capacity * (float)gasBlock.FilledRatio * 0.003f / grid.GridSize);

              //Check atmospheres. If we find oxygen, bigger boom, else, much smaller boom.
              bool airtight = false;
              try{
                airtight = grid.IsRoomAtPositionAirtight(gasBlock.Position);
              } catch(Exception e){}
              if(airtight){
                dmg *= 5f;
              } else {
                foreach(var kv in PlanetStorage.planets){
                  var planet = kv.Value;
                  if(planet.Closed || planet.MarkedForClose || !planet.HasAtmosphere){
                    continue;
                  }
                  if(Vector3D.DistanceSquared(myPos, planet.WorldMatrix.Translation) < (planet.AtmosphereRadius * planet.AtmosphereRadius)){
                    dmg *= 5f * planet.GetAirDensity(myPos);
                    break;
                  }
                }
              }

              //Potential explosion damage less than remaining tank health? Cancel explosion, and vent!
              if((dmg * 2.5f) < (entity.MaxIntegrity - curDmg)){
                if(grid.Physics == null || grid.Physics.IsStatic)
                  return;
                float x = rand.NextFloat() * 2 - 1;
                float y = rand.NextFloat() * 2 - 1;
                float z = rand.NextFloat() * 2 - 1;
                Vector3 thrustVec = new Vector3(x, y, z);
                grid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, Vector3.Normalize(thrustVec) * (gasBlock.Capacity * (float)gasBlock.FilledRatio * 20), myPos, null);
                return;
              } else {
                //Shrapnel from remaining tank integrity
                if(entity.MaxIntegrity > curDmg && (float)gasBlock.FilledRatio >= 0.5f)
                  dmg += (entity.MaxIntegrity - curDmg) / 4;
              }

              //Finalize damage and radius
              float radius = dmg / 2000f;
              if(dmg > 500000f) dmg = 500000f;
              if(radius > 100f) radius = 100f;

              //Explosion Effects!
              MyParticleEffect explosionEffect = null;
              MyParticlesManager.TryCreateParticleEffect("Explosion_Missile", gasBlock.WorldMatrix, out explosionEffect);
              if (explosionEffect != null){
                explosionEffect.UserScale = radius / 6f;
                explosionEffect.UserLifeMultiplier = radius / 7f;
              }

              //Explosion Damage!
              BoundingSphereD sphere = new BoundingSphereD(myPos, radius);
              MyExplosionInfo bomb = new MyExplosionInfo(dmg, dmg, sphere, MyExplosionTypeEnum.BOMB_EXPLOSION, true, true);
              bomb.CreateParticleEffect = false;
              bomb.LifespanMiliseconds = 150 + (int)radius * 45;
              MyExplosions.AddExplosion(ref bomb, true);

              //MyAPIGateway.Utilities.ShowNotification("Damage: "+dmg.ToString()+" Radius: "+radius.ToString(), 15000, MyFontEnum.Red);
            }
          }
        }
      }
    }

    protected override void UnloadData(){}
  }
}
