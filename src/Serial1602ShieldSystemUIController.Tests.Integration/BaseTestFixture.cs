using System;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Serial1602ShieldSystemUIController.Tests.Integration
{
    public class BaseTestFixture
    {
        public string ProjectDirectory;
        public string TemporaryDirectory;

        public BaseTestFixture ()
        {
        }

        [SetUp]
        public void Initialize ()
        {
            Console.WriteLine ("");
            Console.WriteLine ("====================");
            Console.WriteLine ("Preparing test");
            Console.WriteLine (TestContext.CurrentContext.Test.FullName);

            InitializeProjectDirectory ();

            MoveToTemporaryDirectory ();


        }

        [TearDown]
        public virtual void Finish ()
        {
            HandleFailureFile ();

            Console.WriteLine ("Finished test");
            Console.WriteLine ("====================");
            Console.WriteLine ("");
        }

        public void InitializeProjectDirectory ()
        {
            ProjectDirectory = Environment.CurrentDirectory;

            ProjectDirectory = ProjectDirectory.Replace ("/bin/Debug", "");
            ProjectDirectory = ProjectDirectory.Replace ("/bin/Release", "");
        }

        public void MoveToProjectDirectory ()
        {
            Directory.SetCurrentDirectory (ProjectDirectory);
        }

        public void MoveToTemporaryDirectory ()
        {
            var tmpDir = Path.Combine (ProjectDirectory, "_tmp");

            if (!Directory.Exists (tmpDir))
                Directory.CreateDirectory (tmpDir);

            var tmpTestDir = Path.Combine (tmpDir, Guid.NewGuid ().ToString ());

            if (!Directory.Exists (tmpTestDir))
                Directory.CreateDirectory (tmpTestDir);

            TemporaryDirectory = tmpTestDir;

            Directory.SetCurrentDirectory (tmpTestDir);
        }

        public void CleanTemporaryDirectory ()
        {
            var tmpDir = Environment.CurrentDirectory;

            Directory.SetCurrentDirectory (ProjectDirectory);

            Console.WriteLine ("Cleaning temporary directory:");
            Console.WriteLine (tmpDir);

            //Directory.Delete (tmpDir, true);
        }


        public void HandleFailureFile ()
        {
            var failuresDir = Path.GetFullPath ("../../failures");

            var fixtureName = TestContext.CurrentContext.Test.FullName;

            var failureFile = Path.Combine (failuresDir, fixtureName + ".txt");

            if (TestContext.CurrentContext.Result.State == TestState.Error
                || TestContext.CurrentContext.Result.State == TestState.Failure) {
                Console.WriteLine ("Test failed.");

                Console.WriteLine (failuresDir);
                Console.WriteLine (fixtureName);
                Console.WriteLine (failureFile);

                if (!Directory.Exists (failuresDir))
                    Directory.CreateDirectory (failuresDir);

                File.WriteAllText (failureFile, fixtureName);
            } else {
                Console.WriteLine ("Test passed.");
                if (File.Exists (failureFile))
                    File.Delete (failureFile);
            }
        }

        public string GetDevicePort ()
        {
            var devicePort = Environment.GetEnvironmentVariable ("MQTT_BRIDGE_EXAMPLE_DEVICE_PORT");

            if (String.IsNullOrEmpty (devicePort))
                devicePort = "/dev/ttyUSB0";

            Console.WriteLine ("Device port: " + devicePort);

            return devicePort;
        }

        public int GetDeviceSerialBaudRate ()
        {
            return 9600;
        }

        public void CreateExampleDevice (string devicesDir, string name, string label, string group, string host)
        {
            var deviceDir = Path.Combine (devicesDir, name);
            Directory.CreateDirectory (deviceDir);

            File.WriteAllText (Path.Combine (deviceDir, "name.txt"), name);
            File.WriteAllText (Path.Combine (deviceDir, "label.txt"), label);
            File.WriteAllText (Path.Combine (deviceDir, "group.txt"), group);
            File.WriteAllText (Path.Combine (deviceDir, "host.txt"), host);
        }
    }
}
