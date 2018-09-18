using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.Entity;
using VRageMath;
using VRage.Game.Components;
using VRage.Game;

namespace HyperDrive.Support
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class Session : MySessionComponentBase
    {
        public new readonly MyModContext ModContext = new MyModContext();

        public static Session Instance { get; private set; }

        //bool init = false;

        public string ModPath()
        {
            var modPath = ModContext.ModPath;
            return modPath;
        }

        //Shell Entities

        public void CreateMobileShape()
        {
            var shieldSize = HyperDriveLogic.hyperDriveBlock.CubeGrid.PositionComp.WorldAABB.HalfExtents * 5f;
            //ShieldSize = shieldSize;
            var mobileMatrix = MatrixD.CreateScale(shieldSize);
            mobileMatrix.Translation = HyperDriveLogic.hyperDriveBlock.CubeGrid.PositionComp.LocalVolume.Center;
            Spawn._shieldShapeMatrix = mobileMatrix;
        }

        public void ShellVisibility(bool forceInvisible = false)
        {
            if (forceInvisible)
            {
                Spawn._emptyGridShell.Render.UpdateRenderObject(false);
                return;
            }

            else Spawn._emptyGridShell.Render.UpdateRenderObject(true);
        }

        /*public override void UpdateBeforeSimulation()
        {


            if (HyperDriveLogic.hyperDriveBlock.Enabled)
            {
                //var viewDistance = Session.SessionSettings.ViewDistance;
                var viewSphere = new BoundingSphereD(MyAPIGateway.Session.Player.GetPosition(), 50000);
                var entList = new List<MyEntity>();
                MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref viewSphere, entList);

                foreach (var v in entList)
                {
                    if (v == HyperDriveLogic.hyperDriveBlock.CubeGrid) continue;
                    if (HyperDriveLogic.hyper && HyperDriveLogic.hyperFactor > 30)
                    {
                        if (v is MyVoxelBase)
                        {
                            var myBase = v as MyVoxelBase;
                            if (myBase.ContentChanged) continue;
                            v.Physics.Enabled = false;
                            v.Render.UpdateRenderObject(false);
                        }
                        else v.Render.UpdateRenderObject(false);
                    }
                    else if (!HyperDriveLogic.hyper)
                    {
                        if (v is MyVoxelBase) v.Physics.Enabled = true;
                        v.Render.UpdateRenderObject(true);
                    }
                }
            }
        }*/
    }
}
