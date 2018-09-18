/*
Copyright © 2016 Leto
This work is free. You can redistribute it and/or modify it under the
terms of the Do What The Fuck You Want To Public License, Version 2,
as published by Sam Hocevar. See http://www.wtfpl.net/ for more details.
*/

using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;

namespace HyperDrive.hyperControl
{

    internal class ButtonhyperControl<T> : BasehyperControl<T>
    {
        public ButtonhyperControl(
            IMyTerminalBlock hyper_block,
            string internalName,
            string title)
            : base(hyper_block, internalName, title)
        {
        }

        public override void OnCreatehyperUI()
        {
            var button = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, T>(InternalName);
            button.Title = VRage.Utils.MyStringId.GetOrCompute(Title);
            button.Action = OnhyperAction;
            button.Enabled = hyperEnabled;
            button.Visible = ShowhyperControl;
            MyAPIGateway.TerminalControls.AddControl<T>(button);
        }

        public virtual void OnhyperAction(IMyTerminalBlock hyper_block)
        {
        }
    }
}