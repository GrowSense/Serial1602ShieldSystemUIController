using System;
using System.IO;
using NUnit.Framework;

namespace Serial1602ShieldSystemUIController.Tests.Integration
{
    [TestFixture (Category = "Integration")]
    public class SystemMenuControllerUploadSketchTestFixture : BaseTestFixture
    {
        [Test]
        public void Test_UploadSketch ()
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

            Console.WriteLine ("Running a loop to detect UI device...");
            controller.RunLoop ();

            Assert.AreEqual (1, controller.DeviceList.Count, "The device wasn't added to the list.");

            Assert.AreEqual (0, controller.MenuIndex, "The menu index doesn't match.");
            controller.Alerts.Clear ();

            Console.WriteLine ("Device output:");
            Console.WriteLine (mockSerialClient.Output);
            mockSerialClient.ClearOutput ();

            Console.WriteLine ("Setting menu index to the UI device...");
            controller.MenuIndex = 0;
            controller.SubMenuIndex = 2;
            controller.DeviceIsSelected = true;

            Console.WriteLine ("Running a loop to update display...");
            controller.RunLoop ();

            Console.WriteLine ("Device output:");
            Console.WriteLine (mockSerialClient.Output);

            Assert.IsTrue (mockSerialClient.Output.Contains ("Ui1"), "Didn't select the correct device.");
            Assert.IsTrue (mockSerialClient.Output.Contains ("Upload sketch"), "Didn't select the correct sub menu option.");

            mockSerialClient.ClearOutput ();

            Console.WriteLine ("Selecting the upload sketch option...");
            controller.PressMenuSelect ();

            Console.WriteLine ("Running a loop to update display...");
            controller.RunLoop ();

            Console.WriteLine ("Device output:");
            Console.WriteLine (mockSerialClient.Output);

            var menuItemInfo = (UploadSketchMenuItemInfo)controller.GetCurrentMenuItemInfo ();

            Assert.AreEqual (menuItemInfo.StepIndex, 1, "Incorrect step index.");
            Assert.IsTrue (mockSerialClient.Output.Contains ("Ui1"), "Displaying the wrong device.");
            Assert.IsTrue (mockSerialClient.Output.Contains ("SM Monitor"), "Displaying the wrong sketch device.");

            mockSerialClient.ClearOutput ();

            Console.WriteLine ("Pressing up to change to the next sketch type...");
            controller.PressMenuUp ();

            Console.WriteLine ("Running a loop to update display...");
            controller.RunLoop ();

            Console.WriteLine ("Device output:");
            Console.WriteLine (mockSerialClient.Output);

            Assert.AreEqual (menuItemInfo.StepIndex, 1, "Incorrect step index.");
            Assert.AreEqual (menuItemInfo.SelectedSketchIndex, 1, "Incorrect sketch index.");
            Assert.IsTrue (mockSerialClient.Output.Contains ("Ui1"), "Displaying the wrong device.");
            Assert.IsTrue (mockSerialClient.Output.Contains ("Irrigator"), "Displaying the wrong sketch device.");

            mockSerialClient.ClearOutput ();

            Console.WriteLine ("Pressing select to choose this sketch type...");
            controller.PressMenuSelect ();

            Console.WriteLine ("Running a loop to update display...");
            controller.RunLoop ();

            Console.WriteLine ("Device output:");
            Console.WriteLine (mockSerialClient.Output);

            Assert.AreEqual (menuItemInfo.StepIndex, 2, "Incorrect step index.");
            Assert.AreEqual (menuItemInfo.SelectedSketchIndex, 1, "Incorrect sketch index.");
            Assert.IsTrue (mockSerialClient.Output.Contains ("Ui1"), "Displaying the wrong device.");
            Assert.IsTrue (mockSerialClient.Output.Contains ("nano"), "Displaying the wrong board.");

            mockSerialClient.ClearOutput ();

            Console.WriteLine ("Pressing up to change to the next board type...");
            controller.PressMenuUp ();

            Console.WriteLine ("Running a loop to update display...");
            controller.RunLoop ();

            Console.WriteLine ("Device output:");
            Console.WriteLine (mockSerialClient.Output);

            Assert.AreEqual (menuItemInfo.StepIndex, 2, "Incorrect step index.");
            Assert.AreEqual (menuItemInfo.SelectedSketchIndex, 1, "Incorrect sketch index.");
            Assert.AreEqual (menuItemInfo.SelectedBoardIndex, 1, "Incorrect board index.");
            Assert.IsTrue (mockSerialClient.Output.Contains ("Ui1"), "Displaying the wrong device.");
            Assert.IsTrue (mockSerialClient.Output.Contains ("uno"), "Displaying the wrong board.");

            mockSerialClient.ClearOutput ();

            Console.WriteLine ("Pressing select to choose this board type...");
            controller.PressMenuSelect ();

            Console.WriteLine ("Running a loop to update display...");
            controller.RunLoop ();

            Console.WriteLine ("Device output:");
            Console.WriteLine (mockSerialClient.Output);

            /*Assert.AreEqual (menuItemInfo.StepIndex, 2, "Incorrect step index.");
            Assert.AreEqual (menuItemInfo.SelectedSketchIndex, 1, "Incorrect sketch index.");
            Assert.AreEqual (menuItemInfo.SelectedBoardIndex, 1, "Incorrect board index.");
            Assert.IsTrue (mockSerialClient.Output.Contains ("Ui1"), "Displaying the wrong device.");
            Assert.IsTrue (mockSerialClient.Output.Contains ("nano"), "Displaying the wrong board.");*/

            mockSerialClient.ClearOutput ();
            /*
            Console.WriteLine ("Pressing down button to change to yes...");
            controller.MenuDown ();

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
            controller.MenuSelect ();

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
            mockSerialClient.ClearOutput ();*/
        }
    }
}

