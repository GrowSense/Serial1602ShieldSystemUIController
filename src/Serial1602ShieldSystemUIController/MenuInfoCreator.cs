using System;
using System.Collections.Generic;

namespace Serial1602ShieldSystemUIController
{
    public class MenuInfoCreator
    {
        public MenuInfoCreator ()
        {
        }

        public void Create (SystemMenuController controller)
        {

            var irrigatorMenuStructure = new MenuInfo ("irrigator");
            irrigatorMenuStructure.Items.Add ("C", new MqttMenuItemInfo ("C", "Moisture", "%", false));
            irrigatorMenuStructure.Items.Add ("I", new MqttMenuItemInfo ("I", "Interval", "s", true));
            irrigatorMenuStructure.Items.Add ("T", new MqttMenuItemInfo ("T", "Threshold", "%", true));
            var pumpOptions = new Dictionary<int, string> ();
            pumpOptions.Add (0, "Off");
            pumpOptions.Add (1, "On");
            pumpOptions.Add (2, "Auto");
            irrigatorMenuStructure.Items.Add ("P", new MqttMenuItemInfo ("P", "Pump", "", true, pumpOptions));
            irrigatorMenuStructure.Items.Add ("R", new MqttMenuItemInfo ("R", "Raw", "", false));
            irrigatorMenuStructure.Items.Add ("D", new MqttMenuItemInfo ("D", "Dry", "", true));
            irrigatorMenuStructure.Items.Add ("W", new MqttMenuItemInfo ("W", "Wet", "", true));
            controller.MenuStructure.Add ("irrigator", irrigatorMenuStructure);

            var monitorMenuStructure = new MenuInfo ("monitor");
            monitorMenuStructure.Items.Add ("C", new MqttMenuItemInfo ("C", "Moisture", "%", false));
            monitorMenuStructure.Items.Add ("I", new MqttMenuItemInfo ("I", "Interval", "s", true));
            monitorMenuStructure.Items.Add ("R", new MqttMenuItemInfo ("R", "Raw", "", false));
            monitorMenuStructure.Items.Add ("D", new MqttMenuItemInfo ("D", "Dry", "", true));
            monitorMenuStructure.Items.Add ("W", new MqttMenuItemInfo ("W", "Wet", "", true));
            controller.MenuStructure.Add ("monitor", monitorMenuStructure);

            var ventilatorMenuStructure = new MenuInfo ("ventilator");
            ventilatorMenuStructure.Items.Add ("A", new MqttMenuItemInfo ("A", "Temp/Hum", "", false));
            ventilatorMenuStructure.Items.Add ("I", new MqttMenuItemInfo ("I", "Interval", "s", true));
            ventilatorMenuStructure.Items.Add ("S", new MqttMenuItemInfo ("S", "MinTemp", "c", true));
            ventilatorMenuStructure.Items.Add ("U", new MqttMenuItemInfo ("U", "MaxTemp", "c", true));
            ventilatorMenuStructure.Items.Add ("G", new MqttMenuItemInfo ("G", "MinHum", "%", true));
            ventilatorMenuStructure.Items.Add ("J", new MqttMenuItemInfo ("J", "MaxHum", "%", true));
            controller.MenuStructure.Add ("ventilator", ventilatorMenuStructure);

            var illuminatorMenuStructure = new MenuInfo ("illuminator");
            illuminatorMenuStructure.Items.Add ("L", new MqttMenuItemInfo ("L", "Light", "%", false));
            illuminatorMenuStructure.Items.Add ("I", new MqttMenuItemInfo ("I", "Interval", "s", true));
            illuminatorMenuStructure.Items.Add ("T", new MqttMenuItemInfo ("T", "Threshold", "%", true));
            illuminatorMenuStructure.Items.Add ("R", new MqttMenuItemInfo ("R", "Raw", "", false));
            illuminatorMenuStructure.Items.Add ("D", new MqttMenuItemInfo ("D", "Dark", "", true));
            illuminatorMenuStructure.Items.Add ("B", new MqttMenuItemInfo ("B", "Bright", "", true));
            controller.MenuStructure.Add ("illuminator", illuminatorMenuStructure);

            var uiMenuStructure = new MenuInfo ("ui");
            //uiMenuStructure.Items.Add ("Z", new MenuItemInfo ("Z", "Version", "", false));
            uiMenuStructure.Items.Add ("Status", new MqttMenuItemInfo ("StatusText", "Status", "", false, "Online"));

            var clearDevicesOptions = new Dictionary<string, string> ();
            clearDevicesOptions.Add ("No", "");
#if DEBUG
            clearDevicesOptions.Add ("Yes", "notify-send resetting");
#else
            clearDevicesOptions.Add ("Yes", "rm " + controller.DevicesDirectory + " -r");
#endif
            uiMenuStructure.Items.Add ("Reset", new CommandMenuItemInfo ("Reset", "Reset", "", true, clearDevicesOptions, "No", "Clearing\ndevices"));

            var rebootOptions = new Dictionary<string, string> ();
            rebootOptions.Add ("No", "");
#if DEBUG
            rebootOptions.Add ("Yes", "notify-send rebooting");
#else
            rebootOptions.Add ("Yes", "reboot now");
#endif
            uiMenuStructure.Items.Add ("Reboot", new CommandMenuItemInfo ("Reboot", "Reboot", "", true, rebootOptions, "No", "Rebooting\ncomputer"));

            controller.MenuStructure.Add ("ui", uiMenuStructure);
        }
    }
}

