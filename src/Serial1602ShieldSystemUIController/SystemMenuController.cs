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

namespace Serial1602ShieldSystemUIController
{
    public class SystemMenuController : IDisposable
    {
        public bool IsVerbose;

        public SerialClientWrapper Client = null;

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

        public MqttClientWrapper MqttClient;

        public Queue<string> Alerts = new Queue<string> ();

        public int AlertDisplayDuration = 4;
        public DateTime AlertDisplayStartTime;

        public int SleepTimeBetweenLoops = 100;

        public int MqttStatusPublishIntervalInSeconds = 30;
        public DateTime LastMqttStatusPublished = DateTime.MinValue;

        public ProcessStarter Starter = new ProcessStarter ();

        public MenuInfoCreator MenuCreator = new MenuInfoCreator ();

        public SystemMenuController ()
        {
        }

        public void Run ()
        {
            DevicesDirectory = Path.GetFullPath (DevicesDirectory);

            MenuCreator.Create (this);

            if (String.IsNullOrEmpty (DevicesDirectory))
                throw new Exception ("DevicesDirectory property not set.");
            if (!Directory.Exists (DevicesDirectory))
                throw new Exception ("Cannot find devices directory: " + DevicesDirectory);

            Console.WriteLine ("Devices directory:");
            Console.WriteLine (DevicesDirectory);

            Console.WriteLine ("Device Name: " + DeviceName);
            Console.WriteLine ("MQTT Host: " + MqttHost);
            Console.WriteLine ("MQTT Username: " + MqttUsername);
            Console.WriteLine ("MQTT Port: " + MqttPort);

            InitializeSerialPort ();

            Console.WriteLine ("Device name: " + DeviceName);
            Console.WriteLine ("Serial port name: " + SerialPortName);

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
                        
                    Thread.Sleep (SleepTimeBetweenLoops);
                    

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

        public void InitializeSerialPort ()
        {
            var serialPort = GetSerialPort ();
            if (serialPort != null)
                Client = new SerialClientWrapper (serialPort);
            else {
                throw new Exception ("Device port not found.");
            }

        }

        public SerialPort GetSerialPort ()
        {
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
            return port;
        }

        public void SetupMQTT ()
        {
            MqttClient = new MqttClientWrapper (MqttHost, MqttPort);

            var clientId = Guid.NewGuid ().ToString ();

            MqttClient.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
            MqttClient.Connect (clientId, MqttUsername, MqttPassword);


            // TODO: Remove if not needed. Subscriptions are made as devices are added.
/*            var subscribeTopics = GetSubscribeTopics ();

            foreach (var topic in subscribeTopics) {
                MqttClient.Subscribe (new string[] { topic }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            }*/
        }

        public void RunLoop ()
        {
            Console.WriteLine ("===== Start Loop");
            LoopNumber++;

            // Refresh the device list every 100 loops
            //if (LoopNumber % 10 == 0)
            AddNewDevices ();

            EnsurePortIsOpen ();

            RenderDisplay ();

            if (Client.HasData) {
                var line = Client.ReadLine ();

                ProcessLine (line.Trim ());
            }

            RemoveLostDevices ();

            PublishStatusToMqtt ();

            Console.WriteLine ("===== End Loop");

            Thread.Sleep (10);
        }

        public string[] GetSubscribeTopics ()
        {
            var list = new List<string> ();
            foreach (var deviceEntry in DeviceList) {
                var deviceInfo = deviceEntry.Value;
                list.AddRange (GetSubscribeTopicsForDevice (deviceInfo));
            }
            return list.ToArray ();
        }

        public string[] GetSubscribeTopicsForDevice (DeviceInfo deviceInfo)
        {
            var list = new List<string> ();
            var deviceTopicPattern = "/" + deviceInfo.DeviceName + "/#";
            Console.WriteLine ("Subscrice topic: " + deviceTopicPattern);
            list.Add (deviceTopicPattern);
            return list.ToArray ();
        }

        public void EnsurePortIsOpen ()
        {
            if (!Client.IsOpen) {
                Client.Open ();
                Thread.Sleep (1000);
                Client.ReadLine ();
            }
        }

        public void AddNewDevices ()
        {
            if (String.IsNullOrEmpty (DevicesDirectory))
                throw new Exception ("DevicesDirectory property not set.");

            if (!Directory.Exists (DevicesDirectory))
                throw new Exception ("Can't find devices directory: " + DevicesDirectory);

            foreach (var deviceDir in Directory.GetDirectories(DevicesDirectory)) {
                var deviceName = Path.GetFileName (deviceDir);
                if (!DeviceList.ContainsKey (deviceName)) {
                    var deviceInfo = LoadDeviceInfo (deviceName);
                    AddDevice (deviceInfo);
                }
            }

        }

        public void RemoveLostDevices ()
        {
            for (int i = 0; i < DeviceList.Count; i++) {
                var deviceInfo = GetDeviceByIndex (i);
                var deviceDir = Path.Combine (DevicesDirectory, deviceInfo.DeviceName);
                if (!Directory.Exists (deviceDir)) {
                    RemoveDevice (deviceInfo);
                    i--;
                }
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

        public void AddDevice (DeviceInfo info)
        {
            Console.WriteLine ("Adding device: " + info.DeviceName);
            DeviceList.Add (info.DeviceName, info);
            foreach (var topic in GetSubscribeTopicsForDevice (info)) {
                MqttClient.Subscribe (new string[] { topic }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            }
            Alerts.Enqueue ("0|" + info.DeviceLabel + "\r\n1|Detected");
        }

        public void RemoveDevice (DeviceInfo info)
        {
            Console.WriteLine ("Removing device: " + info.DeviceName);
            DeviceList.Remove (info.DeviceName);
            foreach (var topic in GetSubscribeTopicsForDevice (info)) {
                MqttClient.Unsubscribe (new string[] { topic });
            }
            Alerts.Enqueue ("0|" + info.DeviceLabel + "\r\n1|Removed");

            FixMenuIndexAfterDeviceRemoved (info);
        }

        public void FixMenuIndexAfterDeviceRemoved (DeviceInfo info)
        {
            var index = 0;
            var i = 0;
            foreach (var entry in DeviceList) {
                if (entry.Value.DeviceName == info.DeviceName) {
                    index = i;
                    break;
                }

                i++;
            }

            // If the menu index is greater than the index of the removed device then decrement the index
            if (MenuIndex > index)
                MenuIndex--;
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
                    if (menuItemInfo is MqttMenuItemInfo)
                        RenderSubItemMqtt ((MqttMenuItemInfo)menuItemInfo);
                    if (menuItemInfo is CommandMenuItemInfo)
                        RenderSubItemCommand ((CommandMenuItemInfo)menuItemInfo);

                    didRenderSubItem = true;
                }
            }

            if (!didRenderSubItem) {
                SendMessageToDisplay (1, "                ");
            }
        }

        public void RenderSubItemMqtt (MqttMenuItemInfo menuItemInfo)
        {
            var valueLabel = menuItemInfo.Label;
            var valueKey = menuItemInfo.Key;

            var value = String.Empty;
            if (CurrentDevice.UpdatedData.ContainsKey (valueKey)) {
                value = CurrentDevice.UpdatedData [valueKey];
            } else if (CurrentDevice.Data.ContainsKey (valueKey)) {
                value = CurrentDevice.Data [valueKey];
            }

            if (!CurrentDevice.Data.ContainsKey (valueKey))
                CurrentDevice.Data [valueKey] = value;
            value = FixValueForDisplay (value, valueKey, CurrentDevice);

            // Send the second line to the display
            var message = valueLabel + " " + value + menuItemInfo.PostFix;
            SendMessageToDisplay (1, message);

        }

        public void RenderSubItemCommand (CommandMenuItemInfo menuItemInfo)
        {
            var valueLabel = menuItemInfo.Label;
            var valueKey = menuItemInfo.Key;

            var value = String.Empty;
            if (CurrentDevice.UpdatedData.ContainsKey (valueKey)) {
                value = CurrentDevice.UpdatedData [valueKey];
            } else if (CurrentDevice.Data.ContainsKey (valueKey)) {
                value = CurrentDevice.Data [valueKey];
            }

            if (!CurrentDevice.Data.ContainsKey (valueKey))
                CurrentDevice.Data [valueKey] = value;

            value = FixValueForDisplay (value, valueKey, CurrentDevice);

            // Send the second line to the display
            var message = valueLabel + " " + value + menuItemInfo.PostFix;
            SendMessageToDisplay (1, message);
        }

        public string FixValueForDisplay (string value, string key, DeviceInfo info)
        {
            var menuItemInfo = GetMenuItemInfoByIndex (CurrentDevice.DeviceGroup, SubMenuIndex);

            var fixedValue = value;

            if (String.IsNullOrEmpty (value)) {
                fixedValue = menuItemInfo.DefaultValue;
            } else if (menuItemInfo is MqttMenuItemInfo) {
                var mqttMenuItemInfo = (MqttMenuItemInfo)menuItemInfo;
                if (mqttMenuItemInfo.Options != null && mqttMenuItemInfo.Options.Count > 0) {
                    var optionIndex = 0;
                    Int32.TryParse (value, out optionIndex);
                    fixedValue = mqttMenuItemInfo.Options [optionIndex];
                }
            } else if (menuItemInfo is CommandMenuItemInfo) {
                var mqttMenuItemInfo = (CommandMenuItemInfo)menuItemInfo;
                if (mqttMenuItemInfo.Options != null && mqttMenuItemInfo.Options.Count > 0) {
                    var optionIndex = 0;
                    Int32.TryParse (value, out optionIndex);
                    fixedValue = GetCommandOptionKeyByIndex (mqttMenuItemInfo, optionIndex);
                }
            }
            return fixedValue;
        }

        public string GetCommandOptionKeyByIndex (CommandMenuItemInfo menuItemInfo, int index)
        {
            var output = "";
            var i = 0;
            foreach (var option in menuItemInfo.Options) {
                if (i == index) {
                    output = option.Key;
                    break;
                }
                i++;
            }
            return output;
        }

        public string GetCommandOptionValueByIndex (CommandMenuItemInfo menuItemInfo, int index)
        {
            var i = 0;
            foreach (var option in menuItemInfo.Options) {
                if (i == index)
                    return option.Value;
                i++;
            }
            return String.Empty;
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

        public BaseMenuItemInfo GetMenuItemInfoByIndex (string deviceGroup, int subMenuIndex)
        {
            if (!MenuStructure.ContainsKey (deviceGroup))
                return null;

            return GetMenuItemInfoByIndex (MenuStructure [deviceGroup], subMenuIndex);
        }

        public BaseMenuItemInfo GetMenuItemInfoByIndex (MenuInfo menuStructure, int subMenuIndex)
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
                SubMenuUp ();
            }
        }

        public void SubMenuUp ()
        {
            var menuItemInfo = GetMenuItemInfoByIndex (CurrentDevice.DeviceGroup, SubMenuIndex);

            if (menuItemInfo.IsEditable) {
                var key = menuItemInfo.Key;

                var existingValueString = String.Empty;

                if (!CurrentDevice.UpdatedData.ContainsKey (key)) {
                    if (!CurrentDevice.Data.ContainsKey (key))
                        CurrentDevice.Data [key] = menuItemInfo.DefaultValue.ToString ();
                    CurrentDevice.UpdatedData [key] = CurrentDevice.Data [key];
                }

                existingValueString = CurrentDevice.UpdatedData [key];

                var existingValue = 0;
                Int32.TryParse (existingValueString, out existingValue);

                existingValue++;

                if (existingValue > menuItemInfo.MaxValue)
                    existingValue = menuItemInfo.MinValue;
                if (existingValue < menuItemInfo.MinValue)
                    existingValue = menuItemInfo.MaxValue;

                CurrentDevice.UpdatedData [key] = existingValue.ToString ();
                   

                HasChanged = true;
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
                SubMenuIndex = 0;
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
                SubMenuIndex = 0;
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
            var menuItemInfo = GetMenuItemInfoByIndex (CurrentDevice.DeviceGroup, SubMenuIndex);
            var isEditingValue = DeviceIsSelected && menuItemInfo.IsEditable;
            var valueHasChanged = false;
            if (CurrentDevice.Data.ContainsKey (menuItemInfo.Key)) {
                if (CurrentDevice.UpdatedData.ContainsKey (menuItemInfo.Key) &&
                    CurrentDevice.UpdatedData [menuItemInfo.Key] != CurrentDevice.Data [menuItemInfo.Key]) {
                    valueHasChanged = true;
                }
            } else
                valueHasChanged = true;
                    
            if (isEditingValue && valueHasChanged) {
                SubmitSelection ();
            } else {
                DeviceIsSelected = !DeviceIsSelected;
            }
            HasChanged = true;
        }

        public void SubmitSelection ()
        {
            var menuItemInfo = GetMenuItemInfoByIndex (CurrentDevice.DeviceGroup, SubMenuIndex);
            if (menuItemInfo is MqttMenuItemInfo)
                PublishUpdatedValue ();
            else if (menuItemInfo is CommandMenuItemInfo)
                RunSelectedCommand ();
        }

        public void RunSelectedCommand ()
        {
            var menuItemInfo = (CommandMenuItemInfo)GetMenuItemInfoByIndex (CurrentDevice.DeviceGroup, SubMenuIndex);

            var optionIndex = 0;
            if (CurrentDevice.UpdatedData.ContainsKey (menuItemInfo.Key)) {
                var indexString = CurrentDevice.UpdatedData [menuItemInfo.Key];
                Int32.TryParse (indexString, out optionIndex);
            }

            var command = GetCommandOptionValueByIndex (menuItemInfo, optionIndex);

            // Reset back to the default option
            CurrentDevice.UpdatedData [menuItemInfo.Key] = 0.ToString ();
            CurrentDevice.Data [menuItemInfo.Key] = 0.ToString ();

            SendMessageToDisplay (menuItemInfo.StartedText);

            Starter.Start (command);

            Thread.Sleep (3000); // TODO: Make this set by a property

            if (Starter.IsError)
                SendMessageToDisplay ("Error\noccurred.");
            else {
                SendMessageToDisplay (0, "Success.");
                SendMessageToDisplay (1, "                ");
            }

            Thread.Sleep (3000); // TODO: Make this set by a property
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
            // TODO: Remove if not needed
            //EnableIncomingMqtt = false;
            //Thread.Sleep (2000);
        }

        public void EnableMqtt ()
        {
            //EnableIncomingMqtt = true;
        }

        public void PublishUpdatedValue ()
        {
            var key = GetMenuItemInfoByIndex (CurrentDevice.DeviceGroup, SubMenuIndex).Key;

            var deviceTopic = "/" + CurrentDevice.DeviceName;
            var fullTopic = deviceTopic + "/" + key + "/in";

            if (CurrentDevice.UpdatedData.ContainsKey (key) &&
                CurrentDevice.UpdatedData [key] != CurrentDevice.Data [key]) {
                var value = CurrentDevice.UpdatedData [key];

                CurrentDevice.Data [key] = value;

                MqttClient.Publish (fullTopic, value);
            }
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

                if (fullMessage.Trim ().Contains ("\n") && !fullMessage.Contains ("|")) {
                    var parts = fullMessage.Split ('\n');

                    if (parts.Length > 0)
                        Client.WriteLine ("0|" + parts [0]);
                    if (parts.Length > 1)
                        Client.WriteLine ("1|" + parts [1]);
                } else {
                    Client.WriteLine (fullMessage);
                }
            } catch (Exception ex) {
                Console.WriteLine ("Failed to send message to device");
                Console.WriteLine (ex.ToString ());
                Console.WriteLine ();
                Console.WriteLine ("Waiting for " + WaitTimeBeforeRetry + " seconds then retrying");

                Thread.Sleep (WaitTimeBeforeRetry * 1000);

                SendMessageToDisplay (fullMessage);
            }
        }

        public void PublishStatusToMqtt ()
        {
            var isTimeToPublish = LastMqttStatusPublished.AddSeconds (MqttStatusPublishIntervalInSeconds) < DateTime.Now;

            if (isTimeToPublish) {
                LastMqttStatusPublished = DateTime.Now;

                var deviceTopic = "/" + DeviceName;
                var fullTopic = deviceTopic + "/Time";

                var value = DateTime.Now.ToString ();

                MqttClient.Publish (fullTopic, value);
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
                            if (message != "Online") {
                                Console.WriteLine ("Alert on device: " + deviceLabel);
                                Console.WriteLine ("  " + message);
                                Alerts.Enqueue ("0|" + deviceLabel + "\r\n1|" + message);
                            }
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

