using System;

namespace Serial1602ShieldSystemUIController
{
    public abstract class BaseMenuItemInfo
    {
        public bool IsEditable;
        public string Label;
        public string Key;
        public string DefaultValue;
        public int MaxValue = 100;
        public int MinValue = 0;

        public BaseMenuItemInfo ()
        {
        }
    }
}

