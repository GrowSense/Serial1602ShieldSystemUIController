using System;
using System.IO.Ports;
using System.Threading;
using System.IO;
using duinocom;
using System.Net.Mail;
using System.Configuration;
using System.Collections.Generic;
using uPLibrary.Networking.M2Mqtt.Messages;
using uPLibrary.Networking.M2Mqtt;
using System.Text;
using System.Collections.Generic;

namespace SerialSystemUI
{
    public class SystemMenuController : IDisposable
    {
        public bool IsVerbose;

        public SerialClient Client = null;

        public int WaitTimeBeforeRetry = 120;

        public Dictionary<string, DeviceInfo> DeviceList = new Dictionary<string, DeviceInfo> ();

        public string DevicesDirectory = String.Empty;

        public int MenuIndex = 0;
        public int SubMenuIndex = 0;

        public bool HasChanged = true;

        public string MqttHost;
        public string MqttUsername;
        public string MqttPassword;
        public int MqttPort;


        public string SerialPortName;
        public int SerialBaudRate;

        public string DeviceName;

        public string EmailAddress;
        public string SmtpServer;

        public int LoopNumber = 0;

        public bool DeviceIsSelected;
        //public DeviceInfo CurrentDevice;
        public DeviceInfo CurrentDevice {
            get {
                return GetDeviceByIndex (MenuIndex);
            }
        }

        public bool EnableIncomingMqtt = true;

        public Dictionary<string, MenuInfo> MenuStructure = new Dictionary<string, MenuInfo> ();

        public MqttClient MqttClient;

        public Queue<string> Alerts = new Queue<string> ();

        public int AlertDisplayDuration = 10;
        public DateTime AlertDisplayStartTime;

        public SystemMenuController ()
        {
            var irrigatorMenuStructure = new MenuInfo ("irrigator");
            irrigatorMenuStructure.Items.Add ("C", new MenuItemInfo ("C", "Moisture", "%", false));
            irrigatorMenuStructure.Items.Add ("I", new MenuItemInfo ("I", "Interval", "s", true));
            irrigatorMenuStructure.Items.Add ("T", new MenuItemInfo ("T", "Threshold", "%", true));
            var pumpOptions = new Dictionary<int, string> ();
            pumpOptions.Add (0, "Off");
            pumpOptions.Add (1, "On");
            pumpOptions.Add (2, "Auto");
            irrigatorMenuStructure.Items.Add ("P", new MenuItemInfo ("P", "Pump", "", true, pumpOptions));
            irrigatorMenuStructure.Items.Add ("R", new MenuItemInfo ("R", "Raw", "", false));
            irrigatorMenuStructure.Items.Add ("D", new MenuItemInfo ("D", "Dry", "", true));
            irrigatorMenuStructure.Items.Add ("W", new MenuItemInfo ("W", "Wet", "", true));
            MenuStructure.Add ("irrigator", irrigatorMenuStructure);

            var monitorMenuStructure = new MenuInfo ("monitor");
            monitorMenuStructure.Items.Add ("C", new MenuItemInfo ("C", "Moisture", "%", false));
            monitorMenuStructure.Items.Add ("I", new MenuItemInfo ("I", "Interval", "s", true));
            monitorMenuStructure.Items.Add ("R", new MenuItemInfo ("R", "Raw", "", false));
            monitorMenuStructure.Items.Add ("D", new MenuItemInfo ("D", "Dry", "", true));
            monitorMenuStructure.Items.Add ("W", new MenuItemInfo ("W", "Wet", "", true));
            MenuStructure.Add ("monitor", monitorMenuStructure);

            var ventilatorMenuStructure = new MenuInfo ("ventilator");
            ventilatorMenuStructure.Items.Add ("A", new MenuItemInfo ("A", "Temp/Hum", "", false));
            ventilatorMenuStructure.Items.Add ("I", new MenuItemInfo ("I", "Interval", "s", true));
            ventilatorMenuStructure.Items.Add ("S", new MenuItemInfo ("S", "MinTemp", "c", true));
            ventilatorMenuStructure.Items.Add ("U", new MenuItemInfo ("U", "MaxTemp", "c", true));
            ventilatorMenuStructure.Items.Add ("G", new MenuItemInfo ("G", "MinHum", "%", true));
            ventilatorMenuStructure.Items.Add ("J", new MenuItemInfo ("J", "MaxHum", "%", true));
            MenuStructure.Add ("ventilator", ventilatorMenuStructure);

            var illuminatorMenuStructure = new MenuInfo ("illuminator");
            illuminatorMenuStructure.Items.Add ("L", new MenuItemInfo ("L", "Light", "%", false));
            illuminatorMenuStructure.Items.Add ("I", new MenuItemInfo ("I", "Interval", "s", true));
            illuminatorMenuStructure.Items.Add ("T", new MenuItemInfo ("T", "Threshold", "%", true));
            illuminatorMenuStructure.Items.Add ("R", new MenuItemInfo ("R", "Raw", "", false));
            illuminatorMenuStructure.Items.Add ("D", new MenuItemInfo ("D", "Dark", "", true));
            illuminatorMenuStructure.Items.Add ("B", new MenuItemInfo ("B", "Bright", "", true));
            MenuStructure.Add ("illuminator", illuminatorMenuStructure);

            var uiMenuStructure = new MenuInfo ("ui");
            //uiMenuStructure.Items.Add ("Z", new MenuItemInfo ("Z", "Version", "", false));
            MenuStructure.Add ("ui", uiMenuStructure);
        }

