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
using uPLibrary.Networking.M2Mqtt.Exceptions;
using System.Linq;

namespace Serial1602ShieldSystemUIController
{
  public class SystemMenuController : IDisposable
  {
    public bool IsVerbose;
    public SerialClientWrapper Client = null;
    public int WaitTimeBeforeRetry = 3;
    public Dictionary<string, DeviceInfo> DeviceList = new Dictionary<string, DeviceInfo> ();
    public string DevicesDirectory = String.Empty;
    public string TargetDirectory = String.Empty;
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
    public int AlertDisplayDuration = 5;
    public DateTime AlertDisplayStartTime;
    public int SleepTimeBetweenLoops = 200;
    public int MqttStatusPublishIntervalInSeconds = 5;
    public DateTime LastMqttStatusPublished = DateTime.MinValue;
    public ProcessStarter Starter = new ProcessStarter ();
    public MenuInfoCreator MenuCreator = new MenuInfoCreator ();
    public bool IsInitialized = false;
    public string SelfHostName = "";
    public bool ShowLocalDevicesOnly = false;
    public bool IsConnected = false;

    public SystemMenuController ()
    {
      MqttClient = new MqttClientWrapper ();
    }

    public void Run ()
    {
      Initialize ();


      if (String.IsNullOrEmpty (DevicesDirectory))
        throw new Exception ("DevicesDirectory property not set.");
      if (!Directory.Exists (DevicesDirectory)) {
        Console.WriteLine ("Devices directory not found:");
        Console.WriteLine ("  " + DevicesDirectory);
        Console.WriteLine ("Creating devices directory...");
        Directory.CreateDirectory (DevicesDirectory);
      }

      Console.WriteLine ("Devices directory:");
      Console.WriteLine (DevicesDirectory);

      Console.WriteLine ("Device Name: " + DeviceName);
      Console.WriteLine ("MQTT Host: " + MqttHost);
      Console.WriteLine ("MQTT Username: " + MqttUsername);
      Console.WriteLine ("MQTT Port: " + MqttPort);

      InitializeSerialPort ();

      Console.WriteLine ("Device name: " + DeviceName);
      Console.WriteLine ("Serial port name: " + SerialPortName);

      SetupMQTT ();

      ConnectToSerialDevice ();

      var isRunning = true;
      while (isRunning) {
        try {
          RunLoop ();
                        
          Thread.Sleep (SleepTimeBetweenLoops);
                    

        } catch (Exception ex) {
          Console.WriteLine ("An error occurred:");
          Console.WriteLine (ex.ToString ());
          Console.WriteLine ();
          Console.WriteLine ("Waiting for " + WaitTimeBeforeRetry + " seconds then retrying");

          SendErrorEmail (ex, DeviceName, SmtpServer, EmailAddress);

          Thread.Sleep (WaitTimeBeforeRetry * 1000);

          Run ();
        }
      }
    }

