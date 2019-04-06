using System;
using System.Collections.Generic;

namespace Serial1602ShieldSystemUIController
{
    public class MenuInfo
    {
        public string Group;

        public Dictionary<string, BaseMenuItemInfo> Items = new Dictionary<string, BaseMenuItemInfo> ();

        public MenuInfo (string group)
        {
            Group = group;
        }
    }
}