        public void Run ()
        {

            DevicesDirectory = Path.GetFullPath (DevicesDirectory);

            Console.WriteLine ("Devices directory:");
            Console.WriteLine (DevicesDirectory);

            Console.WriteLine ("Device Name: " + DeviceName);
            Console.WriteLine ("MQTT Host: " + MqttHost);
            Console.WriteLine ("MQTT Username: " + MqttUsername);
            Console.WriteLine ("MQTT Port: " + MqttPort);

            LoadDeviceList ();

            SerialPort port = null;

            if (String.IsNullOrEmpty (SerialPortName)) {
                Console.WriteLine ("Serial port not specified. Detecting.");
                var detector = new SerialPortDetector ();
                port = detector.Detect ();
                SerialPortName = port.PortName;
            } else {
                Console.WriteLine ("Serial port specified");
                port = new SerialPort (SerialPortName, SerialBaudRate);
            }

            Console.WriteLine ("Device name: " + DeviceName);
            Console.WriteLine ("Serial port name: " + SerialPortName);

            if (port == null) {
                Console.WriteLine ("Error: Device port not found.");
            } else {
                Console.WriteLine ("Serial port: " + port.PortName);

                Client = new SerialClient (port);

                EnsurePortIsOpen ();

                SetupMQTT ();

                // Wait until the first line arrives
                Client.ReadLine ();

                Thread.Sleep (1000);
                SendMessageToDisplay (0, "Connected!");
                SendMessageToDisplay (1, "                ");

                Thread.Sleep (3000);

                var isRunning = true;
                while (isRunning) {
                    try {
                        RunLoop ();
                        
                        Thread.Sleep (20);
                    

                    } catch (Exception ex) {
                        Console.WriteLine ("An error occurred:");
                        Console.WriteLine (ex.ToString ());
                        Console.WriteLine ();
                        Console.WriteLine ("Waiting for 30 seconds then retrying");

                        SendErrorEmail (ex, DeviceName, SmtpServer, EmailAddress);

                        Thread.Sleep (30 * 1000);

                        Run ();
                    }
                }
            }
        }

