using System;
using duinocom;
using System.IO.Ports;
using System.IO;

namespace Serial1602ShieldSystemUIController
{
    public class SerialClientWrapper
    {
        public SerialClient Client;

        public virtual bool IsOpen {
            get { return Client.Port.IsOpen; }
        }

        public virtual bool HasData {
            get {
                return Client.Port.BytesToRead > 0;
            }
        }


        public SerialClientWrapper ()
        {
        }

        public SerialClientWrapper (SerialPort port)
        {
            Client = new SerialClient (port);
        }

        public SerialClientWrapper (SerialClient client)
        {
            Client = client;
        }

        public virtual void Open ()
        { 
            try {
                Client.Open ();
            } catch (IOException ex) {
                Console.WriteLine ("Failed to connect to the device at port: " + Client.Port.PortName);
                Console.WriteLine (ex.ToString ());
            }
        }

        public virtual string ReadLine ()
        {
            if (!IsOpen)
                Open ();
            return Client.ReadLine ();
        }

        public virtual void WriteLine (string text)
        {
            Client.WriteLine (text);
        }

        public void Close ()
        {
            Client.Close ();
        }
    }
}

