using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.Events;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

[Serializable]
public class SendMessageEntry
{
    public MonoBehaviour targetComponent;
    public string targetMethodName;
    public string commandName;
}

public class YAGMultiplayer : MonoBehaviour
{
    [SerializeField]
    private string _brokerUrl = "";
    public string brokerUrl
    {
        get { return _brokerUrl; }
        set
        {
            if (!IsUrlValid(value))
            {
                Debug.LogError("Invalid URL Format.");
            }
            else
            {
                _brokerUrl = value;
            }
        }
    }

    [SerializeField]
    private string _backupBrokerUrl = "";
    public string backupBrokerUrl
    {
        get { return _backupBrokerUrl; }
        set
        {
            if (!string.IsNullOrEmpty(value) && !IsUrlValid(value))
            {
                Debug.LogError("Invalid URL Format.");
            }
            else
            {
                _backupBrokerUrl = value;
            }
        }
    }

    public bool generateBrockerUrl;
    public bool generateBackupBrockerUrl;

    public string password = "enterapassword";
    public string channel = "defaultchannel";
    public List<string> subChannels = new List<string>();

    public List<SendMessageEntry> sendMessageEntries = new List<SendMessageEntry>();

    public UnityEvent<string> onMessageReceived;

    public bool doNotDestroyOnLoad = true;
    public bool logEvents = false;

    public UnityEvent onConnected;
    public UnityEvent onDisconnected;

    private MqttClient client;

    public static YAGMultiplayer instance;

    private bool invoker = false;
    private string invokerMessage = "";

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            if (doNotDestroyOnLoad)
                DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (!generateBrockerUrl && string.IsNullOrEmpty(brokerUrl))
        {
            Debug.LogError("Broker URL is required.");
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            Debug.LogError("Password is required. If you dont have one, you can create by pressing \"Generate random password\".");
            return;
        }

        if (string.IsNullOrEmpty(channel))
        {
            Debug.LogError("Channel is required.");
            return;
        }