    public void Initialize ()
    {
      if (!IsInitialized) {
        MenuCreator.Create (this);

        IsInitialized = true;

        if (String.IsNullOrEmpty (SelfHostName))
          SelfHostName = GetSelfHostName ();

        DevicesDirectory = Path.GetFullPath (DevicesDirectory);
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

    public void ConnectToSerialDevice ()
    {
      if (!Client.IsOpen)
        Client.Open ();

      Thread.Sleep (1000);

      // Wait until the first line arrives
      ProcessLineFromDevice (Client.ReadLine ());

    }

    public void DisplayConnectedOnDevice ()
    {
      SendMessageToDisplay (0, "Connected!");
      SendMessageToDisplay (1, "                ");

      Thread.Sleep (2000);
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
      while (!IsConnected) {
        try {
          var clientId = Guid.NewGuid ().ToString ();

          MqttClient.MqttMsgPublishReceived += client_MqttMsgPublishReceived;

          MqttClient.Connect (MqttHost, MqttPort, clientId, MqttUsername, MqttPassword);

          IsConnected = true;
                    
          Console.WriteLine ("Connected to MQTT broker");
        } catch (Exception ex) {
          Console.WriteLine ("Error: Failed to connect to MQTT broker");
          Console.WriteLine ("Host: " + MqttHost);
          Console.WriteLine ("Port: " + MqttPort);
          Console.WriteLine ("Username: " + MqttUsername);

          Console.WriteLine (ex.ToString ());

          Console.WriteLine ("Retrying in " + WaitTimeBeforeRetry + " seconds...");

          Thread.Sleep (WaitTimeBeforeRetry * 1000);
        }
      }


      var subscribeTopics = new string[] {
        "garden/#"
      };

      foreach (var topic in subscribeTopics) {
        MqttClient.Subscribe (new string[] { topic }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
      }
    }

    public void RunLoop ()
    {
      if (IsVerbose)
        Console.WriteLine ("== Start UI Controller Loop");

      LoopNumber++;

      EnsureConnectedToMQTT ();

      RemoveLostDevices ();

      AddNewDevices ();

      RenderDisplayOnDevice ();

      EnsureConnectedToSerialDevice ();

      if (Client.HasData) {
        var line = Client.ReadLine ();

        ProcessLineFromDevice (line.Trim ());
      }

      PublishStatusToMqtt ();

      if (IsVerbose)
        Console.WriteLine ("== End UI Controller Loop");
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
      var deviceTopicPattern = deviceInfo.DeviceName + "/#";
      Console.WriteLine ("Subscribe topic: " + deviceTopicPattern);
      list.Add (deviceTopicPattern);
      return list.ToArray ();
    }

    public void EnsureConnectedToMQTT ()
    {
      if (MqttClient == null || MqttClient.Client == null || !MqttClient.Client.IsConnected)
        SetupMQTT ();
    }

    public void EnsureConnectedToSerialDevice ()
    {
      if (!Client.IsOpen) {
        ConnectToSerialDevice ();
      }
    }

    public void ResetDevices ()
    {
      DeviceList.Clear ();
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
          if (deviceInfo != null) {
            var isEnabledOnDisplay = deviceInfo.DeviceHost == SelfHostName || !ShowLocalDevicesOnly;
            if (isEnabledOnDisplay)
              AddDevice (deviceInfo);
          }
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
            
      var labelFilePath = Path.Combine (deviceInfoDir, "label.txt");
      if (!File.Exists (labelFilePath))
        return null;

      var groupFilePath = Path.Combine (deviceInfoDir, "group.txt");
      if (!File.Exists (groupFilePath))
        return null;

      var hostFilePath = Path.Combine (deviceInfoDir, "host.txt");
      if (!File.Exists (hostFilePath))
        return null;

      deviceInfo.DeviceLabel = File.ReadAllText (Path.Combine (deviceInfoDir, "label.txt")).Trim ();
      deviceInfo.DeviceGroup = File.ReadAllText (Path.Combine (deviceInfoDir, "group.txt")).Trim ();
      deviceInfo.DeviceHost = File.ReadAllText (Path.Combine (deviceInfoDir, "host.txt")).Trim ();

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
      if (MenuIndex > index) {
        MenuIndex--;
        SubMenuIndex = 0;
      }
    }

    public void ProcessLineFromDevice (string line)
    {
      if (!String.IsNullOrWhiteSpace (line)) {
        Console.WriteLine ("Processing line:");
        Console.WriteLine (line);

        if (line.StartsWith ("D;"))
          ProcessDataLineFromDevice (line);
        else if (line == "Starting...") {
          StartDisplayOnDevice ();
        } else if (line == "up")
          PressMenuUp ();
        else if (line == "down")
          PressMenuDown ();
        else if (line == "left")
          PressMenuLeft ();
        else if (line == "right")
          MenuRight ();
        else if (line == "select")
          PressMenuSelect ();
      }
    }

    public void ProcessDataLineFromDevice (string line)
    {
      if (line.StartsWith ("D;")) {
        var parts = line.Trim ().Trim (';').Trim (';').Split (';');

        var deviceTopic = DeviceName;

        foreach (var part in parts) {
          if (part.Contains (":")) {
            var subParts = part.Split (':');
            var key = subParts [0];
            var value = subParts [1];

            var fullTopic = deviceTopic + "/" + key;

            MqttClient.Publish (fullTopic, value);
          }
        }

        var timeTopic = deviceTopic + "/Time";

        var dateValue = DateTime.Now.ToString ();

        MqttClient.Publish (timeTopic, dateValue);
      }
    }

    public void StartDisplayOnDevice ()
    {
      EnsureConnectedToSerialDevice ();
      DisplayConnectedOnDevice ();
      HasChanged = true;
      MenuIndex = 0;
      SubMenuIndex = 0;
      DeviceIsSelected = false;
    }

    public void RenderDisplayOnDevice ()
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
          if (menuItemInfo is UploadSketchMenuItemInfo)
            RenderSubItemUploadSketch ((UploadSketchMenuItemInfo)menuItemInfo);
          if (menuItemInfo is BashCommandMenuItemInfo)
            RenderSubItemCommand ((BashCommandMenuItemInfo)menuItemInfo);
          if (typeof(BaseCodeMenuItemInfo).IsAssignableFrom (menuItemInfo.GetType ()))
            RenderSubItemCode ((BaseCodeMenuItemInfo)menuItemInfo);

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

    public void RenderSubItemUploadSketch (UploadSketchMenuItemInfo menuItemInfo)
    {
      var message = String.Empty;
      switch (menuItemInfo.StepIndex) {
      case 0:
        var valueLabel = menuItemInfo.Label;
        message = valueLabel;
        break;
      case 1:
        var selectedSketch = menuItemInfo.SelectedSketchIndex;
        message = menuItemInfo.GetSketchInfoList () [selectedSketch].Label;
        break;
      case 2:
        var selectedBoard = menuItemInfo.SelectedBoardIndex;
        message = menuItemInfo.GetBoardList () [selectedBoard];
        break;
      }

      SendMessageToDisplay (1, message);
    }

    public void RenderSubItemCommand (BashCommandMenuItemInfo menuItemInfo)
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

    public void RenderSubItemCode (BaseCodeMenuItemInfo menuItemInfo)
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
      var message = valueLabel + " " + value;
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
      } else if (menuItemInfo is BashCommandMenuItemInfo) {
        var mqttMenuItemInfo = (BashCommandMenuItemInfo)menuItemInfo;
        if (mqttMenuItemInfo.Options != null && mqttMenuItemInfo.Options.Count > 0) {
          var optionIndex = 0;
          Int32.TryParse (value, out optionIndex);
          fixedValue = GetCommandOptionKeyByIndex (mqttMenuItemInfo, optionIndex);
        }
      } else if (menuItemInfo is DeviceFilterMenuItemInfo) {
        var filterMenuItemInfo = (DeviceFilterMenuItemInfo)menuItemInfo;
        if (filterMenuItemInfo.Options != null && filterMenuItemInfo.Options.Count > 0) {
          var optionIndex = 0;
          Int32.TryParse (value, out optionIndex);
          fixedValue = filterMenuItemInfo.Options [optionIndex];
        }
      }
      return fixedValue;
    }

    public string GetCommandOptionKeyByIndex (BashCommandMenuItemInfo menuItemInfo, int index)
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

    public string GetCommandOptionValueByIndex (BashCommandMenuItemInfo menuItemInfo, int index)
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
      var devices = new List<DeviceInfo> (DeviceList.Values);
      return devices [deviceIndex];
    }

