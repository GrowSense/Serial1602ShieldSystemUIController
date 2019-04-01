using System;
using System.Collections.Generic;

namespace SerialSystemUI
{
    public class MenuInfo
    {
        public string Group;

        public Dictionary<string, MenuItemInfo> Items = new Dictionary<string, MenuItemInfo> ();

        public MenuInfo (string group)
        {
            Group = group;
        }
    }
}

