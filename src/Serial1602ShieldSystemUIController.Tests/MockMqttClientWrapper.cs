﻿using System;
using System.Collections.Generic;

namespace Serial1602ShieldSystemUIController.Tests
{
    public class MockMqttClientWrapper : MqttClientWrapper
    {
        public bool EnableBaseFunctionality = false;

        public List<string> Subscriptions = new List<string> ();
        public List<string> Unsubscriptions = new List<string> ();

        public MockMqttClientWrapper ()
        {
        }

        public override void Connect (string clientId, string username, string password)
        {
            if (EnableBaseFunctionality)
                base.Connect (clientId, username, password);
        }

        public override void Publish (string topic, byte[] value)
        {
            if (EnableBaseFunctionality)
                base.Publish (topic, value);
        }

        public override void Subscribe (string[] topics, byte[] qos)
        {
            if (EnableBaseFunctionality)
                base.Subscribe (topics, qos);

            Subscriptions.AddRange (topics);
        }

        public override void Unsubscribe (string[] topics)
        {
            if (EnableBaseFunctionality)
                base.Unsubscribe (topics);

            Unsubscriptions.AddRange (topics);
        }


    }
}
