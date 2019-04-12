using System;
using System.Collections.Generic;

namespace Serial1602ShieldSystemUIController
{
    public class CommandMenuItemInfo : BaseMenuItemInfo
    {
        public string PostFix;
        public Dictionary<string, string> Options;

        public string StartedText = "";

        public CommandMenuItemInfo (string key, string label, string postFix, bool isEditable)
        {
            Key = key;
            Label = label;
            PostFix = postFix;
            IsEditable = isEditable;
        }

        public CommandMenuItemInfo (string key, string label, string postFix, bool isEditable, int minValue, int maxValue)
        {
            Key = key;
            Label = label;
            PostFix = postFix;
            IsEditable = isEditable;
            MinValue = minValue;
            MaxValue = maxValue;
        }

        public CommandMenuItemInfo (string key, string label, string postFix, bool isEditable, Dictionary<string, string> options)
        {
            Key = key;
            Label = label;
            PostFix = postFix;
            IsEditable = isEditable;
            Options = options;
            MinValue = 0;
            MaxValue = Options.Count - 1;
            StartedText = label + " started";
        }

        public CommandMenuItemInfo (string key, string label, string postFix, bool isEditable, Dictionary<string, string> options, string defaultValue, string startedText)
        {
            Key = key;
            Label = label;
            PostFix = postFix;
            IsEditable = isEditable;
            Options = options;
            MinValue = 0;
            MaxValue = Options.Count - 1;
            DefaultValue = defaultValue;
            StartedText = startedText;
        }
    }
}

