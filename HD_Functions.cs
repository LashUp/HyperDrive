using System.Collections.Generic;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;
using System;
using HyperDrive.Support;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Definitions;
using VRage.Game.ObjectBuilders.Definitions;
using System.Linq;
using VRage.Game.Entity;
using VRage.Game.ModAPI;

namespace HyperDrive.Functions
{
    public class HyperFunctions
    {

        
        
        

        public static Random diceRoll = new Random();

        public static HashSet<IMyEntity> entityList = new HashSet<IMyEntity>();
        public static List<MyPlanet> planetList = new List<MyPlanet>();
        public static List<string> planetNameList = new List<string>();
        public static float curPDistF = 0f;
        public static float maxPDistF = 2000000f;
        public static double distance = 0f;


        public static readonly List<MyResourceSourceComponent> _powerSources = new List<MyResourceSourceComponent>();
        public static readonly List<MyCubeBlock> _functionalBlocks = new List<MyCubeBlock>();
        

        public static float _maxPower;
        //private float _availablePower;
        //private float _currentPower;
        public static float _powerPercent;
        public static MyPlanet planet;
        
        

        double _targetDistance = 0f;
        Vector3D _waypoint = new Vector3D(0, 0, 0);
        public static IMyRemoteControl _rc;
        bool _rcsettings = false;

        public static void EmergencyStop()
        {
            List<IMySlimBlock> _termBlocks = new List<IMySlimBlock>();
            HyperDriveLogic.hyperDriveBlock.CubeGrid.GetBlocks(_termBlocks);
            var termBlocks = _termBlocks.Where(x => x.FatBlock is IMyTerminalBlock).ToList();
            var _diceRoll = diceRoll.Next(1, 6);
            HyperDriveLogic.hyper = false;

            foreach (var _termB in termBlocks)
            {
                if (_diceRoll > 4)
                {

                    MySoundPair _electricAudio = new MySoundPair("ParticleElectricalDischarge");
                    HyperDriveLogic.emitter.PlaySound(_electricAudio);

                    double _dmgDiceChance = diceRoll.Next(0, 100);

                    if (_termB.FatBlock is IMyReactor)
                    {
                        if ((_termB.FatBlock as IMyFunctionalBlock).Enabled)
                        {
                            var _termBDisable = _termB.FatBlock as IMyFunctionalBlock;
                            _termBDisable.Enabled = false;
                        }
                    }

                    if (_termB.FatBlock is IMyBatteryBlock && _dmgDiceChance > 17)
                    {
                        if ((_termB.FatBlock as IMyFunctionalBlock).Enabled)
                        {
                            var _termBDisable = _termB.FatBlock as IMyFunctionalBlock;
                            _termBDisable.Enabled = false;
                        }
                    }

                    if (_termB.FatBlock is IMyLightingBlock && _dmgDiceChance > 17)
                    {
                        if ((_termB.FatBlock as IMyFunctionalBlock).Enabled)
                        {
                            var _termBDisable = _termB.FatBlock as IMyFunctionalBlock;
                            _termBDisable.Enabled = false;
                        }
                    }

                    if (_dmgDiceChance > 70)
                    {
                        double _dmgDice = diceRoll.Next(0, 50);
                        var _dmgDiceF = (float)_dmgDice * 0.01f;

                        if (float.IsPositiveInfinity(_dmgDiceF))
                        {
                            _dmgDiceF = float.MaxValue;
                        }
                        else if (float.IsNegativeInfinity(_dmgDiceF))
                        {
                            _dmgDiceF = float.MinValue;
                        }

                        if (_dmgDiceChance > 90 && _termB.FatBlock is IMyGasTank)
                        {
                            int explosionPower = (int)0;

                            var myGasTank = _termB.FatBlock as IMyGasTank;
                            if (myGasTank != null)
                                explosionPower = (int)(myGasTank.Capacity * 0.05f);

                            if (explosionPower < 500)
                                explosionPower = 500;

                            float explosionRadius = (explosionPower / 50000) * 5;
                            MyVisualScriptLogicProvider.CreateExplosion(_termB.FatBlock.GetPosition(), explosionRadius, explosionPower);

                            var _termBDamage = _termB.MaxIntegrity * 1.1f;

                            try
                            {
                                _termB.DoDamage(_termBDamage, MyDamageType.Destruction, true);
                            }
                            catch
                            {
                                Logging.Logging.Instance.WriteLine("GasTank Explosion System Failure");
                                return;
                            }
                        }

                        else
                        {

                            var _termBDamage = _termB.MaxIntegrity * _dmgDiceF;

                            try
                            {
                                _termB.DoDamage(_termBDamage, MyDamageType.Fire, true);
                                //Use Destruction to damage even with shields, Fire for persistent effects
                            }
                            catch
                            {
                                Logging.Logging.Instance.WriteLine("EmergencyStop Damage System Failure");
                                return;
                            }
                        }
                    }
                }
            }
        }

