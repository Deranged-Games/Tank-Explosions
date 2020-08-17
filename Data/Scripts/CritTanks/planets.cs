using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Sandbox.Common;
using Sandbox.Common.Components;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.Game;
using VRage.Common.Utils;
using VRageMath;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using VRage.ModAPI;
using VRage.Utils;
using VRage.Library.Utils;

using System.Text.RegularExpressions;
using Ingame = Sandbox.ModAPI.Ingame;

namespace Dondelium.crittanks{
  [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
  public class PlanetStorage : MySessionComponentBase{
    public static Dictionary<long, MyPlanet> planets = new Dictionary<long, MyPlanet>();
    private int skip = 0;
    private const int SKIP_TICKS = 180;
    
    private static HashSet<IMyEntity> ents = new HashSet<IMyEntity>();

    //Admin values.
    private bool init = false;
    public static PlanetStorage instance;

    public void Init(){}

    public override void UpdateAfterSimulation(){
      if(!init){
        init = true;
        instance = this;
      }

      if(++skip >= SKIP_TICKS){
        skip = 0;
        MyAPIGateway.Entities.GetEntities(ents, delegate(IMyEntity e){
          if(e is MyPlanet){
            if(!PlanetStorage.planets.ContainsKey(e.EntityId)){
              PlanetStorage.planets.Add(e.EntityId, e as MyPlanet);
            }
          }
          return false;
        });
      }
    }

    protected override void UnloadData(){
      planets.Clear();
      ents.Clear();
    }
  }
}