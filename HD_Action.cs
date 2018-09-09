/*
Copyright © 2016 Leto
This work is free. You can redistribute it and/or modify it under the
terms of the Do What The Fuck You Want To Public License, Version 2,
as published by Sam Hocevar. See http://www.wtfpl.net/ for more details.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Sandbox.ModAPI;
using VRage.ObjectBuilders;
using Sandbox.ModAPI.Interfaces.Terminal;


namespace HyperDrive.hyperControl
{
    public class ControlhyperAction<T>
    {
        public SerializableDefinitionId hyperDefinition;
        public string InternalName;
        public string Name;

        public ControlhyperAction(
            IMyTerminalBlock hyper_block,
            string internalName,
            string name,
            string icon)
        {
            Name = name;
            hyperDefinition = hyper_block.BlockDefinition;
            InternalName = internalName + hyperDefinition.SubtypeId;

            var hyper_controls = new List<IMyTerminalAction>();
            MyAPIGateway.TerminalControls.GetActions<T>(out hyper_controls);
            var hyper_control = hyper_controls.Find((x) => x.Id.ToString() == InternalName);
            if (hyper_control == null)
            {
                var action = MyAPIGateway.TerminalControls.CreateAction<T>(InternalName);
                action.Action = OnhyperAction;
                action.Name = new StringBuilder(Name);
                action.Enabled = hyperVisible;
                action.Writer = hyperWriter;
                action.Icon = icon;
                MyAPIGateway.TerminalControls.AddAction<T>(action);
            }
        }

        public virtual void hyperWriter(IMyTerminalBlock hyper_block, StringBuilder builder)
        {

        }

        public virtual void OnhyperAction(IMyTerminalBlock hyper_block)
        {
        }

        public virtual bool hyperVisible(IMyTerminalBlock hyper_block)
        {
            return hyper_block.BlockDefinition.TypeId == hyperDefinition.TypeId &&
                    hyper_block.BlockDefinition.SubtypeId == hyperDefinition.SubtypeId;
        }
    }
}