        //-------------TERMINAL CONTROLS-------------//

        public static void CreatehyperUI()
        {
            object engageButton;
            engageButton = new EngageButton<Sandbox.ModAPI.Ingame.IMyUpgradeModule>((IMyTerminalBlock)HyperDriveLogic.hyperDriveBlock,
                "starthyper",
                "Engage / Disengage");

            for (var hyper_actionIndex = 0; hyper_actionIndex < 1; ++hyper_actionIndex)
            {
                    HyperDriveLogic.ActionEngage = new ActivatehyperProfileAction<Sandbox.ModAPI.Ingame.IMyUpgradeModule>((IMyTerminalBlock)HyperDriveLogic.hyperDriveBlock,
                    "starthyper" + hyper_actionIndex.ToString(),
                    "Engage / Disengage"/* + hyper_actionIndex.ToString()*/,
                    hyper_actionIndex,
                    @"Textures\GUI\Icons\Actions\Start.dds");
            }
        }
        public class EngageButton<T> : HyperDrive.hyperControl.ButtonhyperControl<T>
        {
            public EngageButton(IMyTerminalBlock hyper_block,
                string internalName,
                string title)
                : base(HyperDriveLogic.hyperDriveBlock, internalName, title)
            {
                CreatehyperUI();
            }

            public override void OnhyperAction(IMyTerminalBlock hyper_block)
            {
                var hyperDriveBlock_Block = hyper_block.GameLogic.GetAs<HyperDriveLogic>();
                if (hyperDriveBlock_Block == null) { Logging.Logging.Instance.WriteLine("Null Error 2"); return; }
                hyperDriveBlock_Block.Engage_OnOff();
            }
        }

        public class ActivatehyperProfileAction<T> : hyperControl.ControlhyperAction<T>
        {
            public int ProfileNr;
            public ActivatehyperProfileAction(
                IMyTerminalBlock hyper_block,
                string internalName,
                string name,
                int profileNr,
                string icon)
                : base(hyper_block, internalName, name, icon)
            {
                ProfileNr = profileNr;
            }

            public override bool hyperVisible(IMyTerminalBlock hyper_block)
            {
                var hyperDriveBlock_Block = hyper_block.GameLogic.GetAs<HyperDriveLogic>();
                if (hyperDriveBlock_Block == null) { return false; }
                return base.hyperVisible(hyper_block);
            }

            public override void OnhyperAction(IMyTerminalBlock hyper_block)
            {
                var hyperDriveBlock_Block = hyper_block.GameLogic.GetAs<HyperDriveLogic>();
                if (hyperDriveBlock_Block == null) { Logging.Logging.Instance.WriteLine("Null Error 4"); return; }
                hyperDriveBlock_Block.Engage_OnOff();
            }

        }

