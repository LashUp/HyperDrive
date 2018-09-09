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

        public static HyperDrive.hyperControl.ButtonhyperControl<Sandbox.ModAPI.Ingame.IMyUpgradeModule> engageButton;
        public static HyperDrive.hyperControl.ControlhyperAction<Sandbox.ModAPI.Ingame.IMyUpgradeModule> ActionEngage;
        public static MyEntity3DSoundEmitter emitter;
        public static MySoundPair pair = new MySoundPair("Hyper");

        public static bool hyper = false;
        public static bool BubbleFormed = true;
        public static float hyperFactor = 0f;
        public static float Maxhyper = 0f;
        public static float WF_Scale = 0f;
        public static float WF_Curve = 0f;
        public static double totalMass = 0;
        public static float totalMassF = 0f;
        public static double _minSpeed;

        int _ticks = 132000;
        private uint _tick;
        int warpTimer = 0;

        Color White = new Color();


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
            /*
            //RemoteControl
            List<IMySlimBlock> _termBlocks = new List<IMySlimBlock>();
            hyperDriveBlock.CubeGrid.GetBlocks(_termBlocks);
            var termBlocks = _termBlocks.Where(x => x.FatBlock is IMyTerminalBlock).ToList();

            foreach (var controller in termBlocks)
            {
                if (controller.FatBlock is IMyRemoteControl)
                {


                    //if (controller.BuildIntegrity > 0.8)
                    //{
                        remoteControl = termBlocks[0] as IMyRemoteControl;

                        //shipForward = remoteControl.WorldMatrix.Forward;


                        //shipForward = remoteControl.LocalMatrix.Forward;
                    //}

                    //if (remoteControl == null)
                    //{

                        //scriptInitFailed = true;
                        //return;

                    //}
                }
            }*/
        }

        public override void UpdateBeforeSimulation()
        {
            // Ignore damaged or build progress hyper_blocks. Ignore ghost grids (projections).
            if (!hyperDriveBlock.IsFunctional || hyperDriveBlock.CubeGrid.Physics == null) return;
            _ticks++;
            _tick++;
            HyperFunctions.UpdateGridPower();
            ResourceSink.Update();
            hyperDriveBlock.RefreshCustomInfo();

            if (!firstrun)
            {
                if (_ticks > 12)
                {
                    HyperFunctions.RefreshLists();
                    HyperFunctions.Distance();
                    totalMass = Math.Sqrt(hyperDriveBlock.CubeGrid.Physics.Mass);
                    totalMassF = (float)totalMass;
                    HyperFunctions.curPDistF = (float)HyperFunctions.distance;
                    MyAPIGateway.Parallel.StartBackground(HyperFunctions.BackGroundChecks);
                    HyperFunctions._powerPercent = (HyperFunctions._maxPower * 0.6f);
                    
                    if (HyperFunctions.curPDistF < HyperFunctions.maxPDistF)
                    {
                        if (HyperFunctions._powerPercent >= 100f && HyperFunctions._powerPercent <= Maxhyper)
                        {
                            hyperFactor = HyperFunctions._powerPercent;
                            float invSqr = (((HyperFunctions.maxPDistF * HyperFunctions.maxPDistF) / (HyperFunctions.curPDistF * HyperFunctions.curPDistF)) * 0.01f);
                            if (invSqr < 0.95f)
                            {
                                hyperFactor = (hyperFactor - (hyperFactor * invSqr));
                                gravMsg = true;
                            }
                            else if (invSqr >= 0.95f)
                            {
                                hyperFactor = 0f;
                                Engage_OnOff();                                
                                if (gravMsg)
                                {
                                    var realPlayerIds = new List<long>();
                                    DsUtilsStatic.GetRealPlayers(hyperDriveBlock.PositionComp.WorldVolume.Center, 500f, realPlayerIds);
                                    foreach (var id in realPlayerIds)
                                    {
                                        MyVisualScriptLogicProvider.ShowNotification("Gravity Field Interference: " + "Minimum Safe Distance - 200km", 19200, "Red", id);
                                    }
                                    gravMsg = false;
                                }
                            }
                        }
                    }
                    else if (HyperFunctions.curPDistF > HyperFunctions.maxPDistF)
                    {
                        if (HyperFunctions._powerPercent >= 100f && HyperFunctions._powerPercent <= Maxhyper)
                        {
                            hyperFactor = HyperFunctions._powerPercent;
                        }
                    }

                    //Convert Mass Double to non-Infinite Float//
                    if (float.IsPositiveInfinity(totalMassF))
                    {
                        totalMassF = float.MaxValue;
                    }
                    else if (float.IsNegativeInfinity(totalMassF))
                    {
                        totalMassF = float.MinValue;
                    }
                    if (float.IsPositiveInfinity(HyperFunctions.curPDistF))
                    {
                        HyperFunctions.curPDistF = float.MaxValue;
                    }
                    else if (float.IsNegativeInfinity(HyperFunctions.curPDistF))
                    {
                        HyperFunctions.curPDistF = float.MinValue;
                    }

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
                    if (HyperFunctions.IsWorking() && hyperDriveBlock.CubeGrid.Physics.LinearVelocity.LengthSquared() >= 0f && HyperEngaged)
                    {
                        hyper = true;
                        var grid = hyperDriveBlock.CubeGrid;
                        //_minSpeed = hyperDriveBlock.CubeGrid.Physics.LinearVelocity.LengthSquared();

                        //if (_minSpeed >= 5000)
                        //{
                            if (BubbleFormed && hyperDriveBlock.CubeGrid.Physics != null && hyperDriveBlock.CubeGrid.WorldMatrix != null && hyperDriveBlock.CubeGrid.WorldMatrix.Translation != null && warpTimer < 300)
                            {
                            //remoteControl;


                            //hyperDriveBlock.SetLocalMatrix(hyperDriveBlock.LocalMatrix, shipForward);

                            //hyperDriveBlock.CubeGrid.SetLocalMatrix(hyperDriveBlock.CubeGrid.LocalMatrix, hyperDriveBlock.Orientation.Forward);
                            //var fwd = hyperDriveBlock.CubeGrid.LocalMatrix.GetDirectionVector(hyperDriveBlock.Orientation.Forward);

                                from = hyperDriveBlock.WorldMatrix.Translation + hyperDriveBlock.WorldMatrix.Forward;// * 1d;
                                to = hyperDriveBlock.WorldMatrix.Translation + hyperDriveBlock.WorldMatrix.Forward * 50d;

                                var gridSpeed = hyperDriveBlock.CubeGrid.WorldMatrix;
                                var gridTranslation = hyperDriveBlock.CubeGrid.WorldMatrix.Translation;
                                var gPhysics = hyperDriveBlock.CubeGrid.Physics;
                                var gameSteps = MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                                var predictedMatrix = hyperDriveBlock.CubeGrid.WorldMatrix;
                                var multipler = 500;
                                predictedMatrix.Translation = gridTranslation + gPhysics.GetVelocityAtPoint(gridTranslation) * gameSteps * multipler;
                                //gPhysics.LinearVelocity = (fwd );
                                gPhysics.LinearVelocity = from - to;

                                //object not set remoteControl.DampenersOverride = false;
                                //grid.Physics.SetSpeeds(gPhysics.LinearVelocity, Vector3.Zero);
                                gPhysics.AngularVelocity = Vector3D.Zero;
                                grid.Teleport(predictedMatrix);
                                gPhysics.AngularVelocity = Vector3D.Zero;
                            warpTimer = (warpTimer + 1);

                            if (warpTimer > 250 && fade)
                            {
                                fade = false;
                                var realPlayerIds = new List<long>();
                                DsUtilsStatic.GetRealPlayers(hyperDriveBlock.PositionComp.WorldVolume.Center, 500f, realPlayerIds);
                                foreach (var id in realPlayerIds)
                                {
                                    //MyVisualScriptLogicProvider.ShowNotification("Jump Initialisation Success: " + "Maybe", 19200, "White", id);
                                    MyVisualScriptLogicProvider.ScreenColorFadingStart(0.25f, true);
                                }
                            }



                            //var setSpeed = hyperDriveBlock.CubeGrid.Physics.LinearVelocity;
                            //remoteControl.WorldMatrix.SetDirectionVector(remoteControl.Orientation.Forward, shipForward);


                            /* Original Code
                            var gridSpeed = hyperDriveBlock.CubeGrid.WorldMatrix;
                            var gridTranslation = hyperDriveBlock.CubeGrid.WorldMatrix.Translation;
                            var gPhysics = hyperDriveBlock.CubeGrid.Physics;
                            var gameSteps = MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                            var predictedMatrix = hyperDriveBlock.CubeGrid.WorldMatrix;
                            var multipler = WF_Scale;
                            predictedMatrix.Translation = gridTranslation + gPhysics.GetVelocityAtPoint(gridTranslation) * gameSteps * multipler;
                            gPhysics.AngularVelocity = Vector3D.Zero;
                            grid.Teleport(predictedMatrix);
                            gPhysics.AngularVelocity = Vector3D.Zero;*/
                        }
                        //}
                        else
                        {
                            hyper = false;
                            warpTimer = 0;
                            fade = true;
                            var realPlayerIds = new List<long>();
                            DsUtilsStatic.GetRealPlayers(hyperDriveBlock.PositionComp.WorldVolume.Center, 500f, realPlayerIds);
                            foreach (var id in realPlayerIds)
                            {
                                MyVisualScriptLogicProvider.ShowNotification("Jump Time: " + "Time to Jump to HyperSpace", 19200, "Red", id);
                                MyVisualScriptLogicProvider.ScreenColorFadingStartSwitch(0.1f);
                            }

                            //Color White = new Color();
                            //White = Color.White;
                            //MyVisualScriptLogicProvider.ScreenColorFadingSetColor(White);
                            //MyVisualScriptLogicProvider.ScreenColorFadingStart(4, true);
                            //MyVisualScriptLogicProvider.ScreenColorFadingStartSwitch(1);
                        }
                    }
                    if (!BubbleFormed && hyperDriveBlock.Enabled && _tick > 16)
                    {
                        var realPlayerIds = new List<long>();
                        DsUtilsStatic.GetRealPlayers(hyperDriveBlock.PositionComp.WorldVolume.Center, 500f, realPlayerIds);
                        foreach (var id in realPlayerIds)
                        {
                            MyVisualScriptLogicProvider.ShowNotification("hyper Field Collapse Imminent: " + "Emergency Shutdown" + "\nInsufficient Power - You require additional pylons.", 19200, "Red", id);
                        }
                        WF_Scale = 0f;
                        hyper = false;
                        hyperDriveBlock.Enabled = false;
                    }
                    if (!HyperFunctions.IsWorking() && hyper)
                    {
                        var realPlayerIds = new List<long>();
                        DsUtilsStatic.GetRealPlayers(hyperDriveBlock.PositionComp.WorldVolume.Center, 500f, realPlayerIds);
                        foreach (var id in realPlayerIds)
                        {
                            MyVisualScriptLogicProvider.ShowNotification("hyper Drive Manually Disabled: Emergency Stop", 19200, "White", id);
                        }
                        WF_Scale = 0f;
                        hyper = false;
                        HyperFunctions.EmergencyStop();
                    }

                    if (hyper)
                    {
                        if (WF_Scale < hyperFactor && WF_Scale >= 0f)
                        {
                            float WF_CurveM = (hyperFactor - hyperFactor * 0.05f);
                            if (WF_Scale < WF_CurveM)
                            {
                                WF_Curve = (hyperFactor * 0.0001f);
                                WF_Scale = (WF_Curve + (WF_Scale + (WF_Scale * 0.0025f)));
                            }
                            else if (WF_Scale > WF_CurveM)
                            {
                                WF_Scale = hyperFactor;
                            }
                        }
                        else if (WF_Scale > hyperFactor && WF_Scale >= 0f)
                        {
                            float WF_CurveP = (hyperFactor * 0.05f + hyperFactor);
                            if (WF_Scale > WF_CurveP)
                            {
                                WF_Scale--;
                            }
                            if (WF_Scale < WF_CurveP)
                            {
                                WF_Scale = hyperFactor;
                            }
                        }
                        else if (WF_Scale == hyperFactor && WF_Scale > 0.1f)
                        {
                            if (hyperFactor > 30f && hyper && !msgSent)
                            {
                                var realPlayerIds = new List<long>();
                                DsUtilsStatic.GetRealPlayers(hyperDriveBlock.PositionComp.WorldVolume.Center, 500f, realPlayerIds);

                                foreach (var id in realPlayerIds)
                                {
                                    MyVisualScriptLogicProvider.ShowNotification("Target hyper Achieved", 19200, "White", id);
                                    msgSent = true;
                                }
                            }
                        }
                        else if (WF_Scale < 0f)
                        {
                            hyperFactor = (hyperFactor * -1f);
                            WF_Scale = hyperFactor;
                            hyper = false;
                        }
                    }
                    if (!hyper)
                    {
                        emitter.StopSound(false, true);
                        if (WF_Scale <= 0f)
                        {
                            hyperFactor = 0f;
                            WF_Scale = hyperFactor;
                            HyperEngaged = false;
                        }
                        else if (WF_Scale > 0f)
                        {
                            hyperFactor = 0f;
                            WF_Scale = hyperFactor;
                            HyperEngaged = false;
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
                hyperFactor = 0f;
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
            Logging.Logging.Instance.WriteLine("Hyper (Dis)Engaged Successfully");
        }
    }
}
 