        public void SetupMQTT ()
        {
            MqttClient = new MqttClient (MqttHost, MqttPort, false, null, null, MqttSslProtocols.None);

            var clientId = Guid.NewGuid ().ToString ();

            MqttClient.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
            MqttClient.Connect (clientId, MqttUsername, MqttPassword);

            var subscribeTopics = GetSubscribeTopics ();

            foreach (var topic in subscribeTopics) {
                MqttClient.Subscribe (new string[] { topic }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            }
        }

        public void RunLoop ()
        {
            Console.WriteLine ("===== Start Loop");
            LoopNumber++;

            // Refresh the device list every 100 loops
            if (LoopNumber % 100 == 0)
                LoadDeviceList ();

            EnsurePortIsOpen ();

            RenderDisplay ();

            if (Client.Port.BytesToRead > 0) {
                var line = Client.Port.ReadLine ();

                ProcessLine (line.Trim ());
            }

            Console.WriteLine ("===== End Loop");

            Thread.Sleep (100);
        }

        public string[] GetSubscribeTopics ()
        {
            var list = new List<string> ();
            foreach (var deviceEntry in DeviceList) {
                var deviceInfo = deviceEntry.Value;
                var deviceTopicPattern = "/" + deviceInfo.DeviceName + "/#";
                list.Add (deviceTopicPattern);
            }
            return list.ToArray ();
        }

        public void EnsurePortIsOpen ()
        {
            if (!Client.Port.IsOpen) {
                Client.Open ();
                Thread.Sleep (1000);
                Client.ReadLine ();
            }
        }

        public void LoadDeviceList ()
        {
            foreach (var deviceDir in Directory.GetDirectories(DevicesDirectory)) {
                var deviceName = Path.GetFileName (deviceDir);
                var deviceInfo = LoadDeviceInfo (deviceName);
                if (!DeviceList.ContainsKey (deviceName))
                    DeviceList.Add (deviceName, deviceInfo);
            }
        }

        public DeviceInfo LoadDeviceInfo (string deviceName)
        {
            var deviceInfo = new DeviceInfo (deviceName);
            var deviceInfoDir = Path.Combine (DevicesDirectory, deviceName);
            deviceInfo.DeviceLabel = File.ReadAllText (Path.Combine (deviceInfoDir, "label.txt")).Trim ();
            deviceInfo.DeviceGroup = File.ReadAllText (Path.Combine (deviceInfoDir, "group.txt")).Trim ();

            return deviceInfo;
        }

        public void ProcessLine (string line)
        {
            Console.WriteLine ("Processing line:");
            Console.WriteLine (line);

            if (line == "up")
                MenuUp ();
            else if (line == "down")
                MenuDown ();
            else if (line == "left")
                MenuLeft ();
            else if (line == "right")
                MenuRight ();
            else if (line == "select")
                MenuSelect ();
        }

        public void RenderDisplay ()
        {
            if (Alerts.Count > 0) {
                if (AlertDisplayStartTime == DateTime.MinValue) {
                    var alert = Alerts.Peek ();
                    SendMessageToDisplay (alert);
                    AlertDisplayStartTime = DateTime.Now;
                } else if (AlertDisplayStartTime.AddSeconds (AlertDisplayDuration) < DateTime.Now) {
                    Alerts.Dequeue ();
                    AlertDisplayStartTime = DateTime.MinValue;
                    HasChanged = true;
                }

            } else {
                if (HasChanged) {
                    var deviceInfo = GetDeviceByIndex (MenuIndex);

                    if (deviceInfo != null) {
                        var deviceLabel = deviceInfo.DeviceLabel;
                        var labelPostFix = (DeviceIsSelected ? "*" : "");
                        var fullTitle = deviceLabel + labelPostFix;

                        // Send the first line to the display
                        SendMessageToDisplay (0, fullTitle);

                        RenderSubItem ();
                    }
                    HasChanged = false;
                }
            }
        }

        public void RenderSubItem ()
        {
            var didRenderSubItem = false;
            if (MenuStructure.ContainsKey (CurrentDevice.DeviceGroup)) {
                var menuInfo = MenuStructure [CurrentDevice.DeviceGroup];

                var menuItemInfo = GetMenuItemInfoByIndex (CurrentDevice.DeviceGroup, SubMenuIndex);

                if (menuItemInfo != null) {
                    var valueLabel = menuItemInfo.Label;
                    var valueKey = menuItemInfo.Key;

                    var value = String.Empty;
                    if (CurrentDevice.UpdatedData.ContainsKey (valueKey)) {
                        value = CurrentDevice.UpdatedData [valueKey];
                    } else if (CurrentDevice.Data.ContainsKey (valueKey)) {
                        value = CurrentDevice.Data [valueKey];
                    }
                    value = FixValueForDisplay (value, valueKey, CurrentDevice);

                    // Send the second line to the display
                    var message = valueLabel + " " + value + menuItemInfo.PostFix;
                    SendMessageToDisplay (1, message);

                    didRenderSubItem = true;
                }
            }

            if (!didRenderSubItem) {
                SendMessageToDisplay (1, "                ");
            }
        }

        public string FixValueForDisplay (string value, string key, DeviceInfo info)
        {
            var menuItemInfo = GetMenuItemInfoByIndex (CurrentDevice.DeviceGroup, SubMenuIndex);

            if (menuItemInfo.Options != null && menuItemInfo.Options.Count > 0) {
                var valueInt = Convert.ToInt32 (value);
                return menuItemInfo.Options [valueInt];
            } else {
                if (String.IsNullOrEmpty (value)) {
                    if (info.DeviceGroup == "ventilator")
                        return "0c 0%";

                    if (key.Length == 1)
                        return 0.ToString ();
                }
            }
            return value;
        }

        public DeviceInfo GetDeviceByIndex (int deviceIndex)
        {
            var i = 0;
            foreach (var entry in DeviceList) {
                if (i == deviceIndex)
                    return entry.Value;

                i++;
            }

            return null;
        }

        public MenuItemInfo GetMenuItemInfoByIndex (string deviceGroup, int subMenuIndex)
        {
            return GetMenuItemInfoByIndex (MenuStructure [deviceGroup], subMenuIndex);
        }

        public MenuItemInfo GetMenuItemInfoByIndex (MenuInfo menuStructure, int subMenuIndex)
        {
            int i = 0;
            foreach (var entry in menuStructure.Items) {
                if (i == subMenuIndex) {
                    return entry.Value;
                }
                i++;
            }

            return null;
        }

        public void MenuUp ()
        {
            if (Alerts.Count > 0) {
                CancelAlert ();
            } else if (DeviceIsSelected) {
                var menuStructureItem = GetMenuItemInfoByIndex (CurrentDevice.DeviceGroup, SubMenuIndex);

                if (menuStructureItem.IsEditable) {
                    var key = menuStructureItem.Key;

                    var existingValueString = String.Empty;

                    if (!CurrentDevice.UpdatedData.ContainsKey (key)) {
                        CurrentDevice.UpdatedData [key] = CurrentDevice.Data [key];
                    }

                    existingValueString = CurrentDevice.UpdatedData [key];

                    var existingValue = 0;

                    if (String.IsNullOrEmpty (existingValueString)) {
                        existingValueString = 0.ToString ();
                    } else
                        existingValue = Convert.ToInt32 (existingValueString);

                    existingValue++;

                    if (existingValue > menuStructureItem.MaxValue)
                        existingValue = menuStructureItem.MinValue;
                    if (existingValue < menuStructureItem.MinValue)
                        existingValue = menuStructureItem.MaxValue;

                    CurrentDevice.UpdatedData [key] = existingValue.ToString ();
                   

                    HasChanged = true;
                }
            }
        }


        public void MenuDown ()
        {
            if (Alerts.Count > 0) {
                CancelAlert ();
            } else if (!DeviceIsSelected) {
                DeviceIsSelected = true;
                HasChanged = true;
            } else {
                var menuStructureItem = GetMenuItemInfoByIndex (CurrentDevice.DeviceGroup, SubMenuIndex);

                if (menuStructureItem.IsEditable) {
                    var key = menuStructureItem.Key;

                    var existingValueString = String.Empty;

                    if (!CurrentDevice.UpdatedData.ContainsKey (key)) {
                        CurrentDevice.UpdatedData [key] = CurrentDevice.Data [key];
                    }

                    existingValueString = CurrentDevice.UpdatedData [key];

                    var existingValue = 0;

                    if (String.IsNullOrEmpty (existingValueString)) {
                        existingValueString = 0.ToString ();
                    } else
                        existingValue = Convert.ToInt32 (existingValueString);

                    existingValue--;

                    if (existingValue > menuStructureItem.MaxValue)
                        existingValue = menuStructureItem.MinValue;
                    if (existingValue < menuStructureItem.MinValue)
                        existingValue = menuStructureItem.MaxValue;

                    CurrentDevice.UpdatedData [key] = existingValue.ToString ();
                   
                    HasChanged = true;
                }
            }
        }

        public void MenuLeft ()
        {
            if (Alerts.Count > 0) {
                CancelAlert ();
            } else if (!DeviceIsSelected) {
                if (MenuIndex == 0)
                    MenuIndex = DeviceList.Count - 1;
                else
                    MenuIndex--;
            } else {
                var menuItemInfo = GetMenuItemInfoByIndex (CurrentDevice.DeviceGroup, SubMenuIndex);
                var isEditingValue = DeviceIsSelected && menuItemInfo.IsEditable;
                var valueHasChanged = CurrentDevice.UpdatedData.ContainsKey (menuItemInfo.Key)
                                      && CurrentDevice.UpdatedData [menuItemInfo.Key] != CurrentDevice.Data [menuItemInfo.Key];

                if (isEditingValue && valueHasChanged) {
                    PublishUpdatedValue ();
                }

                if (SubMenuIndex == 0)
                    SubMenuIndex = MenuStructure [CurrentDevice.DeviceGroup].Items.Count - 1;
                else
                    SubMenuIndex--;

                CheckIfEditable ();
            }

            HasChanged = true;
        }



        public void MenuRight ()
        {
            if (Alerts.Count > 0) {
                CancelAlert ();
            } else if (!DeviceIsSelected) {
                if (MenuIndex >= DeviceList.Count - 1)
                    MenuIndex = 0;
                else
                    MenuIndex++;
            } else {
                var menuItemInfo = GetMenuItemInfoByIndex (CurrentDevice.DeviceGroup, SubMenuIndex);
                var isEditingValue = DeviceIsSelected && menuItemInfo.IsEditable;
                var valueHasChanged = CurrentDevice.UpdatedData.ContainsKey (menuItemInfo.Key)
                                      && CurrentDevice.UpdatedData [menuItemInfo.Key] != CurrentDevice.Data [menuItemInfo.Key];

                if (isEditingValue && valueHasChanged) {
                    PublishUpdatedValue ();
                }

                if (SubMenuIndex >= MenuStructure [CurrentDevice.DeviceGroup].Items.Count - 1)
                    SubMenuIndex = 0;
                else
                    SubMenuIndex++;
            }

            HasChanged = true;
        }

        public void MenuSelect ()
        {
            var isEditingValue = DeviceIsSelected && GetMenuItemInfoByIndex (CurrentDevice.DeviceGroup, SubMenuIndex).IsEditable;
            if (isEditingValue) {
                PublishUpdatedValue ();
            } else {
                DeviceIsSelected = !DeviceIsSelected;
            }
            HasChanged = true;
        }

        public void CancelAlert ()
        {
            Alerts.Dequeue ();
            HasChanged = true;
        }

        public bool CheckIfEditable ()
        {
            var menuItem = MenuStructure [CurrentDevice.DeviceGroup];
            var isEditable = GetMenuItemInfoByIndex (CurrentDevice.DeviceGroup, SubMenuIndex).IsEditable;

            // Disable incoming MQTT when in edit mode
            if (isEditable) {
                DisableMqtt ();
            } else {
                EnableMqtt ();
            }

            return isEditable;
        }

        public void DisableMqtt ()
        {
            EnableIncomingMqtt = false;
            Thread.Sleep (2000);
        }

        public void EnableMqtt ()
        {
            EnableIncomingMqtt = true;
        }

        public void PublishUpdatedValue ()
        {
            var incomingLinePrefix = ConfigurationSettings.AppSettings ["IncomingLinePrefix"];

            var dividerCharacter = ConfigurationSettings.AppSettings ["DividerSplitCharacter"].ToCharArray () [0];
            var equalsCharacter = ConfigurationSettings.AppSettings ["EqualsSplitCharacter"].ToCharArray () [0];

            var key = GetMenuItemInfoByIndex (CurrentDevice.DeviceGroup, SubMenuIndex).Key;

            var deviceTopic = "/" + CurrentDevice.DeviceName;
            var fullTopic = deviceTopic + "/" + key + "/in";

            var value = CurrentDevice.UpdatedData [key];

            MqttClient.Publish (fullTopic, Encoding.UTF8.GetBytes (value),
                MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, // QoS level
                true);
        }

        public void SendErrorEmail (Exception error, string deviceName, string smtpServer, string emailAddress)
        {
            var areDetailsProvided = (smtpServer != "mail.example.com" &&
                                     emailAddress != "user@example.com" &&
                                     !String.IsNullOrWhiteSpace (smtpServer) &&
                                     !String.IsNullOrWhiteSpace (emailAddress));

            if (areDetailsProvided) {
                try {
                    var subject = "Error: System UI controller";
                    var body = "The following error was thrown by the system UI controller...\n\nDevice name: " + deviceName + "\n\n" + error.ToString ();

                    var mail = new MailMessage (emailAddress, emailAddress, subject, body);

                    var smtpClient = new SmtpClient (smtpServer);

                    smtpClient.Send (mail);

                } catch (Exception ex) {
                    Console.WriteLine ("");
                    Console.WriteLine ("An error occurred while sending error report...");
                    Console.WriteLine ("SMTP Server: " + smtpServer);
                    Console.WriteLine ("Email Address: " + emailAddress);
                    Console.WriteLine ("");
                    Console.WriteLine (ex.ToString ());
                    Console.WriteLine ("");
                }
            } else {
                Console.WriteLine ("");
                Console.WriteLine ("SMTP server and email address not provided. Skipping error report email.");
                Console.WriteLine ("");
            }
        }

        public void SendMessageToDisplay (int lineIndex, string message)
        {
            var fullMessage = lineIndex + "|" + message;

            SendMessageToDisplay (fullMessage);
        }

        public void SendMessageToDisplay (string fullMessage)
        {
            try {
                EnsurePortIsOpen ();
                Client.WriteLine (fullMessage);
            } catch (Exception ex) {
                Console.WriteLine ("Failed to send message to device");
                Console.WriteLine (ex.ToString ());
                Console.WriteLine ();
                Console.WriteLine ("Waiting for " + WaitTimeBeforeRetry + " seconds then retrying");

                Thread.Sleep (WaitTimeBeforeRetry * 1000);

                SendMessageToDisplay (fullMessage);
            }
        }

        // this code runs when a message was received
        public void client_MqttMsgPublishReceived (object sender, MqttMsgPublishEventArgs e)
        {
            var topic = e.Topic;
            var message = System.Text.Encoding.Default.GetString (e.Message);

            if (IsVerbose)
                Console.WriteLine ("Message received: " + message);

            ProcessIncomingMqttMessage (topic, message);
        }

        public void ProcessIncomingMqttMessage (string topic, string message)
        {
            if (EnableIncomingMqtt) {
                var topicSections = topic.Trim ('/').Split ('/');
                if (topicSections.Length == 2) {
                    var deviceName = topicSections [0];
                    var subTopic = topicSections [1];

                    if (DeviceList.ContainsKey (deviceName)) {
                        if (IsVerbose) {
                            Console.WriteLine ("Device name: " + deviceName);
                            Console.WriteLine ("Subtopic: " + subTopic);
                            Console.WriteLine ("Message: " + message);
                        }
                
                        var deviceInfo = DeviceList [deviceName];
                        deviceInfo.Data [subTopic] = message;

                        var deviceLabel = DeviceList [deviceName].DeviceLabel;

                        if (subTopic == "StatusMessage") {
                            if (message != "Online")
                                Alerts.Enqueue ("0|" + deviceLabel + "\r\n1|" + message);
                        }
                    }
                }
            }
        }

        public void Dispose ()
        {
            Client.Close ();
        }
    }
}