        public static void hyperDriveBlock_AppendingCustomInfo(IMyTerminalBlock termblock, StringBuilder info)
        {
            float maxInput = HyperDriveLogic.ResourceSink.MaxRequiredInputByType(HyperDriveLogic._electricity);
            float currentInput = HyperDriveLogic.ResourceSink.CurrentInputByType(HyperDriveLogic._electricity);
            string suffix = " MW";

            if (maxInput < 1)
            {
                maxInput *= 1000f;
                currentInput *= 1000f;
                suffix = " kW";
                if (maxInput < 1)
                {
                    maxInput *= 1000f;
                    currentInput *= 1000f;
                    suffix = " W";
                }
            }
            if (HyperDriveLogic.hyperFactor >= 0f && HyperDriveLogic.WF_Scale >= 0f)
            {
                float hyperFactor_S = (HyperDriveLogic.hyperFactor * 0.01f);
                float WF_Scale_S = (HyperDriveLogic.WF_Scale * 0.01f);
                info.AppendLine("Maximum hyper Factor: " + hyperFactor_S.ToString("0"));
                info.AppendLine("Current hyper Factor: " + WF_Scale_S.ToString("0.0"));
                info.AppendLine("Idle Input: " + maxInput.ToString("0.00") + suffix);
                info.AppendLine("Current Input: " + currentInput.ToString("0.00") + suffix);
            }
            else
            {
                float hyperFactor_S = 0f;
                float WF_Scale_S = 0f;
                info.AppendLine("Maximum hyper Factor: " + hyperFactor_S.ToString("0"));
                info.AppendLine("Current hyper Factor: " + WF_Scale_S.ToString("0.0"));
                info.AppendLine("Idle Input: " + maxInput.ToString("0.00") + suffix);
                info.AppendLine("Current Input: " + currentInput.ToString("0.00") + suffix);
            }
        }

        public static double Distance()
        {
            foreach (var planet in planetList)
            {
                var planetEntity = planet as IMyEntity;
                distance = Math.Round(Vector3D.Distance(HyperDriveLogic.hyperDriveBlock.CubeGrid.GetPosition(), planetEntity.GetPosition()), 2);
            }
            return distance;
        }

        public static void RefreshLists()
        {
            entityList.Clear();
            planetList.Clear();
            planetNameList.Clear();

            MyAPIGateway.Entities.GetEntities(entityList);

            if (entityList.Count != 0)
            {

                foreach (var entity in entityList)
                {

                    planet = entity as MyPlanet;

                    if (planet != null)
                    {

                        planetList.Add(planet);

                    }

                }
            }

            planetNameList.Clear();
            var planetDefList = MyDefinitionManager.Static.GetPlanetsGeneratorsDefinitions();
            foreach (var planetDef in planetDefList)
            {

                planetNameList.Add(planetDef.Id.SubtypeName);

            }

        }

        public static float MinimumPowertoActivate()
        {
            return 15f;
        }

        public static float PowerConsumption()
        {
            if (HyperDriveLogic.WF_Scale < Single.Epsilon && IsWorking())
            {
                return 15f;
            }

            else if (HyperDriveLogic.WF_Scale < Single.Epsilon && !IsWorking())
            {
                return 0f;
            }

            else
            {
                //return (float)((15f + (17f * (WF_Scale * 0.023f)) * (totalMassF * 0.0015f)));
                //return (float)((15f + WF_Scale) + (totalMassF * 0.036f / 3f));
                //return (float)((totalMassF * totalMassF) / (_maxPower * _maxPower * _maxPower) * 0.01f);
                if (HyperDriveLogic.totalMassF >= 0f)
                {
                    return (float)((15f + (17f * (HyperDriveLogic.WF_Scale * 0.023f)) * (HyperDriveLogic.totalMassF * 0.003f)));
                }
                else
                {
                    return 0f;
                }
            }
        }

        public static bool IsWorking()
        {
            return HyperDriveLogic.hyperDriveBlock.IsFunctional && HyperDriveLogic.hyperDriveBlock.Enabled && HyperDriveLogic.ResourceSink.IsPowerAvailable(HyperDriveLogic._electricity, MinimumPowertoActivate());
        }

