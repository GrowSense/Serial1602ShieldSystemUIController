using System;
using NUnit.Framework;
using System.IO;

namespace Serial1602ShieldSystemUIController.Tests.Integration
{
    [TestFixture (Category = "Integration")]
    public class SystemMenuControllerNavigationTestFixture : BaseTestFixture
    {
        [Test]
        public void Test_NavigateRightThroughDevices ()
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

            CreateExampleDevice (devicesDirectory, "irrigator1", "Irrigator1", "irrigator", "host");
            CreateExampleDevice (devicesDirectory, "illuminator1", "Illuminator1", "illuminator", "host");
            CreateExampleDevice (devicesDirectory, "ventilator1", "Ventilator1", "ventilator", "host");

            // Run a loop to detect all the devices
            controller.RunLoop ();

            Assert.AreEqual (3, controller.DeviceList.Count, "The device wasn't added to the list.");

            Assert.AreEqual (0, controller.MenuIndex, "The menu index doesn't match.");

            // Clear all alerts so they're skipped
            controller.Alerts.Clear ();

            for (int i = 1; i < 3; i++) {
                controller.MenuRight ();

                Assert.AreEqual (i, controller.MenuIndex, "The menu index doesn't match.");
            }

            controller.MenuRight ();

            Assert.AreEqual (0, controller.MenuIndex, "The menu index didn't go back to 0 as it should have.");

        }

        [Test]
        public void Test_NavigateLeftThroughDevices ()
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

            CreateExampleDevice (devicesDirectory, "irrigator1", "Irrigator1", "irrigator", "host");
            CreateExampleDevice (devicesDirectory, "illuminator1", "Illuminator1", "illuminator", "host");
            CreateExampleDevice (devicesDirectory, "ventilator1", "Ventilator1", "ventilator", "host");

            // Run a loop to detect all the devices
            controller.RunLoop ();

            Assert.AreEqual (3, controller.DeviceList.Count, "The device wasn't added to the list.");

            Assert.AreEqual (0, controller.MenuIndex, "The menu index doesn't match.");

            // Clear all alerts so they're skipped
            controller.Alerts.Clear ();

            // Move left to go to the end of the list
            controller.PressMenuLeft ();

            // Loop backwards through the list
            for (int i = 2; i >= 0; i--) {
                Assert.AreEqual (i, controller.MenuIndex, "The menu index doesn't match.");

                // Move left again
                controller.PressMenuLeft ();
            }