    public BaseMenuItemInfo GetCurrentMenuItemInfo ()
    {
      return GetMenuItemInfoByIndex (CurrentDevice.DeviceGroup, SubMenuIndex);
    }

    public BaseMenuItemInfo GetMenuItemInfoByIndex (string deviceGroup, int subMenuIndex)
    {
      if (!MenuStructure.ContainsKey (deviceGroup))
        throw new ArgumentException ("Group '" + deviceGroup + "' not supported.");

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

    public void PressMenuUp ()
    {
      if (Alerts.Count > 0) {
        CancelAlert ();
      } else if (DeviceIsSelected) {
        PressSubMenuUp ();
      }
    }

    public void PressSubMenuUp ()
    {
      var menuItemInfo = GetMenuItemInfoByIndex (CurrentDevice.DeviceGroup, SubMenuIndex);

      if (menuItemInfo is UploadSketchMenuItemInfo) {
        ((UploadSketchMenuItemInfo)menuItemInfo).Up ();
        HasChanged = true;
      } else if (menuItemInfo.IsEditable) {
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

    public void PressSubMenuDown ()
    {
      var menuItemInfo = GetMenuItemInfoByIndex (CurrentDevice.DeviceGroup, SubMenuIndex);

      if (menuItemInfo is UploadSketchMenuItemInfo) {
        ((UploadSketchMenuItemInfo)menuItemInfo).Up ();
        HasChanged = true;
      } else if (menuItemInfo.IsEditable) {
        var key = menuItemInfo.Key;

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

        if (existingValue > menuItemInfo.MaxValue)
          existingValue = menuItemInfo.MinValue;
        if (existingValue < menuItemInfo.MinValue)
          existingValue = menuItemInfo.MaxValue;

        CurrentDevice.UpdatedData [key] = existingValue.ToString ();
                   
        HasChanged = true;
      }
    }

    public void SubMenuDown ()
    {
      var menuItemInfo = GetMenuItemInfoByIndex (CurrentDevice.DeviceGroup, SubMenuIndex);

      if (menuItemInfo is UploadSketchMenuItemInfo) {
        ((UploadSketchMenuItemInfo)menuItemInfo).Down ();
      } else if (menuItemInfo.IsEditable) {
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

    public void PressMenuDown ()
    {
      if (Alerts.Count > 0) {
        CancelAlert ();
      } else if (!DeviceIsSelected) {
        DeviceIsSelected = true;
        HasChanged = true;
      } else {
        PressSubMenuDown ();
      }
    }

    public void PressMenuLeft ()
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

    public void PressMenuSelect ()
    {
      var menuItemInfo = GetMenuItemInfoByIndex (CurrentDevice.DeviceGroup, SubMenuIndex);
      if (menuItemInfo is UploadSketchMenuItemInfo) {
        ((UploadSketchMenuItemInfo)menuItemInfo).Select ();
      } else {
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
      }
      HasChanged = true;
    }

    public void SubmitSelection ()
    {
      var menuItemInfo = GetMenuItemInfoByIndex (CurrentDevice.DeviceGroup, SubMenuIndex);
      if (menuItemInfo is MqttMenuItemInfo)
        PublishUpdatedValue ();
      else if (typeof(BashCommandMenuItemInfo).IsAssignableFrom (menuItemInfo.GetType ()))
        RunSelectedBashCommand ();
      else if (typeof(BaseCodeMenuItemInfo).IsAssignableFrom (menuItemInfo.GetType ()))
        RunSelectedCodeCommand ();
    }

    public void RunSelectedBashCommand ()
    {
      var menuItemInfo = (BashCommandMenuItemInfo)GetMenuItemInfoByIndex (CurrentDevice.DeviceGroup, SubMenuIndex);

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

      command = FixCommand (command);

      Starter.Start (command);

      // TODO: Clean up. Alerts aren't working here.
      if (Starter.IsError) {
        //Alerts.Enqueue ("0|Error\r\n1|occurred");
        SendMessageToDisplay ("Error\noccurred.");
      } else {
        //Alerts.Enqueue ("0|Successful\r\n1|                ");
        SendMessageToDisplay (0, "Successful");
        SendMessageToDisplay (1, "                ");
      }

      Thread.Sleep (AlertDisplayDuration * 1000);
    }

    public string FixCommand (string originalCommand)
    {
      var fixedCommand = originalCommand;
      fixedCommand = fixedCommand.Replace ("{DEVICE_NAME}", CurrentDevice.DeviceName);
      if (!String.IsNullOrEmpty (TargetDirectory))
        fixedCommand = "cd " + TargetDirectory + " && " + fixedCommand;
      return fixedCommand;
    }

    public void RunSelectedCodeCommand ()
    {
      var menuItemInfo = (BaseCodeMenuItemInfo)GetMenuItemInfoByIndex (CurrentDevice.DeviceGroup, SubMenuIndex);

      var optionIndex = 0;
      if (CurrentDevice.UpdatedData.ContainsKey (menuItemInfo.Key)) {
        var indexString = CurrentDevice.UpdatedData [menuItemInfo.Key];
        Int32.TryParse (indexString, out optionIndex);
      }

      menuItemInfo.Execute (optionIndex);
    }

    public void CancelAlert ()
    {
      Alerts.Dequeue ();
      AlertDisplayStartTime = DateTime.MinValue;
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

      var deviceTopic = CurrentDevice.DeviceName;
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
          var subject = "Error: System UI controller on '" + SelfHostName + "'";
          var body = "The following error was thrown by the system UI controller...\n\nSource host: " + SelfHostName + "\n\nDevice name: " + deviceName + "\n\n" + error.ToString ();

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
        EnsureConnectedToSerialDevice ();

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
      // TODO: Remove if not needed. Status should be published when the sketch outputs data.
      /*var isTimeToPublish = LastMqttStatusPublished.AddSeconds (MqttStatusPublishIntervalInSeconds) < DateTime.Now;

            if (isTimeToPublish) {
                LastMqttStatusPublished = DateTime.Now;

                var deviceTopic = "/" + DeviceName;
                var fullTopic = deviceTopic + "/Time";

                var value = DateTime.Now.ToString ();

                MqttClient.Publish (fullTopic, value);
            }*/
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
            ProcessIncomingMessageForDevice (deviceName, subTopic, message);
          } else {
            if (subTopic == "StatusMessage") {
              if (message != "Online") {
                Console.WriteLine ("Alert for garden");
                Console.WriteLine ("  " + message);
                Alerts.Enqueue ("0|Garden\n1|" + message);
              }
            }
          }
        }
      }
    }

    public void ProcessIncomingMessageForDevice (string deviceName, string subTopic, string message)
    {
      if (IsVerbose) {
        Console.WriteLine ("Device name: " + deviceName);
        Console.WriteLine ("Subtopic: " + subTopic);
        Console.WriteLine ("Message: " + message);
      }

      var menuItemInfo = GetMenuItemInfoByIndex (CurrentDevice.DeviceGroup, SubMenuIndex);

      if (menuItemInfo != null) {
        var originalValue = menuItemInfo.DefaultValue;
                            
        var deviceInfo = DeviceList [deviceName];

        if (deviceInfo.Data.ContainsKey (subTopic))
          originalValue = deviceInfo.Data [subTopic];

        deviceInfo.Data [subTopic] = message;

        var deviceLabel = DeviceList [deviceName].DeviceLabel;

        if (subTopic == "StatusMessage") {
          if (message != "Online") {
            Console.WriteLine ("Alert on device: " + deviceLabel);
            Console.WriteLine ("  " + message);
            Alerts.Enqueue ("0|" + deviceLabel + "\r\n1|" + message);
          }
        }

        var key = menuItemInfo.Key;

        var isCurrentlyViewing = (key == subTopic);
        var hasChanged = originalValue != message;
        if (isCurrentlyViewing && hasChanged)
          HasChanged = true;
      }
    }

    public string GetSelfHostName ()
    {
      Starter.WriteOutputToConsole = false;
      Starter.Start ("hostname");

      var selfHostName = "";
      if (!Starter.IsError)
        selfHostName = Starter.Output.Trim ();

      Console.WriteLine ("Host (self): " + selfHostName);

      return selfHostName;
    }

    public void Dispose ()
    {
      Client.Close ();
    }
  }
}

