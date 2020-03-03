using System;
using uPLibrary.Networking.M2Mqtt;
using System.Text;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace Serial1602ShieldSystemUIController
{
  public class MqttClientWrapper
  {
    public MqttClient Client;

    public virtual bool IsConnected {
      get { return Client != null && Client.IsConnected; }
    }

    public event MqttClient.ConnectionClosedEventHandler ConnectionClosed;
    public event MqttClient.MqttMsgPublishEventHandler MqttMsgPublishReceived;

    public MqttClientWrapper ()
    {
    }

    public virtual void Connect (string host, int port, string clientId, string username, string password)
    {
      Client = new MqttClient (host, port, false, null, null, MqttSslProtocols.None);
      Client.MqttMsgPublishReceived += Client_MqttMsgPublishReceived;
      Client.ConnectionClosed += Client_ConnectionClosed;

      Client.Connect (clientId, username, password);
    }

    public virtual void Subscribe (string[] topics, byte[] qos)
    {
      Client.Subscribe (topics, qos);
    }

    public virtual void Unsubscribe (string[] topics)
    {
      Client.Unsubscribe (topics);
    }

    public virtual void Publish (string topic, string value)
    {
      Publish (topic, Encoding.UTF8.GetBytes (value));
    }

    public virtual void Publish (string topic, byte[] value)
    {
      Client.Publish (topic, value, MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, true);
    }

    protected virtual void Client_MqttMsgPublishReceived (object sender, uPLibrary.Networking.M2Mqtt.Messages.MqttMsgPublishEventArgs e)
    {
      MqttMsgPublishReceived (sender, e);
    }

    void Client_ConnectionClosed (object sender, EventArgs e)
    {
      ConnectionClosed (sender, e);
    }
  }
}

