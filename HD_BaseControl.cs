using System.Collections.Generic;

using Sandbox.ModAPI;
using VRage.ObjectBuilders;
using Sandbox.ModAPI.Interfaces.Terminal;

namespace HyperDrive.hyperControl
{
    public class BasehyperControl<T>
    {
        public SerializableDefinitionId Definition;
        public string InternalName;
        public string Title;

        public BasehyperControl(
            IMyTerminalBlock hyper_block,
            string internalName,
            string title)
        {
            Definition = hyper_block.BlockDefinition;
            InternalName = internalName + Definition.SubtypeId;
            Title = title;
        }

        public void CreatehyperUI()
        {
            var hyper_controls = new List<IMyTerminalControl>();
            MyAPIGateway.TerminalControls.GetControls<T>(out hyper_controls);
            var hyper_control = hyper_controls.Find((x) => x.Id.ToString() == InternalName);
            if (hyper_control == null)
            {
                OnCreatehyperUI();
            }
        }

        public virtual void OnCreatehyperUI()
        {
        }

        public virtual bool hyperEnabled(IMyTerminalBlock hyper_block)
        {
            return true;
        }

        public virtual bool ShowhyperControl(IMyTerminalBlock hyper_block)
        {
            return hyper_block.BlockDefinition.TypeId == Definition.TypeId &&
                   hyper_block.BlockDefinition.SubtypeId == Definition.SubtypeId;
        }
    }
}
