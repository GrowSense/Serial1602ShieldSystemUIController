using System;
using NUnit.Framework;
using System.IO;

namespace Serial1602ShieldSystemUIController.Tests.Integration
{
    [TestFixture (Category = "Integration")]
    public class SystemMenuControllerDeviceFilterTestFixture : BaseTestFixture
    {
        [Test]
        public void Test_NavigateToAndEnableShowOnlyLocalDevices ()
        {

            Console.WriteLine (Environment.CurrentDirectory);

            var devicesDirectory = Path.GetFullPath ("devices");
            Directory.CreateDirectory (devicesDirectory);

            var mockClient = new MockSerialClientWrapper ();
            var mockMqttClient = new MockMqttClientWrapper ();

            var controller = new SystemMenuController ();
            controller.DevicesDirectory = devicesDirectory;
            controller.Client = mockClient;
            controller.MqttClient = mockMqttClient;
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
            Console.WriteLine (mockClient.Output);
            mockClient.ClearOutput ();

            Console.WriteLine ("Setting menu index to the UI device...");
            controller.MenuIndex = 2;

            Console.WriteLine ("Pressing down button to select the UI device...");
            controller.PressMenuDown ();

            Console.WriteLine ("Running a loop to update display...");
            controller.RunLoop ();

            Console.WriteLine ("Device output:");
            Console.WriteLine (mockClient.Output);
            mockClient.ClearOutput ();

            Console.WriteLine ("Pressing right button to move to next option...");
            controller.MenuRight ();

            Console.WriteLine ("Running a loop to update display...");
            controller.RunLoop ();

            Assert.IsTrue (mockClient.Output.Contains ("Devices all"), "Output is invalid.");

            Console.WriteLine ("Device output:");
            Console.WriteLine (mockClient.Output);
            mockClient.ClearOutput ();

            Console.WriteLine ("Pressing up button to change to local devices only...");
            controller.PressMenuUp ();

            Console.WriteLine ("Running a loop to update display...");
            controller.RunLoop ();

            Assert.IsTrue (mockClient.Output.Contains ("Devices local"), "Output is invalid.");

            Console.WriteLine ("Device output:");
            Console.WriteLine (mockClient.Output);
            mockClient.ClearOutput ();

            Console.WriteLine ("Pressing select button to submit the change...");
            controller.PressMenuSelect ();

            Console.WriteLine ("Running a loop to update display...");
            controller.RunLoop ();

            Console.WriteLine ("Device output:");
            Console.WriteLine (mockClient.Output);
            mockClient.ClearOutput ();

            Assert.IsTrue (controller.ShowLocalDevicesOnly, "The ShowLocalDevicesOnly property wasn't updated.");

            Assert.AreEqual (2, controller.DeviceList.Count, "The device list count is incorrect. The remote device shouldn't have been loaded.");

            Assert.AreEqual (0, controller.MenuIndex, "The menu index should have been reset.");
            Assert.IsFalse (controller.DeviceIsSelected, "The device should have been deselected after reload.");

            Console.WriteLine ("Running a loop to update display...");
            controller.RunLoop ();

            Console.WriteLine ("Device output:");
            Console.WriteLine (mockClient.Output);
            mockClient.ClearOutput ();
        }

    }
}

