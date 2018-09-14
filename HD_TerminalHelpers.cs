using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.Entity;
using VRageMath;
using VRage.Game.Components;

namespace HyperDrive.Support
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class Session : MySessionComponentBase
    {
        public static Session Instance { get; private set; }

        public override void UpdateBeforeSimulation()
        {
            /*if (HyperDriveLogic.hyperDriveBlock.Enabled)
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
            }*/
        }
    }
}
