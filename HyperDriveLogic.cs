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
using VRage;

namespace HyperDrive
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_UpgradeModule), false, "CX3WarpCore")]
    public class HyperDriveLogic : MyGameLogicComponent
    {
        bool _shellBool = false;

        Vector3D from = new Vector3D();
        Vector3D to = new Vector3D();
        Vector3D lastPos = new Vector3D();
        Vector3D destination = new Vector3D();
        Vector3D exit = new Vector3D();

        MyParticleEffect _effect;

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
        int hyperSpaceTimer = 0;

        Color White = new Color();
        public static bool jumpOut = false;
        public static bool jumpIn = false;
        public static bool hyperSpace = false;
        bool ExitWarning10 = false;
        bool ExitWarning60 = false;


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
                    if (!_shellBool)
                    {
                        try
                        {
                            var parent = (MyEntity)hyperDriveBlock.CubeGrid;
                            Spawn._emptyGridShell = Spawn.EmptyEntity("dShellPassive2", $"{Session.Instance.ModPath()}{Spawn._modelShell}", parent, true);
                            Spawn._emptyGridShell.Render.CastShadows = false;
                            Spawn._emptyGridShell.IsPreview = true;
                            Spawn._emptyGridShell.Render.Visible = true;
                            Spawn._emptyGridShell.Render.RemoveRenderObjects();
                            Spawn._emptyGridShell.Render.UpdateRenderObject(true);
                            Spawn._emptyGridShell.Render.UpdateRenderObject(false);
                            Spawn._emptyGridShell.Save = false;
                            Spawn._emptyGridShell.SyncFlag = false;
                            try
                            {
                                CreateMobileShape();
                                Spawn._emptyGridShell.PositionComp.LocalMatrix = Matrix.Zero;  // Bug - Cannot just change X coord, so I reset first.
                                Spawn._emptyGridShell.PositionComp.LocalMatrix = Spawn._shieldShapeMatrix;
                            }
                            catch
                            {
                                Logging.Logging.Instance.WriteLine("CreateMobileShape() Failure");
                            }
                        }
                        catch
                        {
                            Logging.Logging.Instance.WriteLine("emptyGrid Failure");
                        }
                        
                        _shellBool = true;
                    }
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
                                    //MyVisualScriptLogicProvider.ShowNotification("Jump Initialisation Success: " + "Maybe", 9500, "White", id);
                                    MyVisualScriptLogicProvider.ScreenColorFadingStart(0.25f, true);
                                }
                                lastPos = hyperDriveBlock.CubeGrid.GetPosition();
                                from = hyperDriveBlock.WorldMatrix.Translation + hyperDriveBlock.WorldMatrix.Forward;// * 1d;
                                to = hyperDriveBlock.WorldMatrix.Translation + hyperDriveBlock.WorldMatrix.Forward * 2001d;
                                destination = from - to;
                                hyperSpaceTimer = 7200;
                                //hyperSpaceTimer = (hyperSpaceTimer + distanceVar);
                                jumpOut = true;
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
                                DsUtilsStatic.GetRealPlayers(hyperDriveBlock.PositionComp.WorldVolume.Center, 500f, realPlayerIds);
                                foreach (var id in realPlayerIds)
                                {
                                    //MyVisualScriptLogicProvider.ShowNotification("Jump Time: " + "Time to Jump to HyperSpace", 9500, "White", id);
                                    MyVisualScriptLogicProvider.ScreenColorFadingStartSwitch(0.25f);
                                }
                                hyperSpace = true;
                                HyperEngaged = false;
                                HyperSpaceParticle();
                            }
                        }
                    }
                    if (hyperSpace)
                    {
                        hyperSpaceTimer = (hyperSpaceTimer - 1);

                        try
                        {
                            ShellVisibility(true);
                        }
                        catch
                        {
                            Logging.Logging.Instance.WriteLine("ShellVisibility(true) Failure");
                        }

                        if (hyperSpaceTimer >= 0)
                        {
                            hyperDriveBlock.CubeGrid.Physics.AngularVelocity = Vector3D.Zero;
                            hyperDriveBlock.CubeGrid.Physics.LinearVelocity = Vector3D.Zero;

                            if (!ExitWarning10 && hyperSpaceTimer <= 600)
                            {
                                var realPlayerIds = new List<long>();
                                DsUtilsStatic.GetRealPlayers(hyperDriveBlock.PositionComp.WorldVolume.Center, 500f, realPlayerIds);
                                foreach (var id in realPlayerIds)
                                {
                                    MyVisualScriptLogicProvider.ShowNotification("10 Seconds to Normal Space", 9600, "White", id);
                                }
                                ExitWarning10 = true;
                            }
                            else if (!ExitWarning60 && hyperSpaceTimer <= 3600)
                            {
                                var realPlayerIds = new List<long>();
                                DsUtilsStatic.GetRealPlayers(hyperDriveBlock.PositionComp.WorldVolume.Center, 500f, realPlayerIds);
                                foreach (var id in realPlayerIds)
                                {
                                    MyVisualScriptLogicProvider.ShowNotification("60 Seconds to Normal Space", 9600, "White", id);
                                }
                                ExitWarning60 = true;
                            }
                        }
                        else if (ExitWarning10 && ExitWarning60)
                        {
                            warpTimer = (warpTimer + 1);
                            if (warpTimer < 25)
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
                                ExitWarning10 = false;
                                ExitWarning60 = false;
                                //hyperSpaceTimer = 7200;
                                HyperSpaceParticleStop();
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
                        warpTimer = (warpTimer + 1);
                        HyperFunctions.Warp();
                        try
                        {
                            ShellVisibility(false);
                        }
                        catch
                        {
                            Logging.Logging.Instance.WriteLine("ShellVisibility(false) Failure");
                        }
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

        //Particles

        private void HyperSpaceParticle()
        {
            //var _effect = "WarpDrivePrototype";
            var pos = HyperDriveLogic.hyperDriveBlock.CubeGrid.PositionComp.WorldAABB.Center;
            uint entId = (uint)HyperDriveLogic.hyperDriveBlock.CubeGrid.EntityId;
            Vector3D dir = Vector3D.Normalize(hyperDriveBlock.WorldMatrix.Forward);  // Relative to grid
            MatrixD matrix = MatrixD.CreateFromDir(-dir);


            MyParticlesManager.TryCreateParticleEffect(1701, out _effect, ref matrix, ref pos, entId, true); // 15, 16, 24, 25, 28, (31, 32) 211 215 53
            if (_effect == null) return;
            //_effect.UserEmitterScale = (float)HyperDriveLogic.hyperDriveBlock.CubeGrid.GridSize;// * 30f;
            _effect.UserScale = (float)HyperDriveLogic.hyperDriveBlock.CubeGrid.GridSize * 2f; //1.75f to 2.0f
            matrix.Translation = pos + dir * hyperDriveBlock.CubeGrid.PositionComp.WorldAABB.HalfExtents.AbsMax() * -4f; //???
            _effect.WorldMatrix = matrix;
            _effect.Loop = true;
            _effect.Play();
        }

        public void HyperSpaceParticleStop()
        {
            if (_effect == null) return;
            _effect.Stop();
            _effect.Close(false, true);
            _effect = null;
        }

        //Ellipsoid

        //Shell Entities

        private void CreateMobileShape()
        {
            var shieldSize = hyperDriveBlock.CubeGrid.PositionComp.WorldAABB.HalfExtents * 5f;
            //ShieldSize = shieldSize;
            var mobileMatrix = MatrixD.CreateScale(shieldSize);
            mobileMatrix.Translation = hyperDriveBlock.CubeGrid.PositionComp.LocalVolume.Center;
            Spawn._shieldShapeMatrix = mobileMatrix;
        }

        private void ShellVisibility(bool forceInvisible = false)
        {
            if (forceInvisible)
            {
                Spawn._emptyGridShell.Render.UpdateRenderObject(false);
                return;
            }

            else Spawn._emptyGridShell.Render.UpdateRenderObject(true);
        }
    }
}
 