using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Room5ListManager : MonoBehaviour
{
    public static Room5ListManager Instance;

    [Header("Zones (one per list node)")]
    public Room5AddressZone[] zones;

    [Header("Door")]
    public DoorController door;

    [Header("Feedback Screen (Correct! / The door is open! message)")]
    public Text feedbackScreen;
    public float correctMessageDuration = 1.5f;
    [Tooltip("How long \"Get Out!\" stays on and off when blinking, in seconds.")]
    public float blinkInterval = 1f;

    [Header("Second Screen (instruction for the player)")]
    public Text secondScreen;
    [Tooltip("Shown while the puzzle is not solved yet.")]
    public string beforeSolvedText = "Sit on chair!";
    [Tooltip("Blinks once the puzzle is solved.")]
    public string afterSolvedText = "Touch the ground!";

    private bool solved = false;
    private Coroutine feedbackCoroutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (secondScreen != null)
            secondScreen.text = beforeSolvedText;
    }

    public void OnZoneChanged()
    {
        if (solved) return;
        CheckWin();
    }

    public void ShowCorrectFeedback()
    {
        if (solved || feedbackScreen == null) return;

        if (feedbackCoroutine != null)
            StopCoroutine(feedbackCoroutine);

        feedbackCoroutine = StartCoroutine(FlashMessage("Correct!"));
    }

    public void ShowWrongFeedback()
    {
        if (solved || feedbackScreen == null) return;

        if (feedbackCoroutine != null)
            StopCoroutine(feedbackCoroutine);

        feedbackCoroutine = StartCoroutine(FlashMessage("Wrong!"));
    }

    IEnumerator FlashMessage(string message)
    {
        feedbackScreen.text = message;
        yield return new WaitForSeconds(correctMessageDuration);

        if (!solved)
            feedbackScreen.text = "";
    }

    void CheckWin()
    {
        foreach (Room5AddressZone zone in zones)
        {
            if (zone == null || !zone.HasCube || !zone.IsCorrect)
                return;
        }

        solved = true;
        if (door != null) door.OpenDoor();

        // Unlock the finish zone: only now can the player actually escape.
        if (GameManager.Instance != null) GameManager.Instance.AllowFinish();

        if (feedbackScreen != null)
        {
            if (feedbackCoroutine != null) StopCoroutine(feedbackCoroutine);
            StartCoroutine(BlinkGetOut());
        }

        if (secondScreen != null)
            StartCoroutine(BlinkSecondScreen());

        Debug.Log("Room5: The list is correct! The door opens.");
    }

    IEnumerator BlinkGetOut()
    {
        while (true)
        {
            feedbackScreen.text = "Get Out!";
            yield return new WaitForSeconds(blinkInterval);

            feedbackScreen.text = "";
            yield return new WaitForSeconds(blinkInterval);
        }
    }

    IEnumerator BlinkSecondScreen()
    {
        while (true)
        {
            secondScreen.text = afterSolvedText;
            yield return new WaitForSeconds(blinkInterval);

            secondScreen.text = "";
            yield return new WaitForSeconds(blinkInterval);
        }
    }
}