        public static void BackGroundChecks()
        {
            lock (_powerSources) _powerSources.Clear();
            lock (_functionalBlocks) _functionalBlocks.Clear();

            foreach (var hyper_block in ((MyCubeGrid)HyperDriveLogic.hyperDriveBlock.CubeGrid).GetFatBlocks())
            {
                lock (_functionalBlocks) if (hyper_block.IsFunctional) _functionalBlocks.Add(hyper_block);
                var source = hyper_block.Components.Get<MyResourceSourceComponent>();
                if (source == null) continue;
                foreach (var type in source.ResourceTypes)
                {
                    if (type != MyResourceDistributorComponent.ElectricityId) continue;
                    lock (_powerSources) _powerSources.Add(source);
                    break;
                }
            }
        }

        public static void UpdateGridPower()
        {
            _maxPower = 0f;

            lock (_powerSources)
                for (int i = 0; i < _powerSources.Count; i++)
                {
                    var source = _powerSources[i];
                    if (!source.Enabled || !source.ProductionEnabled) continue;
                    _maxPower += source.MaxOutput;
                }
        }

        /*public class Dummy
        {
            public int DummyID { get; set; }
            public string DummyName { get; set; }
        }

        class StudentDictionaryComparer : IEqualityComparer<KeyValuePair<int, Dummy>>
        {
            public bool Equals(KeyValuePair<int, Dummy> x, KeyValuePair<int, Dummy> y)
            {
                if (x.Key == y.Key && (x.Value.DummyID == y.Value.DummyID) && (x.Value.DummyName == y.Value.DummyName))
                    return true;

                return false;
            }

            public int GetHashCode(KeyValuePair<int, Dummy> obj)
            {
                return obj.Key.GetHashCode();
            }
        }

        class Program
        {
            static void Main(string[] args)
            {
                IDictionary<int, Dummy> DummyDict = new Dictionary<int, Dummy>()
                    {
                        { 1, new Dummy(){ DummyID =1, DummyName = "Bill"}},
                        { 2, new Dummy(){ DummyID =2, DummyName = "Steve"}},
                        { 3, new Dummy(){ DummyID =3, DummyName = "Ram"}}
                    };

                Dummy std = new Dummy() { DummyID = 1, DummyName = "Bill" };

                KeyValuePair<int, Dummy> elementToFind = new KeyValuePair<int, Dummy>(1, std);

                bool result = DummyDict.Contains(elementToFind, new StudentDictionaryComparer()); // returns true

                Console.WriteLine(result);
            }
        }

        public static void GetHyperLane()
        {
            // test

            Vector3D from = new Vector3D();
            Vector3D to = new Vector3D();
            Vector3D toTarget = new Vector3D();

            MyEntitySubpart subpart1 = HyperDriveLogic.hyperDriveBlock.GetSubpart("InteriorTurretBase1");
            MyEntitySubpart subpart2 = subpart1.GetSubpart("InteriorTurretBase2");
            //List<IMyModelDummy> dummies = new List<IMyModelDummy>();
            string lll = "";
            int ooo = 0;

            //IMyModelDummy dummy1 = 

            Matrix matrixA = new Matrix();
            string modelName = "";
            HyperDriveLogic.hyperDriveBlock


            //IMyModelDummy dummy1 = HyperDriveLogic.hyperDriveBlock.Model.GetDummies();
            List<string> Dummies = new List<string>();
            var dummies = new Dictionary<string, IMyModelDummy>();
            HyperDriveLogic.hyperDriveBlock.Model.GetDummies(dummies);
            bool dname = false;
            IMyModelDummy dummy1 = dummies.TryGetValue("HD_Front", out dname);
            foreach (var dummy in dummies)
            {
                Dummies.Add(dummy);
            }

            //IMyModelDummy
            //MyAPIGateway.Utilities.ShowNotification("Dif: " + (currentShootTime - lastShootTime), 17, MyFontEnum.Blue);

            from = HyperDriveLogic.hyperDriveBlock.WorldMatrix.Translation + HyperDriveLogic.hyperDriveBlock.WorldMatrix.Forward * 2.5d;
            to = HyperDriveLogic.hyperDriveBlock.WorldMatrix.Translation + HyperDriveLogic.hyperDriveBlock.WorldMatrix.Forward * 3000d;

            //from = 

            LineD testRay = new LineD(from, to);

            List<MyLineSegmentOverlapResult<MyEntity>> result = new List<MyLineSegmentOverlapResult<MyEntity>>();

            MyGamePruningStructure.GetAllEntitiesInRay(ref testRay, result);


            foreach (var resultItem in result)
            {
                IMyCubeGrid grid = resultItem.Element as IMyCubeGrid;
                MyPlanet planet = resultItem.Element as MyPlanet;

                Vector3D? resultV3D = planet.GetIntersectionWithLineAndBoundingSphere(ref testRay, 1.2f);

                //if (planet != null)
                //{

                if (resultV3D != null)
                {
                    toTarget = from + subpart2.WorldMatrix.Forward * 1.2f;
                }

                //}



                    planet.Components.Get<MyGravityProviderComponent>().IsPositionInRange(resultV3D);
                if (planet != null)
                {

                }

                IMyDestroyableObject destroyableEntity = resultItem.Element as IMyDestroyableObject;

                if (grid != null)
                {
                    IMySlimBlock slimblock;

                    //IMyGravityProvider gravityProvider;

                    double hitd;

                    Vector3D? resultVec = grid.GetLineIntersectionExactAll(ref testRay, out hitd, out slimblock);

                    if (resultVec != null)
                    {

                        hitBool = true;

                        toTarget = from + subpart2.WorldMatrix.Forward * hitd;

                        if (!MyAPIGateway.Session.CreativeMode)
                        {
                            slimblock.DoDamage(beamWeaponInfo.damage * (currentHeat / beamWeaponInfo.maxHeat + 0.2f), MyStringHash.GetOrCompute("Laser"), false, default(MyHitInfo), cubeBlock.EntityId);
                        }
                        else
                        {
                            slimblock.DoDamage(beamWeaponInfo.damage * 1.2f, MyStringHash.GetOrCompute("Laser"), false, default(MyHitInfo), cubeBlock.EntityId);
                        }
                        //MyAPIGateway.Utilities.ShowNotification("" + s.BlockDefinition.Id.SubtypeId + " ::: " + resultItem.Distance, 17);
                    }
                }
                if (destroyableEntity != null)
                {
                    IMyEntity ent = (IMyEntity)destroyableEntity;
                    double hitd = (from - ent.WorldMatrix.Translation).Length();

                    toTarget = from + subpart2.WorldMatrix.Forward * hitd;

                    hitBool = true;

                    if (!MyAPIGateway.Session.CreativeMode)
                    {
                        destroyableEntity.DoDamage(beamWeaponInfo.damage * (currentHeat / beamWeaponInfo.maxHeat + 0.2f), MyStringHash.GetOrCompute("Laser"), false, default(MyHitInfo), cubeBlock.EntityId);
                    }
                    else
                    {
                        destroyableEntity.DoDamage(beamWeaponInfo.damage * 1.2f, MyStringHash.GetOrCompute("Laser"), false, default(MyHitInfo), cubeBlock.EntityId);
                    }
                }

            }


            // test

            lastShootTime = currentShootTime;
            lastShootTimeTicks = ticks;

            currentHeat += beamWeaponInfo.heatPerTick;

            if (currentHeat > beamWeaponInfo.maxHeat)
            {
                currentHeat = beamWeaponInfo.maxHeat;

                overheated = true;
            }

        }*/

        //Color White = new Color();
        //White = Color.White;
        //MyVisualScriptLogicProvider.ScreenColorFadingSetColor(White);
        //MyVisualScriptLogicProvider.ScreenColorFadingStart(4, true);
        //MyVisualScriptLogicProvider.ScreenColorFadingStartSwitch(1);
        //Needed??? MyVisualScriptLogicProvider.PlayerConnected += Factions.PlayerConnected;

        //var shipForward = remoteControl.Orientation.Forward;
        //MatrixD shipFWD_MTX = new MatrixD();
        //shipFWD = shipFWD_MTX.Translation;
        //var _termBDisable = remote.FatBlock as IMyFunctionalBlock;
        //_termBDisable.Enabled = false;
    }
}