using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Serial1602ShieldSystemUIController.Tests
{
    public class MockProcessStarter : ProcessStarter
    {
        public List<string> CommandsRun = new List<string> ();
        public string LastCommandRun = "";

        public bool EnableBaseFunctionality;

        public MockProcessStarter ()
        {
        }

        public override Process Start (string command, string arguments)
        {
            CommandsRun.Add (command + " " + arguments);
            LastCommandRun = command + " " + arguments;

            if (EnableBaseFunctionality)
                return base.Start (command, arguments);
            else
                return null;
        }
    }
}

