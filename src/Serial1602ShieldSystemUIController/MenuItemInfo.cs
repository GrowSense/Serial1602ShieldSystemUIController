using System;
using System.Collections.Generic;

namespace SerialSystemUI
{
    public class MenuItemInfo
    {
        public string Key;
        public string Label;
        public string PostFix;
        public bool IsEditable;
        public Dictionary<int, string> Options;
        public int MaxValue = 100;
        public int MinValue = 0;

        public MenuItemInfo (string key, string label, string postFix, bool isEditable)
        {
            Key = key;
            Label = label;
            PostFix = postFix;
            IsEditable = isEditable;
        }

        public MenuItemInfo (string key, string label, string postFix, bool isEditable, int minValue, int maxValue)
        {
            Key = key;
            Label = label;
            PostFix = postFix;
            IsEditable = isEditable;
            MinValue = minValue;
            MaxValue = maxValue;
        }

        public MenuItemInfo (string key, string label, string postFix, bool isEditable, Dictionary<int, string> options)
        {
            Key = key;
            Label = label;
            PostFix = postFix;
            IsEditable = isEditable;
            Options = options;
            MinValue = 0;
            MaxValue = Options.Count - 1;
        }
    }
}

