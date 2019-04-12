using System;
using System.Collections.Generic;

namespace Serial1602ShieldSystemUIController
{
    public class DeviceFilterMenuItemInfo : BaseCodeMenuItemInfo
    {
        public Dictionary<int, string> Options = new Dictionary<int, string> ();

        public DeviceFilterMenuItemInfo (SystemMenuController controller) : base (controller)
        {
            Label = "Devices";
            Key = "DeviceFilter";
            DefaultValue = "all";
            IsEditable = true;

            Options.Add (0, "all");
            Options.Add (1, "local");

            MaxValue = Options.Count - 1;
        }

        public override void Execute (int optionIndex)
        {
            switch (optionIndex) {
            case 0:
                Controller.ShowLocalDevicesOnly = false;
                Controller.Alerts.Enqueue ("0|Showing\n1|all devices");
                break;
            case 1:
                Controller.ShowLocalDevicesOnly = true;
                Controller.Alerts.Enqueue ("0|Showing\n1|local devices");
                break;
            }

            Controller.ResetDevices ();

            Controller.MenuIndex = 0;
            Controller.DeviceIsSelected = false;
            Controller.HasChanged = true;
        }
    }
}

