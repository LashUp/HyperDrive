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
using HyperDrive.Functions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Definitions;
using VRage.Game.ObjectBuilders.Definitions;
using System.Linq;
using VRage.Game.Entity;
using VRage.Game.ModAPI;

namespace HyperDrive
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_UpgradeModule), false, "CX3WarpCore")]
    public class HyperDriveLogic : MyGameLogicComponent
    {

        Vector3D from = new Vector3D();
        Vector3D to = new Vector3D();
        Vector3D lastPos = new Vector3D();
        Vector3D destination = new Vector3D();
        Vector3D exit = new Vector3D();

        public static HyperDrive.hyperControl.ButtonhyperControl<Sandbox.ModAPI.Ingame.IMyUpgradeModule> engageButton;
        public static HyperDrive.hyperControl.ControlhyperAction<Sandbox.ModAPI.Ingame.IMyUpgradeModule> ActionEngage;
        public static MyEntity3DSoundEmitter emitter;
        public static MySoundPair pair = new MySoundPair("Hyper");

        public static bool hyper = false;
        public static bool BubbleFormed = true;
        public static float Maxhyper = 0f;

        int _ticks = 132000;
        private uint _tick;
        int warpTimer = 0;

        Color White = new Color();
        public static bool jumpOut = false;
        public static bool jumpIn = false;
        public static bool hyperSpace = false;


        bool _bubbleNotification = false;
        bool msgSent = false;

        bool fade = true;

        bool firstrun = true;
        bool HyperEngaged = false;
        bool gravMsg = false;

        public static IMyUpgradeModule hyperDriveBlock;
        MyObjectBuilder_EntityBase _objectBuilder;

        public static MyResourceSinkComponent ResourceSink;
        public static MyDefinitionId _electricity = MyResourceDistributorComponent.ElectricityId;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            _objectBuilder = objectBuilder;

            hyperDriveBlock = Entity as IMyUpgradeModule;

            if (!hyperDriveBlock.Components.TryGet<MyResourceSinkComponent>(out ResourceSink))
            {
                ResourceSink = new MyResourceSinkComponent();
                var sinkInfo = new MyResourceSinkInfo();
                sinkInfo.ResourceTypeId = _electricity;
                ResourceSink.AddType(ref sinkInfo);

                hyperDriveBlock.Components.Add(ResourceSink);
            }
        }

        public override void UpdateOnceBeforeFrame()
        {
            if (firstrun)
            {
                var info = new List<MyUpgradeModuleInfo>();
                hyperDriveBlock.GetUpgradeList(out info);

                Maxhyper = info.FirstOrDefault(x => x.UpgradeType == "WarpFactor").Modifier;

                ResourceSink.SetMaxRequiredInputByType(_electricity, HyperFunctions.MinimumPowertoActivate());
                ResourceSink.SetRequiredInputByType(_electricity, HyperFunctions.PowerConsumption());
                ResourceSink.SetRequiredInputFuncByType(_electricity, HyperFunctions.PowerConsumption);
                ResourceSink.Update();

                hyperDriveBlock.AppendingCustomInfo += HyperFunctions.hyperDriveBlock_AppendingCustomInfo;

                Maxhyper = Maxhyper * 100f;
                emitter = new Sandbox.Game.Entities.MyEntity3DSoundEmitter(hyperDriveBlock as MyEntity);
                MyEntity3DSoundEmitter.PreloadSound(pair);
                HyperFunctions.CreatehyperUI();

                firstrun = false;

                White = Color.White;
                MyVisualScriptLogicProvider.ScreenColorFadingSetColor(White);
            }
        }

        public override void UpdateAfterSimulation10()
        {
            if (hyperDriveBlock == null || !hyperDriveBlock.InScene || !hyperDriveBlock.CubeGrid.InScene)
                return;

            if (firstrun)
                return;

            if (Maxhyper > 1000f)
            {
                Maxhyper = 1000f;
            }
        }

        public override void UpdateBeforeSimulation()
        {
            // Ignore damaged or build progress hyper_blocks. Ignore ghost grids (projections).
            if (!hyperDriveBlock.IsFunctional || hyperDriveBlock.CubeGrid.Physics == null) return;
            _ticks++;
            _tick++;
            if (!firstrun)
            {
                if (_ticks > 12)
                {
                    MyAPIGateway.Parallel.StartBackground(HyperFunctions.BackGroundChecks);
                    //HyperFunctions._powerPercent = (HyperFunctions._maxPower * 0.6f);
                    HyperFunctions.UpdateGridPower();
                    ResourceSink.Update();
                    hyperDriveBlock.RefreshCustomInfo();
                    _ticks = 0;
                }
                if (MyAPIGateway.Session.IsServer)
                {
                    if (ResourceSink.IsPowerAvailable(_electricity, HyperFunctions.PowerConsumption()))
                    {
                        BubbleFormed = true;
                        _bubbleNotification = true;
                    }
                    else if (!ResourceSink.IsPowerAvailable(_electricity, HyperFunctions.PowerConsumption()))
                    {
                        BubbleFormed = false;
                    }
                    if (_tick > 10000)
                    {
                        _tick = 0;
                        msgSent = false;
                    }
                    if (HyperFunctions.IsWorking() && HyperEngaged)
                    {
                        hyper = true;
                        //var grid = hyperDriveBlock.CubeGrid;
                        if (BubbleFormed && hyperDriveBlock.CubeGrid.Physics != null && hyperDriveBlock.CubeGrid.WorldMatrix != null && hyperDriveBlock.CubeGrid.WorldMatrix.Translation != null && warpTimer < 120)
                        {
                            HyperFunctions.Warp();
                            warpTimer = (warpTimer + 1);

                            if (warpTimer > 95 && fade)
                            {
                                fade = false;
                                var realPlayerIds = new List<long>();
                                DsUtilsStatic.GetRealPlayers(hyperDriveBlock.PositionComp.WorldVolume.Center, 500f, realPlayerIds);
                                foreach (var id in realPlayerIds)
                                {
                                    MyVisualScriptLogicProvider.ShowNotification("Jump Initialisation Success: " + "Maybe", 500, "White", id);
                                    MyVisualScriptLogicProvider.ScreenColorFadingStart(0.25f, true);
                                }
                                jumpOut = true;
                                lastPos = hyperDriveBlock.CubeGrid.GetPosition();
                                from = hyperDriveBlock.WorldMatrix.Translation + hyperDriveBlock.WorldMatrix.Forward;// * 1d;
                                to = hyperDriveBlock.WorldMatrix.Translation + hyperDriveBlock.WorldMatrix.Forward * 2001d;
                                destination = from - to;
                                HyperFunctions.HyperJump();
                            }
                        }
                        else
                        {
                            hyper = false;
                            warpTimer = 0;
                            fade = true;
                            if (jumpOut)
                            {
                                var realPlayerIds = new List<long>();
                                jumpOut = false;
                                hyperSpace = true;
                                DsUtilsStatic.GetRealPlayers(hyperDriveBlock.PositionComp.WorldVolume.Center, 500f, realPlayerIds);
                                foreach (var id in realPlayerIds)
                                {
                                    MyVisualScriptLogicProvider.ShowNotification("Jump Time: " + "Time to Jump to HyperSpace", 19200, "Red", id);
                                    MyVisualScriptLogicProvider.ScreenColorFadingStartSwitch(0.04f);
                                }
                                HyperEngaged = false;
                            }
                        }
                    }
                    if (hyperSpace)
                    {
                        warpTimer = (warpTimer + 1);//replace with HyperJump timer
                        if (warpTimer < 200)
                        {
                            hyperDriveBlock.CubeGrid.Physics.AngularVelocity = Vector3D.Zero;
                            hyperDriveBlock.CubeGrid.Physics.LinearVelocity = Vector3D.Zero;
                        }
                        else
                        {
                            if (warpTimer >= 200 && warpTimer < 225)
                            {
                                var realPlayerIds = new List<long>();
                                DsUtilsStatic.GetRealPlayers(hyperDriveBlock.PositionComp.WorldVolume.Center, 500f, realPlayerIds);
                                foreach (var id in realPlayerIds)
                                {
                                    MyVisualScriptLogicProvider.ScreenColorFadingStart(0.25f, true);
                                }
                            }
                            else
                            {
                                HyperFunctions.HyperJumpDestination(destination, lastPos);
                                hyperSpace = false;
                                jumpIn = true;
                                warpTimer = 0;
                            }
                        }
                    }
                    else if (!hyperSpace && !jumpIn)
                    {
                        if (!BubbleFormed && hyperDriveBlock.Enabled && _tick > 16)
                        {
                            hyper = false;
                            hyperDriveBlock.Enabled = false;
                        }
                        if (!HyperFunctions.IsWorking() && hyper)
                        {
                            hyper = false;
                            HyperFunctions.EmergencyStop();
                        }
                        if (!hyper)
                        {
                            emitter.StopSound(false, true);
                            HyperEngaged = false;
                        }
                    }
                    if (jumpIn)
                    {
                        warpTimer = (warpTimer + 1);//replace with HyperJump timer
                        HyperFunctions.Warp();
                        if (warpTimer > 30)
                        {
                            var realPlayerIds = new List<long>();
                            DsUtilsStatic.GetRealPlayers(hyperDriveBlock.PositionComp.WorldVolume.Center, 500f, realPlayerIds);
                            foreach (var id in realPlayerIds)
                            {
                                MyVisualScriptLogicProvider.ScreenColorFadingStartSwitch(0.04f);
                            }
                            if (warpTimer > 120)
                            {
                                warpTimer = 0;
                                jumpIn = false;
                                HyperEngaged = false;
                            }
                        }
                    }
                }
            }
        }

        public override void MarkForClose()
        {
            hyperDriveBlock.AppendingCustomInfo -= HyperFunctions.hyperDriveBlock_AppendingCustomInfo;
        }

        public void Engage_OnOff()
        {
            if (HyperEngaged)
            {
                HyperEngaged = false;
                hyper = false;
                HyperFunctions._powerPercent = 0f;
                MyEntity3DSoundEmitter.ClearEntityEmitters();
                emitter.StopSound(false, true);
            }
            else if (!HyperEngaged)
            {
                HyperEngaged = true;
                hyper = false;
                emitter.PlaySound(pair);
            }
            //Logging.Logging.Instance.WriteLine("Hyper Jump Disengaged Successfully");
        }
    }
}
 