using UnityEngine;
using TMPro;

public class EnterButton : MonoBehaviour
{
    public static EnterButton Instance;

    [System.Serializable]
    public class Problem
    {
        [TextArea(4, 12)] public string questionText;
        public int[] answerValues = new int[4]; // values for A, B, C, D
        public int correctIndex; // 0=A, 1=B, 2=C, 3=D
    }

    [Header("Switches (bit3 → bit0)")]
    public SwitchToggle[] switches;

    [Header("Problems (increasing difficulty)")]
    public Problem[] problems = new Problem[]
    {
        new Problem
        {
            questionText = "Problem 1 — Integer division and remainder\n\n" +
                            "int a = 17, b = 5;\n" +
                            "int x = a / b;\n" +
                            "int y = a % b;\n" +
                            "int result = x + y;\n" +
                            "// What is the value of result?",
            answerValues = new int[] { 5, 6, 3, 2 },
            correctIndex = 0
        },
        new Problem
        {
            questionText = "Problem 2 — Precedence of % and *\n\n" +
                            "int a = 13, b = 4, c = 2;\n" +
                            "int result = a % b * c + 1;\n" +
                            "// What is the value of result?",
            answerValues = new int[] { 4, 6, 3, 5 },
            correctIndex = 2
        },
        new Problem
        {
            questionText = "Problem 3 — Overflow in unsigned char\n\n" +
                            "unsigned char x = 250;\n" +
                            "x = x + 10;\n" +
                            "int result = x % 7;\n" +
                            "// What is the value of result?",
            answerValues = new int[] { 1, 4, 0, 5 },
            correctIndex = 1
        }
    };

    [Header("Screens")]
    public TextMeshProUGUI questionScreen;   // Screen2(1) (question + code)
    public TextMeshProUGUI[] answerScreens;  // Screen3, Screen4, Screen5, Screen6 (A, B, C, D)
    public TextMeshProUGUI titleScreen;      // Screen7 (round title / door message)

    [Header("Door Room2 → Room3")]
    public DoorController door;

    private int currentProblem = 0;
    private static readonly string[] letters = { "A", "B", "C", "D" };
    private static readonly string[] roundLabels = { "First question", "Second question", "Third question" };

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        ShowProblem();
        ShowAnswerPlaceholders();   // hide round 1's real answers until the hand-demo hands over
        if (titleScreen != null) titleScreen.text = "";   // Screen7 lights up only once the demo hands over
    }

    /// <summary>Shows "A: ----" etc. instead of the real values, e.g. before the first hand-demo has played.</summary>
    void ShowAnswerPlaceholders()
    {
        for (int i = 0; i < answerScreens.Length; i++)
            if (answerScreens[i] != null)
                answerScreens[i].text = letters[i] + ": ----";
    }

    /// <summary>Re-shows the current problem and answers, e.g. after a hand-demo has messed with the screens.</summary>
    public void RefreshScreens()
    {
        ShowProblem();
    }

    /// <summary>Turns every switch off and updates the binary display, e.g. after a hand-demo pressed some.</summary>
    public void ResetAllSwitches()
    {
        ResetSwitches();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Camera cam = AutoSitView.CurrentCamera ?? Camera.main;
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {
                EnterButton clicked = hit.collider.GetComponentInParent<EnterButton>();
                if (clicked != null)
                {
                    if (clicked == this)
                        OnEnterPressed();
                    break;
                }
            }
        }
    }

    void ShowProblem()
    {
        Problem p = problems[currentProblem];

        if (questionScreen != null)
            questionScreen.text = p.questionText;

        if (titleScreen != null && currentProblem < roundLabels.Length)
            titleScreen.text = roundLabels[currentProblem];

        for (int i = 0; i < answerScreens.Length && i < p.answerValues.Length; i++)
            answerScreens[i].text = letters[i] + ": " + ToBinary(p.answerValues[i]);
    }

    void OnEnterPressed()
    {
        Problem p = problems[currentProblem];
        int playerAnswer = GetCurrentValue();
        int correct = p.answerValues[p.correctIndex];

        if (playerAnswer == correct)
        {
            currentProblem++;
            ResetSwitches();

            if (currentProblem < problems.Length)
            {
                ShowProblem();
                Debug.Log("Correct! Next problem: " + (currentProblem + 1));
            }
            else
            {
                door.OpenDoor();

                if (questionScreen != null)
                    questionScreen.text = "";

                if (titleScreen != null)
                    titleScreen.text = "The door is open!";

                Debug.Log("All correct! The door opens!");
            }
        }
        else
        {
            ScoreManager.Instance.RemovePoints(1);
            Debug.Log("Wrong! -1 point. Correct answer: " + ToBinary(correct));
        }
    }

    void ResetSwitches()
    {
        foreach (var sw in switches)
        {
            sw.isOn = false;
            sw.binaryDisplay.UpdateDisplay();
        }
    }

    int GetCurrentValue()
    {
        int value = 0;
        for (int i = 0; i < switches.Length; i++)
            if (switches[i].isOn)
                value += (int)Mathf.Pow(2, switches.Length - 1 - i);
        return value;
    }

    string ToBinary(int n) => System.Convert.ToString(n, 2).PadLeft(4, '0');
}
