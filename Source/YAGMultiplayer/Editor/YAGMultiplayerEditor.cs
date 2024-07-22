using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;

[CustomEditor(typeof(YAGMultiplayer))]
public class YAGMultiplayerEditor : Editor
{
    SerializedProperty brokerUrlProp;
    SerializedProperty backupBrokerUrlProp;
    SerializedProperty passwordProp;
    SerializedProperty channelProp;
    SerializedProperty subChannelsProp;
    SerializedProperty sendMessageEntriesProp;
    SerializedProperty onMessageReceivedProp;
    SerializedProperty doNotDestroyOnLoadProp;
    SerializedProperty logEventsProp;
    SerializedProperty onConnectedProp;
    SerializedProperty onDisconnectedProp;
    SerializedProperty generateBrockerUrlProp;
    SerializedProperty generateBackupBrockerUrlProp;

    private string[] componentFunctionNames;
    private bool showJokerExplanation = false;
    private string testMessage = "";
    private bool showSendExplanation = false;

    private DateTime lastTestTime;

    void OnEnable()
    {
        brokerUrlProp = serializedObject.FindProperty("_brokerUrl");
        backupBrokerUrlProp = serializedObject.FindProperty("_backupBrokerUrl");
        passwordProp = serializedObject.FindProperty("password");
        channelProp = serializedObject.FindProperty("channel");
        subChannelsProp = serializedObject.FindProperty("subChannels");
        sendMessageEntriesProp = serializedObject.FindProperty("sendMessageEntries");
        onMessageReceivedProp = serializedObject.FindProperty("onMessageReceived");
        doNotDestroyOnLoadProp = serializedObject.FindProperty("doNotDestroyOnLoad");
        logEventsProp = serializedObject.FindProperty("logEvents");
        onConnectedProp = serializedObject.FindProperty("onConnected");
        onDisconnectedProp = serializedObject.FindProperty("onDisconnected");
        generateBrockerUrlProp = serializedObject.FindProperty("generateBrockerUrl");
        generateBackupBrockerUrlProp = serializedObject.FindProperty("generateBackupBrockerUrl");

        RefreshComponentFunctionNames();
    }

