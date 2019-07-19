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
            irrigatorMenuStructure.Items.Add ("D", new MqttMenuItemInfo ("D", "Dry", "", true, 0, 1024));
            irrigatorMenuStructure.Items.Add ("W", new MqttMenuItemInfo ("W", "Wet", "", true, 0, 1024));
            irrigatorMenuStructure.Items.Add ("Remove", new RemoveDeviceMenuItemInfo ());
            controller.MenuStructure.Add ("irrigator", irrigatorMenuStructure);

            var monitorMenuStructure = new MenuInfo ("monitor");
            monitorMenuStructure.Items.Add ("C", new MqttMenuItemInfo ("C", "Moisture", "%", false));
            monitorMenuStructure.Items.Add ("I", new MqttMenuItemInfo ("I", "Interval", "s", true));
            monitorMenuStructure.Items.Add ("R", new MqttMenuItemInfo ("R", "Raw", "", false));
            monitorMenuStructure.Items.Add ("D", new MqttMenuItemInfo ("D", "Dry", "", true, 0, 1024));
            monitorMenuStructure.Items.Add ("W", new MqttMenuItemInfo ("W", "Wet", "", true, 0, 1024));
            monitorMenuStructure.Items.Add ("Remove", new RemoveDeviceMenuItemInfo ());
            controller.MenuStructure.Add ("monitor", monitorMenuStructure);

            var ventilatorMenuStructure = new MenuInfo ("ventilator");
            ventilatorMenuStructure.Items.Add ("A", new MqttMenuItemInfo ("A", "Temp/Hum", "", false));
            ventilatorMenuStructure.Items.Add ("I", new MqttMenuItemInfo ("I", "Interval", "s", true));
            var fanOptions = new Dictionary<int, string> ();
            fanOptions.Add (0, "Off");
            fanOptions.Add (1, "On");
            fanOptions.Add (2, "Auto");
            ventilatorMenuStructure.Items.Add ("F", new MqttMenuItemInfo ("F", "Fan", "", true, fanOptions));
            ventilatorMenuStructure.Items.Add ("S", new MqttMenuItemInfo ("S", "MinTemp", "c", true));
            ventilatorMenuStructure.Items.Add ("U", new MqttMenuItemInfo ("U", "MaxTemp", "c", true));
            ventilatorMenuStructure.Items.Add ("G", new MqttMenuItemInfo ("G", "MinHum", "%", true));
            ventilatorMenuStructure.Items.Add ("J", new MqttMenuItemInfo ("J", "MaxHum", "%", true));
            ventilatorMenuStructure.Items.Add ("Remove", new RemoveDeviceMenuItemInfo ());
            controller.MenuStructure.Add ("ventilator", ventilatorMenuStructure);

            var illuminatorMenuStructure = new MenuInfo ("illuminator");
            illuminatorMenuStructure.Items.Add ("L", new MqttMenuItemInfo ("L", "Light", "%", false));
            var lightModeOptions = new Dictionary<int, string> ();
            lightModeOptions.Add (0, "Off");
            lightModeOptions.Add (1, "On");
            lightModeOptions.Add (2, "Above Threshold");
            lightModeOptions.Add (3, "Below Threshold");
            lightModeOptions.Add (4, "PWM");
            lightModeOptions.Add (5, "Supplement");
            lightModeOptions.Add (6, "Timer");
            illuminatorMenuStructure.Items.Add ("M", new MqttMenuItemInfo ("M", "Mode", "", true, lightModeOptions));
            illuminatorMenuStructure.Items.Add ("I", new MqttMenuItemInfo ("I", "Interval", "s", true));
            illuminatorMenuStructure.Items.Add ("T", new MqttMenuItemInfo ("T", "Threshold", "%", true));
            illuminatorMenuStructure.Items.Add ("R", new MqttMenuItemInfo ("R", "Raw", "", false));
            illuminatorMenuStructure.Items.Add ("D", new MqttMenuItemInfo ("D", "Dark", "", true, 0, 1024));
            illuminatorMenuStructure.Items.Add ("B", new MqttMenuItemInfo ("B", "Bright", "", true, 0, 1024));
            illuminatorMenuStructure.Items.Add ("E", new MqttMenuItemInfo ("E", "Start Hour", "", true, 0, 23));
            illuminatorMenuStructure.Items.Add ("F", new MqttMenuItemInfo ("F", "Start Minute", "", true, 0, 59));
            illuminatorMenuStructure.Items.Add ("G", new MqttMenuItemInfo ("G", "Stop Hour", "", true, 0, 23));
            illuminatorMenuStructure.Items.Add ("H", new MqttMenuItemInfo ("H", "Stop Minute", "", true, 0, 59));
            illuminatorMenuStructure.Items.Add ("Remove", new RemoveDeviceMenuItemInfo ());
            controller.MenuStructure.Add ("illuminator", illuminatorMenuStructure);

            var uiMenuStructure = new MenuInfo ("ui");
            uiMenuStructure.Items.Add ("Status", new MqttMenuItemInfo ("StatusText", "Status", "", false, "Online"));

            uiMenuStructure.Items.Add ("Devices", new DeviceFilterMenuItemInfo (controller));

            /* var upgradeOptions = new Dictionary<string, string> ();
            upgradeOptions.Add ("No", "");
#if DEBUG
            upgradeOptions.Add ("Yes", "notify-send upgrading");
#else
            upgradeOptions.Add ("Yes", "sh upgrade.sh");
#endif*/
            uiMenuStructure.Items.Add ("Upload sketch", new UploadSketchMenuItemInfo ());

            var upgradeOptions = new Dictionary<string, string> ();
            upgradeOptions.Add ("No", "");
