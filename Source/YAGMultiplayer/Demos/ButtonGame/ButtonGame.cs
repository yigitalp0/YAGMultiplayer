using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonGame : MonoBehaviour
{
    public InputField playerName;
    public Text lastClick;

    public void OnClick()
    {
        YAGMultiplayer.instance.SendCommand("buttonclick", playerName.text);
    }

    public void OtherPlayerClick(string parameter)
    {
        lastClick.text = "Last clicked by: " + parameter;
    }
}