    private void RefreshComponentFunctionNames()
    {
        componentFunctionNames = new string[] { }; // Empty by default
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
        headerStyle.fontSize = 16;
        headerStyle.alignment = TextAnchor.MiddleCenter;
        EditorGUILayout.LabelField("Server Settings", headerStyle, GUILayout.Height(30), GUILayout.ExpandWidth(true));

        if(generateBrockerUrlProp.boolValue) { brokerUrlProp.stringValue = ""; }
        GUI.enabled = !generateBrockerUrlProp.boolValue;
        EditorGUILayout.PropertyField(brokerUrlProp, new GUIContent("Broker Url"));
        GUI.enabled = true;
        GUILayout.BeginHorizontal();
        GUILayout.Label("Generate For Me", GUILayout.Width(100)); // Label geniþliði
        generateBrockerUrlProp.boolValue = GUILayout.Toggle(generateBrockerUrlProp.boolValue, GUIContent.none, GUILayout.Width(20)); // Toggle geniþliði
        GUILayout.EndHorizontal();
        ValidateUrl(brokerUrlProp.stringValue, "Broker URL");
        EditorGUILayout.Space();

        if (generateBackupBrockerUrlProp.boolValue) { backupBrokerUrlProp.stringValue = ""; }
        GUI.enabled = !generateBackupBrockerUrlProp.boolValue;
        EditorGUILayout.PropertyField(backupBrokerUrlProp, new GUIContent("Backup Broker URL"));
        GUI.enabled = true;
        GUILayout.BeginHorizontal();
        GUILayout.Label("Generate For Me", GUILayout.Width(100)); // Label geniþliði
        generateBackupBrockerUrlProp.boolValue = GUILayout.Toggle(generateBackupBrockerUrlProp.boolValue, GUIContent.none, GUILayout.Width(20)); // Toggle geniþliði
        GUILayout.EndHorizontal();
        ValidateBackupBrokerUrl(backupBrokerUrlProp.stringValue);
        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(passwordProp, new GUIContent("Connection Password"));
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Generate Random Password", GUILayout.Width(175)))
        {
            GenerateRandomPassword();
        }
        GUILayout.EndHorizontal();
        if (string.IsNullOrEmpty(passwordProp.stringValue))
        {
            EditorGUILayout.HelpBox("Password cannot be empty.", MessageType.Error);
        }

        ValidatePassword(passwordProp.stringValue);

        EditorGUILayout.LabelField("Channel Settings", headerStyle, GUILayout.Height(30), GUILayout.ExpandWidth(true));

        EditorGUILayout.PropertyField(channelProp, new GUIContent("Channel Name"));
        if (string.IsNullOrEmpty(channelProp.stringValue))
        {
            EditorGUILayout.HelpBox("Channel cannot be empty.", MessageType.Error);
        }
        ValidateChannelName(channelProp.stringValue);
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(subChannelsProp, new GUIContent("Subchannels"), true);

        ValidateSubChannels();

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (showJokerExplanation)
        {
            if (GUILayout.Button("- Wildcard Characters", GUILayout.Width(140)))
            {
                showJokerExplanation = false;
            }
        }
        else
        {
            if (GUILayout.Button("+ Wildcard Characters", GUILayout.Width(140)))
            {
                showJokerExplanation = true;
            }
        }
        EditorGUILayout.EndHorizontal();

        if (showJokerExplanation)
        {
            EditorGUILayout.HelpBox("+: Writing + in a subchannel accepts all subchannels for that subchannel. For example, writing 3 subchannels as players/+/movement will receive or send data from all subchannels like players/player1/movement, players/player2/movement, etc.\n\n#: Writing # in the last subchannel accepts all subchannels of that subchannel. For example, writing 3 subchannels as players/player1/# will receive or send data from all subchannels like players/player1/movement, players/player1/inventory, etc.", MessageType.Info);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Data Settings", headerStyle, GUILayout.Height(30), GUILayout.ExpandWidth(true));

        if (GUILayout.Button("Add New Command"))
        {
            AddSendMessageEntry();
        }

        if (sendMessageEntriesProp.arraySize == 0)
        {
            EditorGUILayout.HelpBox("No commands have been added.", MessageType.Info);
        }
        else
        {
            for (int i = 0; i < sendMessageEntriesProp.arraySize; i++)
            {
                SerializedProperty entryProp = sendMessageEntriesProp.GetArrayElementAtIndex(i);
                DrawSendMessageEntry(entryProp, i);
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(onMessageReceivedProp, new GUIContent("On Data Received"));

        EditorGUILayout.LabelField("Tests", headerStyle, GUILayout.Height(30), GUILayout.ExpandWidth(true));

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Connection Test"))
        {
            TestConnection();
        }
        GUILayout.EndHorizontal();

        // Display test connection result message
        if (!string.IsNullOrEmpty(testMessage))
        {
            EditorGUILayout.HelpBox(testMessage, testMessage.Contains("success") ? MessageType.Info : MessageType.Error);
        }
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Other", headerStyle, GUILayout.Height(30), GUILayout.ExpandWidth(true));

        EditorGUILayout.PropertyField(doNotDestroyOnLoadProp, new GUIContent("Do Not Destroy On Load"));

        EditorGUILayout.PropertyField(logEventsProp, new GUIContent("Log Events"));
        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(onConnectedProp, new GUIContent("On Connected"));
        EditorGUILayout.PropertyField(onDisconnectedProp, new GUIContent("On Disconnected"));

        EditorGUILayout.BeginHorizontal();
        if (showSendExplanation)
        {
            if (GUILayout.Button("- How to Send Data"))
            {
                showSendExplanation = false;
            }
        }
        else
        {
            if (GUILayout.Button("+ How to Send Data"))
            {
                showSendExplanation = true;
            }
        }
        EditorGUILayout.EndHorizontal();

        if (showSendExplanation)
        {
            EditorGUILayout.HelpBox("Sending Plain Data\nYou can send plain data using\nYAGMultiplayer.instance.SendData(\"data\");", MessageType.Info);
            EditorGUILayout.HelpBox("Sending Command\nYou can send a command using\nYAGMultiplayer.instance.SendCommand(\"commandName\", \"parameter\");", MessageType.Info);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSendMessageEntry(SerializedProperty entryProp, int index)
    {
        SerializedProperty targetComponentProp = entryProp.FindPropertyRelative("targetComponent");
        SerializedProperty targetMethodNameProp = entryProp.FindPropertyRelative("targetMethodName");
        SerializedProperty commandNameProp = entryProp.FindPropertyRelative("commandName");

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.PropertyField(targetComponentProp, new GUIContent("Target Component"));

        // Fetch and display method names of selected MonoBehaviour
        MonoBehaviour selectedComponent = (MonoBehaviour)targetComponentProp.objectReferenceValue;
        if (selectedComponent != null)
        {
            FetchComponentFunctionNames(selectedComponent);

            if (componentFunctionNames.Length == 0)
            {
                EditorGUILayout.HelpBox("No suitable method found in the selected component.", MessageType.Error);
                targetMethodNameProp.stringValue = null; // Reset method selection
            }
            else
            {
                int selectedIndex = Array.IndexOf(componentFunctionNames, targetMethodNameProp.stringValue);
                selectedIndex = EditorGUILayout.Popup("Target Method", selectedIndex, componentFunctionNames);
                string selectedCommand = EditorGUILayout.TextField("Command Name", commandNameProp.stringValue);

                if (selectedIndex >= 0 && selectedIndex < componentFunctionNames.Length)
                {
                    targetMethodNameProp.stringValue = componentFunctionNames[selectedIndex];
                }
                else
                {
                    EditorGUILayout.HelpBox("Select a method.", MessageType.Error);
                    targetMethodNameProp.stringValue = null; // Reset method selection
                }

                if (selectedCommand.Length > 0)
                {
                    if (Regex.IsMatch(selectedCommand, @"^[a-zA-Z0-9]+$"))
                    {
                        commandNameProp.stringValue = selectedCommand;
                    }
                    else
                    {
                        commandNameProp.stringValue = selectedCommand;
                        EditorGUILayout.HelpBox("Invalid command name.", MessageType.Error);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Enter a command name.", MessageType.Error);
                    commandNameProp.stringValue = null;
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Select a MonoBehaviour to choose a method.", MessageType.Info);
            targetMethodNameProp.stringValue = null; // Reset method selection
            commandNameProp.stringValue = null;
        }

        // Check for duplicate entries
        for (int j = 0; j < sendMessageEntriesProp.arraySize; j++)
        {
            if (j != index)
            {
                SerializedProperty otherEntryProp = sendMessageEntriesProp.GetArrayElementAtIndex(j);
                SerializedProperty otherComponentProp = otherEntryProp.FindPropertyRelative("targetComponent");
                SerializedProperty otherMethodProp = otherEntryProp.FindPropertyRelative("targetMethodName");
                SerializedProperty otherCommandProp = otherEntryProp.FindPropertyRelative("commandName");

                if (otherComponentProp.objectReferenceValue == targetComponentProp.objectReferenceValue &&
                    otherMethodProp.stringValue == targetMethodNameProp.stringValue &&
                    otherCommandProp.stringValue == commandNameProp.stringValue)
                {
                    EditorGUILayout.HelpBox("Command is already selected in another item.", MessageType.Error);
                    targetMethodNameProp.stringValue = null; // Reset method selection
                }
            }
        }

        if (GUILayout.Button("Remove", GUILayout.Width(60)))
        {
            RemoveSendMessageEntry(index);
        }

        EditorGUILayout.EndVertical();
    }

    private void FetchComponentFunctionNames(MonoBehaviour component)
    {
        MethodInfo[] methods = component.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                                 .Where(m => m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(string)
                                          && !m.IsSpecialName && m.DeclaringType == component.GetType()) // Exclude special and inherited members
                                 .ToArray();

        componentFunctionNames = methods.Select(m => m.Name).ToArray();
    }

    private void AddSendMessageEntry()
    {
        sendMessageEntriesProp.arraySize++;
        SerializedProperty newEntryProp = sendMessageEntriesProp.GetArrayElementAtIndex(sendMessageEntriesProp.arraySize - 1);
        newEntryProp.FindPropertyRelative("targetComponent").objectReferenceValue = null;
        newEntryProp.FindPropertyRelative("targetMethodName").stringValue = null;
    }

    private void RemoveSendMessageEntry(int index)
    {
        if (index >= 0 && index < sendMessageEntriesProp.arraySize)
        {
            sendMessageEntriesProp.DeleteArrayElementAtIndex(index);
        }
    }

    private void GenerateRandomPassword()
    {
        YAGMultiplayer mqttClient = (YAGMultiplayer)target;
        mqttClient.GenerateRandomPassword();
    }

    private void ValidateUrl(string url, string urlType)
    {
        if (generateBrockerUrlProp.boolValue) return;
        if (!IsUrlValid(url))
        {
            EditorGUILayout.HelpBox($"{url} is not a valid URL format.", MessageType.Error);
        }
        else
        {
            EditorGUILayout.HelpBox("URL format is valid.", MessageType.Info);
        }
    }

    private void ValidateBackupBrokerUrl(string url)
    {
        if (generateBackupBrockerUrlProp.boolValue) return;
        if (!string.IsNullOrEmpty(url) && !IsUrlValid(url))
        {
            EditorGUILayout.HelpBox("Invalid URL format.", MessageType.Error);
        }
    }

    private void ValidateChannelName(string name)
    {
        if (!string.IsNullOrEmpty(name))
        {
            if (!Regex.IsMatch(name, @"^[a-zA-Z0-9_\-]+$"))
            {
                EditorGUILayout.HelpBox("Invalid channel name.", MessageType.Error);
            }
            else if(name.Contains("-") || name.Contains("_"))
            {
                if (name.StartsWith("-") || name.EndsWith("-") ||
                    name.StartsWith("_") || name.EndsWith("_"))
                {
                    EditorGUILayout.HelpBox("Invalid channel name.", MessageType.Error);
                }

            }
        }
    }

    private void ValidatePassword(string password)
    {
        if (!string.IsNullOrEmpty(password))
        {
            if (!Regex.IsMatch(password, @"^[a-zA-Z0-9_\-]+$"))
            {
                EditorGUILayout.HelpBox("Invalid password format.", MessageType.Error);
            }
            else if (password.Contains("-") || password.Contains("_"))
            {
                if (password.StartsWith("-") || password.EndsWith("-") ||
                    password.StartsWith("_") || password.EndsWith("_"))
                {
                    EditorGUILayout.HelpBox("Invalid password format.", MessageType.Error);
                }

            }
        }
    }

    private void TestConnection()
    {
        YAGMultiplayer mqttClient = (YAGMultiplayer)target;

        bool success = mqttClient.TestConnection();

        if (success)
        {
            testMessage = $"Test successful. Connected to the server. Last test: {DateTime.Now.ToString("HH:mm")}";
        }
        else
        {
            testMessage = $"Test failed. Unable to connect to the server. Last test: {DateTime.Now.ToString("HH:mm")}";
        }

        EditorUtility.DisplayDialog("Connection Test", testMessage, "OK");
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

    private void ValidateSubChannels()
    {
        List<string> invalidEntries = new List<string>();

        for (int i = 0; i < subChannelsProp.arraySize; i++)
        {
            SerializedProperty subChannelProp = subChannelsProp.GetArrayElementAtIndex(i);
            string subChannel = subChannelProp.stringValue;

            if (string.IsNullOrEmpty(subChannel))
            {
                invalidEntries.Add("empty name");
                continue;
            }

            if (!IsValidSubChannel(subChannel))
            {
                invalidEntries.Add(subChannel);
            }
        }

        if (invalidEntries.Count > 0)
        {
            string errorMessage = "Invalid channel names:";
            foreach (var entry in invalidEntries)
            {
                errorMessage += $"\n- {entry}";
            }
            EditorGUILayout.HelpBox(errorMessage, MessageType.Error);
        }
    }

    private bool IsValidSubChannel(string subChannel)
    {
        // Check for valid characters
        if (!Regex.IsMatch(subChannel, @"^[a-zA-Z0-9+#_\-]+$"))
        {
            return false;
        }

        // Check for '-' or '_' at the start or end
        if (subChannel.StartsWith("-") || subChannel.EndsWith("-") ||
            subChannel.StartsWith("_") || subChannel.EndsWith("_"))
        {
            return false;
        }

        // Check for '#' joker character at the end only
        if (subChannel.Contains("#"))
        {
            if (subChannel.Length > 1)
            {
                return false;
            }
            String[] hepsi = new string[subChannelsProp.arraySize];
            for (int i = 0; i < subChannelsProp.arraySize; i++)
            {
                SerializedProperty subChannelProp = subChannelsProp.GetArrayElementAtIndex(i);
                hepsi[i] = subChannelProp.stringValue;
            }

            if (Array.IndexOf(hepsi, subChannel) != subChannelsProp.arraySize -1)
            {
                return false;
            }
        }

        if (subChannel.Contains("+"))
        {
            if (subChannel.Length > 1)
            {
                return false;
            }
        }

        return true;
    }

}
