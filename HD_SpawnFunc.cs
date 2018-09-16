using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace HyperDrive.Support
{
    #region Spawn
    internal class Spawn
    {
        //public static string _modelPassive = "";
        public const string _modelShell = "\\Models\\Empties\\emptyGrid.mwm";
        public static MatrixD _shieldShapeMatrix;

        public static MyEntity _emptyGridShell;

        //Shell Entities
        public static MyEntity EmptyEntity(string displayName, string model, MyEntity parent, bool parented = false)
        {
            try
            {
                var myParent = parented ? parent : null;
                var ent = new MyEntity { NeedsWorldMatrix = true };

                ent.Init(new StringBuilder(displayName), model, myParent, null, null);
                ent.Name = $"{parent.EntityId}";
                MyAPIGateway.Entities.AddEntity(ent);
                Logging.Logging.Instance.WriteLine("HD_SpawnFunc: Empty Entity Success");
                return ent;
            }
            catch (Exception ex) { Logging.Logging.Instance.WriteLine($"Exception in EmptyEntity: {ex}"); return null; }
        }

        //Spawn Block
        public static IMyEntity SpawnBlock(string subtypeId, string name, bool isVisible = false, bool hasPhysics = false, bool isStatic = false, bool toSave = false, bool destructible = false, long ownerId = 0)
        {
            try
            {
                CubeGridBuilder.Name = name;
                CubeGridBuilder.CubeBlocks[0].SubtypeName = subtypeId;
                CubeGridBuilder.CreatePhysics = hasPhysics;
                CubeGridBuilder.IsStatic = isStatic;
                CubeGridBuilder.DestructibleBlocks = destructible;
                var ent = MyAPIGateway.Entities.CreateFromObjectBuilder(CubeGridBuilder);

                ent.Flags &= ~EntityFlags.Save;
                ent.Visible = isVisible;
                MyAPIGateway.Entities.AddEntity(ent, true);

                Logging.Logging.Instance.WriteLine("HD_SpawnFunc: Spawn Block Success");

                return ent;
            }
            catch (Exception ex)
            {
                Logging.Logging.Instance.WriteLine($"Exception in Spawn");
                Logging.Logging.Instance.WriteLine($"{ex}");
                return null;
            }
        }

        private static readonly SerializableBlockOrientation EntityOrientation = new SerializableBlockOrientation(Base6Directions.Direction.Forward, Base6Directions.Direction.Up);

        //OBJECTBUILDERS
        private static readonly MyObjectBuilder_CubeGrid CubeGridBuilder = new MyObjectBuilder_CubeGrid()
        {

            EntityId = 0,
            GridSizeEnum = MyCubeSize.Large,
            IsStatic = true,
            Skeleton = new List<BoneInfo>(),
            LinearVelocity = Vector3.Zero,
            AngularVelocity = Vector3.Zero,
            ConveyorLines = new List<MyObjectBuilder_ConveyorLine>(),
            BlockGroups = new List<MyObjectBuilder_BlockGroup>(),
            Handbrake = false,
            XMirroxPlane = null,
            YMirroxPlane = null,
            ZMirroxPlane = null,
            PersistentFlags = MyPersistentEntityFlags2.InScene,
            Name = "ArtificialCubeGrid2",
            DisplayName = "FieldEffect2",
            CreatePhysics = false,
            DestructibleBlocks = false,
            PositionAndOrientation = new MyPositionAndOrientation(Vector3D.Zero, Vector3D.Forward, Vector3D.Up),
            
            CubeBlocks = new List<MyObjectBuilder_CubeBlock>()
            {
                    new MyObjectBuilder_CubeBlock()
                    {
                        EntityId = 0,
                        BlockOrientation = EntityOrientation,
                        SubtypeName = "",
                        Name = "",
                        Min = Vector3I.Zero,
                        Owner = 0,
                        ShareMode = MyOwnershipShareModeEnum.None,
                        DeformationRatio = 0,
                    }
            }
        };
    }
    
    #endregion
}