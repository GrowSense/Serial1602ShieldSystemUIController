using System;
using NUnit.Framework;
using Serial1602ShieldSystemUIController;
using System.IO;

namespace Serial1602ShieldSystemUIController.Tests.Integration
{
    [TestFixture (Category = "Integration")]
    public class SystemMenuControllerTestFixture : BaseTestFixture
    {
        [Test]
        public void Test_DetectNewDevice ()
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

            CreateExampleDevice (devicesDirectory, "device1", "Device1", "Group");

            controller.RunLoop ();

            Assert.AreEqual (1, controller.DeviceList.Count, "The device wasn't added to the list.");
            Assert.AreEqual (1, mockMqttClient.Subscriptions.Count, "The device MQTT subscriptions weren't found.");
        }

        [Test]
        public void Test_RemoveMissingDevice ()
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

            var deviceInfo = new DeviceInfo ();
            deviceInfo.DeviceName = "device1";
            deviceInfo.DeviceGroup = "DeviceGroup";
            deviceInfo.DeviceLabel = "DeviceLabel";

            controller.DeviceList.Add ("device1", deviceInfo);

            //CreateExampleDevice (devicesDirectory, "device1", "Device1", "Group");

            controller.RunLoop ();

            Assert.AreEqual (0, controller.DeviceList.Count, "The device wasn't removed from the list.");
            Assert.AreEqual (1, mockMqttClient.Unsubscriptions.Count, "The device MQTT subscriptions weren't removed.");
        }

        public void CreateExampleDevice (string devicesDir, string name, string label, string group)
        {
            var deviceDir = Path.Combine (devicesDir, name);
            Directory.CreateDirectory (deviceDir);

            File.WriteAllText (Path.Combine (deviceDir, "name.txt"), name);
            File.WriteAllText (Path.Combine (deviceDir, "label.txt"), label);
            File.WriteAllText (Path.Combine (deviceDir, "group.txt"), group);
        }
    }
}

