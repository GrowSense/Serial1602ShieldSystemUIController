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

            CreateExampleDevice (devicesDirectory, "device1", "Device1", "Group", "host");

            controller.RunLoop ();

            Assert.AreEqual (1, controller.DeviceList.Count, "The device wasn't added to the list.");
            Assert.AreEqual (2, mockMqttClient.Subscriptions.Count, "The device MQTT subscriptions weren't found.");
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

        [Test]
        public void Test_FixMenuIndexAfterDevicedRemoved_Decrement ()
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

            var deviceInfo2 = new DeviceInfo ();
            deviceInfo2.DeviceName = "device2";
            deviceInfo2.DeviceGroup = "DeviceGroup";
            deviceInfo2.DeviceLabel = "DeviceLabel";

            var deviceInfo3 = new DeviceInfo ();
            deviceInfo3.DeviceName = "device2";
            deviceInfo3.DeviceGroup = "DeviceGroup";
            deviceInfo3.DeviceLabel = "DeviceLabel";

            controller.DeviceList.Add ("device1", deviceInfo);
            controller.DeviceList.Add ("device2", deviceInfo2);
            controller.DeviceList.Add ("device3", deviceInfo3);

            controller.MenuIndex = 2;

            controller.RemoveDevice (deviceInfo3);

            Assert.AreEqual (1, controller.MenuIndex, "Didn't correct the menu index.");
        }

        [Test]
        public void Test_FixMenuIndexAfterDevicedRemoved_DontDecrement ()
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

            var deviceInfo2 = new DeviceInfo ();
            deviceInfo2.DeviceName = "device2";
            deviceInfo2.DeviceGroup = "DeviceGroup";
            deviceInfo2.DeviceLabel = "DeviceLabel";

            var deviceInfo3 = new DeviceInfo ();
            deviceInfo3.DeviceName = "device2";
            deviceInfo3.DeviceGroup = "DeviceGroup";
            deviceInfo3.DeviceLabel = "DeviceLabel";

            controller.DeviceList.Add ("device1", deviceInfo);
            controller.DeviceList.Add ("device2", deviceInfo2);
            controller.DeviceList.Add ("device3", deviceInfo3);

            controller.MenuIndex = 1;

            controller.RemoveDevice (deviceInfo3);

            Assert.AreEqual (1, controller.MenuIndex, "Changed the menu index when it shouldn't have.");
        }

    }
}

