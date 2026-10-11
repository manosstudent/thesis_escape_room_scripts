using UnityEngine;
using System.Collections;
using TMPro;

public class Room1QuizManager : MonoBehaviour
{
    public static Room1QuizManager Instance;

    [System.Serializable]
    public class Round
    {
        [TextArea(3, 8)] public string questionText;
        [Tooltip("The index (0-3) of the object that is the correct answer in this round.")]
        public int correctObjectIndex;
        [Tooltip("The 4 labels, in the same order as the objects in the Objects array.")]
        public string[] labels = new string[4];
    }

    [Header("The 4 objects (order defines labels/correctObjectIndex)")]
    public GameObject[] objects = new GameObject[4];

    [Header("Screens")]
    [Tooltip("The question screen on the pipe.")]
    public TMP_Text questionScreen;
    [Tooltip("The 4 labels, one next to each object (same order as Objects).")]
    public TMP_Text[] objectLabels = new TMP_Text[4];

    [Header("Rounds")]
    public Round[] rounds = new Round[]
    {
        new Round
        {
            questionText = "Which variable declaration is syntactically correct in C?",
            correctObjectIndex = 0,
            labels = new string[]
            {
                "int x = 5;",
                "int x = 5",
                "int 5x = 5;",
                "integer x = 5;"
            }
        },
        new Round
        {
            questionText = "Which declaration correctly stores the value 19.99?",
            correctObjectIndex = 2,
            labels = new string[]
            {
                "int price = 19.99;",
                "float price = \"19.99\";",
                "float price = 19.99;",
                "char price = 19.99;"
            }
        },
        new Round
        {
            questionText = "Which declaration can correctly hold the number 3000000000?",
            correctObjectIndex = 3,
            labels = new string[]
            {
                "int bigNumber = 3000000000;",
                "short bigNumber = 3000000000;",
                "char bigNumber = 3000000000;",
                "long long bigNumber = 3000000000;"
            }
        }
    };

    [Header("Door (same settings as the old DropZone)")]
    public Transform door;
    public float openAngle = 90f;
    public float openSpeed = 3f;

    private int currentRound = 0;
    private bool solved = false;
    private Vector3[] startPositions;
    private Quaternion[] startRotations;
    private Quaternion closedRot;
    private Quaternion openedRot;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        startPositions = new Vector3[objects.Length];
        startRotations = new Quaternion[objects.Length];

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] == null) continue;
            startPositions[i] = objects[i].transform.position;
            startRotations[i] = objects[i].transform.rotation;
        }

        if (door != null)
        {
            closedRot = door.rotation;
            openedRot = Quaternion.Euler(
                door.rotation.eulerAngles.x,
                door.rotation.eulerAngles.y + openAngle,
                door.rotation.eulerAngles.z
            );
        }

        ShowRound();
    }

    /// <summary>Re-shows the current round's question and labels, e.g. after a "Sit on Chair!" prompt is dismissed.</summary>
    public void RefreshQuestionScreen()
    {
        ShowRound();
    }

    void ShowRound()
    {
        if (currentRound >= rounds.Length) return;
        Round r = rounds[currentRound];

        if (questionScreen != null)
            questionScreen.text = r.questionText;

        for (int i = 0; i < objectLabels.Length && i < r.labels.Length; i++)
            if (objectLabels[i] != null)
                objectLabels[i].text = r.labels[i];
    }

    /// <summary>
    /// Called by DropZone when an object enters the pipe.
    /// </summary>
    public void OnObjectDropped(GameObject dropped)
    {
        if (solved) return;

        int index = System.Array.IndexOf(objects, dropped);
        if (index < 0) return;

        if (index == rounds[currentRound].correctObjectIndex)
        {
            currentRound++;

            if (currentRound < rounds.Length)
            {
                RespawnAll();
                ShowRound();
                Debug.Log("Room1: Correct! Next question: " + (currentRound + 1));
            }
            else
            {
                solved = true;
                if (door != null) StartCoroutine(OpenDoor());
                if (questionScreen != null) questionScreen.text = "The door is open!";
                Debug.Log("Room1: All correct! The door opens.");
            }
        }
        else
        {
            if (ScoreManager.Instance != null)
                ScoreManager.Instance.RemovePoints(1);
            Debug.Log("Room1: Wrong! -1 point.");
        }
    }

    IEnumerator OpenDoor()
    {
        float elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * openSpeed;
            door.rotation = Quaternion.Slerp(closedRot, openedRot, elapsed);
            yield return null;
        }
    }

    /// <summary>
    /// Resets all 4 objects to their starting position, visible and active.
    /// </summary>
    void RespawnAll()
    {
        for (int i = 0; i < objects.Length; i++)
        {
            GameObject obj = objects[i];
            if (obj == null) continue;

            obj.SetActive(true);
            obj.transform.position = startPositions[i];
            obj.transform.rotation = startRotations[i];

            Collider col = obj.GetComponent<Collider>();
            if (col != null) col.enabled = true;

            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }
}
