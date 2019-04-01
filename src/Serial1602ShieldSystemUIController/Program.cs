using System;
using duinocom;
using System.Threading;
using System.Text;
using System.Configuration;
using System.IO;
using System.IO.Ports;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Diagnostics;
using System.Net.Mail;

namespace SerialSystemUI
{
    class MainClass
    {
        public static bool IsSubscribed = false;

        public static string IncomingKeyValueSeparator = "";

        public static bool IsVerbose;

        public static void Main (string[] args)
        {
            var arguments = new Arguments (args);

            Run (arguments);
        }

        public static void Run (Arguments arguments)
        {
            var controller = new SystemMenuController ();

            controller.MqttUsername = GetConfigValue (arguments, "UserId");
            controller.MqttUsername = GetConfigValue (arguments, "Password");
            controller.MqttHost = GetConfigValue (arguments, "Host");
            controller.MqttPort = Convert.ToInt32 (GetConfigValue (arguments, "MqttPort"));


            controller.SerialPortName = GetConfigValue (arguments, "SerialPort");
            controller.SerialBaudRate = Convert.ToInt32 (GetConfigValue (arguments, "SerialBaudRate"));

            controller.DeviceName = GetConfigValue (arguments, "DeviceName");

            controller.EmailAddress = GetConfigValue (arguments, "EmailAddress");
            controller.SmtpServer = GetConfigValue (arguments, "SmtpServer");

            controller.DevicesDirectory = GetConfigValue (arguments, "DevicesDirectory");

            IsVerbose = arguments.Contains ("v");
            controller.IsVerbose = IsVerbose;

            controller.Run ();
        }


        public static string GetConfigValue (Arguments arguments, string argumentKey)
        {
            var value = String.Empty;

            if (IsVerbose)
                Console.WriteLine ("Getting config/argument value for: " + argumentKey);

            if (arguments.Contains (argumentKey)) {
                value = arguments [argumentKey];
                if (IsVerbose)
                    Console.WriteLine ("Found in arguments");
            } else {

                try {
                    value = ConfigurationManager.AppSettings [argumentKey];
                } catch (Exception ex) {
                    Console.WriteLine ("Failed to get configuration value: " + argumentKey);
                    throw ex;
                }

                if (IsVerbose)
                    Console.WriteLine ("Looking in config");
            }

            return value;
        }

    }
}