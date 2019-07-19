using System;
using NUnit.Framework;
using System.IO;

namespace Serial1602ShieldSystemUIController.Tests.Integration
{
    [TestFixture (Category = "Integration")]
    public class SystemMenuControllerRemoveDeviceTestFixture : BaseTestFixture
    {
        [Test]
        public void Test_RemoveDevice ()
        {
            Console.WriteLine (Environment.CurrentDirectory);

            var devicesDirectory = Path.GetFullPath ("devices");
            Directory.CreateDirectory (devicesDirectory);

            var mockSerialClient = new MockSerialClientWrapper ();
            var mockMqttClient = new MockMqttClientWrapper ();
            var mockProcessStarter = new MockProcessStarter ();
            mockProcessStarter.EnableBaseFunctionality = true;

            var controller = new SystemMenuController ();
            controller.DevicesDirectory = devicesDirectory;
            controller.TargetDirectory = Environment.CurrentDirectory;
            controller.Client = mockSerialClient;
            controller.MqttClient = mockMqttClient;
            controller.Starter = mockProcessStarter;
            controller.SelfHostName = "host";

            Console.WriteLine ("Initializing to generate menu structure...");
            controller.Initialize ();

            CreateExampleDevice (devicesDirectory, "ui1", "Ui1", "ui", "host");
            CreateExampleDevice (devicesDirectory, "irrigator1", "Irrigator1", "irrigator", "host");
            CreateExampleDevice (devicesDirectory, "irrigator2", "Irrigator2", "irrigator", "remote");

            Console.WriteLine ("Running a loop to detect UI device...");
            controller.RunLoop ();

            Assert.AreEqual (3, controller.DeviceList.Count, "The device wasn't added to the list.");

            Assert.AreEqual (0, controller.MenuIndex, "The menu index doesn't match.");
            controller.Alerts.Clear ();

            Console.WriteLine ("Device output:");
            Console.WriteLine (mockSerialClient.Output);
            mockSerialClient.ClearOutput ();

            Console.WriteLine ("Setting menu index to the UI device...");
            controller.MenuIndex = 1;
            controller.SubMenuIndex = 7;
            controller.DeviceIsSelected = true;

            Console.WriteLine ("Running a loop to update display...");
            controller.RunLoop ();

            Assert.IsTrue (mockSerialClient.Output.Contains ("Irrigator2*"), "Didn't select the correct device.");
            Assert.IsTrue (mockSerialClient.Output.Contains ("Remove no"), "Didn't select the correct sub menu option.");

            Console.WriteLine ("Device output:");
            Console.WriteLine (mockSerialClient.Output);
            mockSerialClient.ClearOutput ();

            Console.WriteLine ("Pressing down button to change to yes...");
            controller.PressMenuDown ();

            Console.WriteLine ("Running a loop to update display...");
            controller.RunLoop ();

            Assert.IsTrue (mockSerialClient.Output.Contains ("Remove yes"), "Didn't change to yes as expected.");

            Console.WriteLine ("Device output:");
            Console.WriteLine (mockSerialClient.Output);
            mockSerialClient.ClearOutput ();

            Console.WriteLine ("Creating remove script...");
            var removeScriptName = "remove-garden-device.sh";
            var removeScriptContent = "rm -vr " + controller.DevicesDirectory + "/$1";

            File.WriteAllText (Path.Combine (controller.TargetDirectory, removeScriptName), removeScriptContent);

            Console.WriteLine ("Pressing select button to execute the removal...");
            controller.PressMenuSelect ();

            Console.WriteLine ("Device output:");
            Console.WriteLine (mockSerialClient.Output);
            mockSerialClient.ClearOutput ();

            Console.WriteLine ("Running a loop to update display...");
            controller.RunLoop ();

            Console.WriteLine ("Device output:");
            Console.WriteLine (mockSerialClient.Output);
            mockSerialClient.ClearOutput ();

            var expectedCommand = "cd " + controller.TargetDirectory + " && sh " + removeScriptName + " irrigator2";

            Assert.AreEqual (expectedCommand, mockProcessStarter.LastCommandRun, "Invalid command.");

            Assert.IsFalse (controller.DeviceList.ContainsKey ("irrigator2"), "The device wasn't removed.");

            Assert.AreEqual (0, controller.MenuIndex, "Invalid menu index.");
            Assert.AreEqual (0, controller.SubMenuIndex, "Invalid sub menu index.");

            Console.WriteLine ("Running a loop to detect changes...");
            controller.RunLoop ();

            Console.WriteLine ("Device output:");
            Console.WriteLine (mockSerialClient.Output);
            mockSerialClient.ClearOutput ();
        }
    }
}

