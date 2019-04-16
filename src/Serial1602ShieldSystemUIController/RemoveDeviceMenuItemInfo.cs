using System;
using System.Collections.Generic;

namespace Serial1602ShieldSystemUIController
{
    public class RemoveDeviceMenuItemInfo : BashCommandMenuItemInfo
    {
        public RemoveDeviceMenuItemInfo () : base ("Remove", "Remove", "", true)
        {
            Label = "Remove";
            Key = "Remove";

            DefaultValue = "no";
            IsEditable = true;

            Options.Add ("no", "");
            Options.Add ("yes", "sh remove-garden-device.sh {DEVICE_NAME}");

            MaxValue = Options.Count - 1;
        }
    }
}

