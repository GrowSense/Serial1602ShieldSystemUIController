using System;
using System.Collections.Generic;

namespace Serial1602ShieldSystemUIController
{
    public class MqttMenuItemInfo : BaseMenuItemInfo
    {
        public string PostFix;
        public Dictionary<int, string> Options;

        public MqttMenuItemInfo (string key, string label, string postFix, bool isEditable)
        {
            Key = key;
            Label = label;
            PostFix = postFix;
            IsEditable = isEditable;
        }

        public MqttMenuItemInfo (string key, string label, string postFix, bool isEditable, string defaultValue)
        {
            Key = key;
            Label = label;
            PostFix = postFix;
            IsEditable = isEditable;
            DefaultValue = defaultValue;
        }

        public MqttMenuItemInfo (string key, string label, string postFix, bool isEditable, int minValue, int maxValue)
        {
            Key = key;
            Label = label;
            PostFix = postFix;
            IsEditable = isEditable;
            MinValue = minValue;
            MaxValue = maxValue;
        }

        public MqttMenuItemInfo (string key, string label, string postFix, bool isEditable, Dictionary<int, string> options)
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

