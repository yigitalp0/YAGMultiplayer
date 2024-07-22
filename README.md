# YAGMultiplayer
YAGMultiplayer is a development tool that helps you add instant data transfer and multiplayer support to games you develop with Unity via the MQTT protocol.
## Editor UI
![image](https://github.com/user-attachments/assets/4c7ddd48-27ae-4dea-bf24-f15d300ee62f)

## Sending Data
```cs
YAGMultiplayer.instance.SendData("data");
YAGMultiplayer.instance.SendCommand("commandName", "parameter");
```
