using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class StackManager : MonoBehaviour
{
    public static StackManager Instance;

    [Header("Zones")]
    [SerializeField] public StackZone pushZone;
    [SerializeField] public StackZone stackZone;
    [SerializeField] public StackZone popZone;

    [Header("Screen")]
    [SerializeField] public TMP_Text codeText;

    [Header("Result Screen (Screen7)")]
    [SerializeField] public TMP_Text resultText;

    [Header("Correct Order (bottom to top)")]
    [SerializeField] public string[] correctOrder = { "Yellow", "Green", "Red", "Blue", "White" };

    [Header("Door")]
    [SerializeField] public DoorController door;

    private List<string> operations = new List<string>();
    private List<GameObject> previousStackPlates = new List<GameObject>();

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void OnZoneChanged(StackZone changedZone)
    {
        if (changedZone == stackZone)
        {
            List<GameObject> current = stackZone.GetPlates();
            current.Sort((a, b) => a.transform.position.y.CompareTo(b.transform.position.y));

            if (current.Count > previousStackPlates.Count)
            {
                // Μπήκε πιάτο
                for (int i = 0; i < current.Count; i++)
                {
                    GameObject plate = current[i];
                    if (!previousStackPlates.Contains(plate))
                    {
                        PlateColor pc = plate.GetComponent<PlateColor>();
                        if (pc != null)
                            operations.Add($"push({pc.colorName})");

                        bool correct = pc != null && i < correctOrder.Length && pc.colorName == correctOrder[i];
                        if (!correct && ScoreManager.Instance != null)
                            ScoreManager.Instance.RemovePoints(1);
                    }
                }
            }
            else if (current.Count < previousStackPlates.Count)
            {
                // Βγήκε πιάτο
                operations.Add("pop()");
            }

            previousStackPlates = new List<GameObject>(current);
        }

        UpdateCodeDisplay();
        CheckWin();
    }

    void UpdateCodeDisplay()
    {
        int maxLines = 4;
        int start = Mathf.Max(0, operations.Count - maxLines);
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = start; i < operations.Count; i++)
            sb.AppendLine(operations[i]);
        codeText.text = sb.ToString();
    }

    void CheckWin()
    {
        List<GameObject> stackPlates = stackZone.GetPlates();
        if (stackPlates.Count != correctOrder.Length)
        {
            if (resultText != null) resultText.text = "";
            return;
        }

        stackPlates.Sort((a, b) => a.transform.position.y.CompareTo(b.transform.position.y));

        for (int i = 0; i < correctOrder.Length; i++)
        {
            PlateColor pc = stackPlates[i].GetComponent<PlateColor>();
            if (pc == null || pc.colorName != correctOrder[i])
            {
                if (resultText != null) resultText.text = "";
                return;
            }
        }

        Debug.Log("Σωστό! Η στοίβα είναι στη σωστή σειρά!");
        if (door != null) door.OpenDoor();
        if (resultText != null) resultText.text = "Η πόρτα άνοιξε!";
    }
}