            Assert.AreEqual (2, controller.MenuIndex, "The menu index didn't go back to 2 as it should have.");

        }

        [Test]
        public void Test_NavigateSelectDevice ()
        {
            Console.WriteLine (Environment.CurrentDirectory);

            var devicesDirectory = Path.GetFullPath ("devices");
            Directory.CreateDirectory (devicesDirectory);

            var mockSerialClient = new MockSerialClientWrapper ();
            var mockMqttClient = new MockMqttClientWrapper ();

            var controller = new SystemMenuController ();
            controller.DevicesDirectory = devicesDirectory;
            controller.Client = mockSerialClient;
            controller.MqttClient = mockMqttClient;

            CreateExampleDevice (devicesDirectory, "irrigator1", "Irrigator1", "irrigator", "host");
            CreateExampleDevice (devicesDirectory, "illuminator1", "Illuminator1", "illuminator", "host");
            CreateExampleDevice (devicesDirectory, "ventilator1", "Ventilator1", "ventilator", "host");

            // Initialize the controller
            controller.Initialize ();

            // Run a loop to detect all the devices
            controller.RunLoop ();

            Assert.AreEqual (3, controller.DeviceList.Count, "The device wasn't added to the list.");

            Assert.AreEqual (0, controller.MenuIndex, "The menu index doesn't match.");

            // Clear all alerts so they're skipped
            controller.Alerts.Clear ();

            controller.MenuRight ();

            Assert.AreEqual (1, controller.MenuIndex, "The menu index doesn't match.");

            controller.PressMenuSelect ();

            Assert.IsTrue (controller.DeviceIsSelected, "The device wasn't selected.");

        }

        [Test]
        public void Test_SelectDeviceNavigateRightThroughSubItems ()
        {
            Console.WriteLine (Environment.CurrentDirectory);

            var devicesDirectory = Path.GetFullPath ("devices");
            Directory.CreateDirectory (devicesDirectory);

            var mockSerialClient = new MockSerialClientWrapper ();
            var mockMqttClient = new MockMqttClientWrapper ();

            var controller = new SystemMenuController ();
            controller.DevicesDirectory = devicesDirectory;
            controller.Client = mockSerialClient;
            controller.MqttClient = mockMqttClient;

            CreateExampleDevice (devicesDirectory, "irrigator1", "Irrigator1", "irrigator", "host");
            CreateExampleDevice (devicesDirectory, "illuminator1", "Illuminator1", "illuminator", "host");
            CreateExampleDevice (devicesDirectory, "ventilator1", "Ventilator1", "ventilator", "host");

            // Initialize the controller
            controller.Initialize ();

            // Run a loop to detect all the devices
            controller.RunLoop ();

            Assert.AreEqual (3, controller.DeviceList.Count, "The device wasn't added to the list.");

            Assert.AreEqual (0, controller.MenuIndex, "The menu index doesn't match.");

            // Clear all alerts so they're skipped
            controller.Alerts.Clear ();

            controller.MenuRight ();

            Assert.AreEqual (1, controller.MenuIndex, "The menu index doesn't match.");

            controller.PressMenuSelect ();

            Assert.IsTrue (controller.DeviceIsSelected, "The device wasn't selected.");


            Assert.AreEqual (0, controller.SubMenuIndex, "The sub menu index doesn't match.");

            // Clear all alerts so they're skipped
            controller.Alerts.Clear ();

            var totalSubItems = controller.MenuStructure [controller.CurrentDevice.DeviceGroup].Items.Count;

            for (int i = 1; i < totalSubItems; i++) {
                controller.MenuRight ();

                Assert.AreEqual (i, controller.SubMenuIndex, "The sub menu index doesn't match.");
            }

            controller.MenuRight ();

            Assert.AreEqual (0, controller.SubMenuIndex, "The sub menu index didn't go back to 0 as it should have.");

        }

        [Test]
        public void Test_SelectDeviceNavigateLeftThroughSubItems ()
        {
            Console.WriteLine (Environment.CurrentDirectory);

            var devicesDirectory = Path.GetFullPath ("devices");
            Directory.CreateDirectory (devicesDirectory);

            var mockSerialClient = new MockSerialClientWrapper ();
            var mockMqttClient = new MockMqttClientWrapper ();

            var controller = new SystemMenuController ();
            controller.DevicesDirectory = devicesDirectory;
            controller.Client = mockSerialClient;
            controller.MqttClient = mockMqttClient;

            CreateExampleDevice (devicesDirectory, "irrigator1", "Irrigator1", "irrigator", "host");
            CreateExampleDevice (devicesDirectory, "illuminator1", "Illuminator1", "illuminator", "host");
            CreateExampleDevice (devicesDirectory, "ventilator1", "Ventilator1", "ventilator", "host");

            // Initialize the controller
            controller.Initialize ();

            // Run a loop to detect all the devices
            controller.RunLoop ();

            Assert.AreEqual (3, controller.DeviceList.Count, "The device wasn't added to the list.");

            Assert.AreEqual (0, controller.MenuIndex, "The menu index doesn't match.");

            // Clear all alerts so they're skipped
            controller.Alerts.Clear ();

            controller.MenuRight ();

            Assert.AreEqual (1, controller.MenuIndex, "The menu index doesn't match.");

            controller.PressMenuSelect ();

            Assert.IsTrue (controller.DeviceIsSelected, "The device wasn't selected.");

            Assert.AreEqual (0, controller.SubMenuIndex, "The sub menu index doesn't match.");

            // Clear all alerts so they're skipped
            controller.Alerts.Clear ();

            var totalSubItems = controller.MenuStructure [controller.CurrentDevice.DeviceGroup].Items.Count;

            for (int i = totalSubItems - 1; i >= 0; i--) {
                controller.PressMenuLeft ();

                Assert.AreEqual (i, controller.SubMenuIndex, "The sub menu index doesn't match.");
            }

            controller.PressMenuLeft ();

            Assert.AreEqual (totalSubItems - 1, controller.SubMenuIndex, "The sub menu index didn't go back to 0 as it should have.");

        }

    }
}


