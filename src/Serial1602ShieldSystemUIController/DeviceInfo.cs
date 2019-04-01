using System;
using System.Collections.Generic;

namespace SerialSystemUI
{
    public class DeviceInfo
    {
        public Dictionary<string, string> Data = new Dictionary<string, string> ();
        public Dictionary<string, string> UpdatedData = new Dictionary<string, string> ();

        public string DeviceName;
        public string DeviceLabel;
        public string DeviceGroup;

        public DeviceInfo ()
        {
        }

        public DeviceInfo (string deviceName)
        {
            DeviceName = deviceName;
        }
    }
}

