using TMPro;
using UnityEngine;

public class BinaryDisplay : MonoBehaviour
{
    public TextMeshProUGUI displayText;
    public SwitchToggle[] switches; // the 4 switches of the room

    public void UpdateDisplay()
    {
        string binary = "";
        foreach (SwitchToggle s in switches)
            binary += s.isOn ? "1" : "0";
        displayText.text = binary;
    }

    private void Start()
    {
        UpdateDisplay();
    }
}