        Connect();
    }

    public void Connect()
    {
        if (generateBrockerUrl) brokerUrl = "broker.hivemq.com";
        if (generateBackupBrockerUrl) backupBrokerUrl = "test.mosquitto.org";

        try
        {
            using (var testClient = new TcpClient(brokerUrl, 1883))
            {
                client = new MqttClient(brokerUrl);
                client.MqttMsgPublishReceived += Client_MqttMsgPublishReceived;

                string clientId = Guid.NewGuid().ToString();
                client.Connect(clientId);

                SubscribeToTopic();
                OnConnected();
                client.ConnectionClosed += OnDisconnected;
            }
        }
        catch (Exception)
        {
            if (!string.IsNullOrEmpty(backupBrokerUrl))
            {
                try
                {
                    client = new MqttClient(backupBrokerUrl);
                    client.MqttMsgPublishReceived += Client_MqttMsgPublishReceived;

                    string clientId = Guid.NewGuid().ToString();
                    client.Connect(clientId);
                    Debug.LogWarning("Unable to connect to the main server. Switching to backup server.");
                    SubscribeToTopic();
                    OnConnected();
                    client.ConnectionClosed += OnDisconnected;
                }
                catch (Exception)
                {
                    Debug.LogError("Unable to connect to both the main server and the backup server.");
                }
            }
            else
            {
                Debug.LogError("Unable to connect to the main server. Backup server not found.");
            }
        }
    }

    private void SubscribeToTopic()
    {
        string topic = $"{password}/{channel}/";
        foreach (var subChannel in subChannels)
        {
            topic += subChannel + "/";
        }

        client.Subscribe(new string[] { topic.TrimEnd('/') }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
    }

    private void Client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
    {
        string message = System.Text.Encoding.UTF8.GetString(e.Message);
        invoker = true;
        invokerMessage = message;
    }

    private void Update()
    {
        if (invoker)
        {
            if (invokerMessage.StartsWith("</>command;;</>"))
            {
                SendMessageToTargets(invokerMessage);
            }
            else
            {
                onMessageReceived?.Invoke(invokerMessage);
                if (logEvents) Debug.Log("Data Received: " + invokerMessage);

            }
            invoker = false;
        }
    }

    private void SendMessageToTargets(string message)
    {
        foreach (var entry in sendMessageEntries)
        {
            if (entry.targetComponent != null)
            {
                var methodInfo = entry.targetComponent.GetType().GetMethod(entry.targetMethodName, new Type[] { typeof(string) });
                if (methodInfo != null)
                {
                    string veri = message;

                    string[] slices = veri.Split(new string[] { ";</>" }, StringSplitOptions.RemoveEmptyEntries);

                    if (slices.Length >= 3)
                    {
                        string nameOfCommand = slices[1];
                        string parameter = slices[2];
                        if(nameOfCommand == entry.commandName)
                        {
                            methodInfo.Invoke(entry.targetComponent, new object[] { parameter });
                        }
                        if (logEvents) Debug.Log("Received command " + entry.commandName + " with parameter " + parameter);
                    }
                    else
                    {
                        Debug.LogError("Command error occured.");
                    }
                }
                else
                {
                    Debug.LogError($"Function '{entry.targetMethodName}' not found in component '{entry.targetComponent.gameObject.name}'.");
                }
            }
            else
            {
                Debug.LogError("No method has been set for all commands.");
            }
        }
    }

    public void AddSendMessageEntry()
    {
        sendMessageEntries.Add(new SendMessageEntry());
    }

    public void RemoveSendMessageEntry(int index)
    {
        if (index >= 0 && index < sendMessageEntries.Count)
        {
            sendMessageEntries.RemoveAt(index);
        }
    }

    public void GenerateRandomPassword()
    {
        password = Guid.NewGuid().ToString().Substring(0, 32); 
    }

    public bool TestConnection()
    {
        if (generateBrockerUrl) brokerUrl = "broker.hivemq.com";

        try
        {
            using (var testClient = new TcpClient(brokerUrl, 1883))
            {
                if (logEvents) Debug.Log("Test Successful");
                return true;
            }
        }
        catch (Exception)
        {
            if (logEvents) Debug.Log("Test Failed. Main server did not respond.");
            return false;
        }
    }

    private bool IsUrlValid(string url)
    {
        if (string.IsNullOrEmpty(url))
            return false;

        // URL should not contain '/', ':' and must contain at least one '.'
        if (url.Contains("/") || url.Contains(":") || !url.Contains("."))
            return false;

        return true;
    }

    public void SendData(string data)
    {
        try
        {
            string topic = $"{password}/{channel}/";
            foreach (var subChannel in subChannels)
            {
                topic += subChannel + "/";
            }
            client.Publish(topic.TrimEnd('/'), System.Text.Encoding.UTF8.GetBytes(data), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
            if (logEvents) Debug.Log("Data Sent: " + data);
        }
        catch (Exception)
        {
            Debug.LogError("An error occurred while sending data.");
        }
    }

    public void SendCommand(string commandName, string parameter)
    {
        try
        {
            string topic = $"{password}/{channel}/";
            foreach (var subChannel in subChannels)
            {
                topic += subChannel + "/";
            }
            string data = $"</>command;;</>{commandName};</>{parameter}";
            client.Publish(topic.TrimEnd('/'), System.Text.Encoding.UTF8.GetBytes(data), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
            if (logEvents) Debug.Log("Sent command " + commandName + " with parameter " + parameter);
        }
        catch (Exception)
        {
            Debug.LogError("An error occurred while sending the command.");
        }
    }
    private void OnConnected()
    {
        if (logEvents) Debug.Log("Connected to server.");
        onConnected?.Invoke();
    }

    private void OnDisconnected(object sender, EventArgs e)
    {
        if (logEvents) Debug.Log("Connection lost.");
        onDisconnected?.Invoke();
    }
}