#if DEBUG
            upgradeOptions.Add ("Yes", "notify-send upgrading");
#else
            upgradeOptions.Add ("Yes", "sh upgrade.sh");
#endif
            uiMenuStructure.Items.Add ("Upgrade", new BashCommandMenuItemInfo ("Upgrade", "Upgrade", "", true, upgradeOptions, "No", "Upgrading"));

            var reinstallOptions = new Dictionary<string, string> ();
            reinstallOptions.Add ("No", "");
#if DEBUG
            reinstallOptions.Add ("Yes", "notify-send reinstalling");
#else
            reinstallOptions.Add ("Yes", "cd scripts-web && sh reinstall-plug-and-play-from-web.sh");
#endif
            uiMenuStructure.Items.Add ("Reinstall", new BashCommandMenuItemInfo ("Reinstall", "Reinstall", "", true, reinstallOptions, "No", "Reinstalling"));

            var clearDevicesOptions = new Dictionary<string, string> ();
            clearDevicesOptions.Add ("No", "");
#if DEBUG
            clearDevicesOptions.Add ("Yes", "notify-send resetting");
#else
            clearDevicesOptions.Add ("Yes", "sh clean-devices.sh");
#endif
            uiMenuStructure.Items.Add ("Clean", new BashCommandMenuItemInfo ("Clean", "Clean", "", true, clearDevicesOptions, "No", "Cleaning\ndevices"));

            var rebootOptions = new Dictionary<string, string> ();
            rebootOptions.Add ("No", "");
#if DEBUG
            rebootOptions.Add ("Yes", "notify-send rebooting");
#else
            rebootOptions.Add ("Yes", "reboot now");
#endif
            uiMenuStructure.Items.Add ("Reboot", new BashCommandMenuItemInfo ("Reboot", "Reboot", "", true, rebootOptions, "No", "Rebooting\ncomputer"));
            uiMenuStructure.Items.Add ("V", new MqttMenuItemInfo ("V", "Version", "", false));
            uiMenuStructure.Items.Add ("Remove", new RemoveDeviceMenuItemInfo ());
            controller.MenuStructure.Add ("ui", uiMenuStructure);

        }
    }
}

