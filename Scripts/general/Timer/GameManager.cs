using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;

/// <summary>
/// Central game flow: name entry -> countdown -> win/lose -> leaderboard -> restart.
/// The leaderboard lives in a static list, so it survives scene restarts
/// but is lost when the application closes.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public class ScoreEntry
    {
        public string playerName;
        public float timeTaken;
        public bool won;
        public int score; // points left when the game ended (tie-breaker)
    }

    private static readonly List<ScoreEntry> leaderboard = new List<ScoreEntry>();

    private enum State { EnteringName, Playing, Finished }
    private State state = State.EnteringName;

    [Header("Timer")]
    [Tooltip("Total time in seconds before the player loses (600 = 10 minutes).")]
    public float timeLimit = 600f;
    [Tooltip("Always-visible countdown text.")]
    public TMP_Text timerText;
    [Tooltip("If on, the countdown waits until the player enters Room 1 (Room1EntryZone).")]
    public bool startTimerInRoom1 = true;

    [Header("Name Panel (shown at start)")]
    public GameObject namePanel;
    public TMP_InputField nameInput;

    [Header("End Panel (shown on win/lose)")]
    public GameObject endPanel;
    public TMP_Text endMessage;
    public TMP_Text leaderboardText;

    [Header("Finish portal (hidden until the last room is solved)")]
    [Tooltip("The portal outside, with the FinishZone inside it. Appears when the last puzzle is done.")]
    public GameObject finishPortal;

    [Header("Welcome sign (blinks once the game starts)")]
    [Tooltip("The GameObject of the \"Welcome!\" text. It is switched on and off to blink.")]
    public GameObject welcomeText;
    [Tooltip("How long the welcome text stays on and off, in seconds.")]
    public float welcomeBlinkInterval = 0.5f;
    [Tooltip("Steady text shown until the player has read the instructions.")]
    public string instructionsMessage = "See the instructions first!";
    [Tooltip("Blinking text shown after the player leaves the instructions.")]
    public string welcomeMessage = "Welcome!";

    [Header("Player scripts to disable when the game ends")]
    public SimpleFPSController playerController;
    public PlayerPickUpDrop playerPickUp;

    private float timeRemaining;
    private string currentPlayerName = "Player";
    private bool canFinish = false;
    private bool timerPaused = false;
    private bool timerStarted = false;
    private bool instructionsRead = false;

    // Leaderboard colours: green for players who escaped, red for those who did not.
    private const string escapedColor = "#00FF2F";
    private const string failedColor = "#FF1A1A";

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        timeRemaining = timeLimit;
        timerStarted = !startTimerInRoom1;

        if (endPanel != null) endPanel.SetActive(false);
        if (namePanel != null) namePanel.SetActive(true);
        if (welcomeText != null) welcomeText.SetActive(false);
        if (finishPortal != null) finishPortal.SetActive(false);

        SetPlayerEnabled(false);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (nameInput != null)
        {
            nameInput.text = "";
            nameInput.Select();
            nameInput.ActivateInputField();
        }

        UpdateTimerText();
    }

    void Update()
    {
        switch (state)
        {
            case State.EnteringName:
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    StartGame();
                break;

            case State.Playing:
                if (timerStarted && !timerPaused)
                    timeRemaining -= Time.deltaTime;
                UpdateTimerText();

                if (timeRemaining <= 0f)
                {
                    timeRemaining = 0f;
                    UpdateTimerText();
                    EndGame(false, "Time's up!");
                }
                break;

            case State.Finished:
                if (Input.GetKeyDown(KeyCode.Space))
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                break;
        }
    }

    /// <summary>Called by the Start button, or by pressing Enter on the name panel.</summary>
    public void StartGame()
    {
        if (state != State.EnteringName) return;

        if (nameInput != null && !string.IsNullOrWhiteSpace(nameInput.text))
            currentPlayerName = nameInput.text.Trim();

        if (namePanel != null) namePanel.SetActive(false);

        SetPlayerEnabled(true);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        state = State.Playing;

        // First the sign steadily asks the player to read the instructions.
        if (welcomeText != null)
        {
            SetSignText(instructionsMessage);
            welcomeText.SetActive(true);
        }
    }

    /// <summary>Called by the instruction ViewLockZone when the player leaves it with Q.
    /// From now on the sign blinks "Welcome!".</summary>
    public void InstructionsRead()
    {
        if (instructionsRead || welcomeText == null) return;
        instructionsRead = true;

        SetSignText(welcomeMessage);
        if (state == State.Playing)
            StartCoroutine(BlinkWelcome());
    }

    void SetSignText(string message)
    {
        TMP_Text tmp = welcomeText.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null) { tmp.text = message; return; }

        UnityEngine.UI.Text legacy = welcomeText.GetComponentInChildren<UnityEngine.UI.Text>(true);
        if (legacy != null) legacy.text = message;
    }

    IEnumerator BlinkWelcome()
    {
        while (state == State.Playing)
        {
            welcomeText.SetActive(true);
            yield return new WaitForSeconds(welcomeBlinkInterval);

            welcomeText.SetActive(false);
            yield return new WaitForSeconds(welcomeBlinkInterval);
        }

        welcomeText.SetActive(false);
    }

    /// <summary>Called by Room1EntryZone when the player walks into Room 1.</summary>
    public void StartTimer()
    {
        timerStarted = true;
        UpdateTimerText();
    }

    /// <summary>Stops or restarts the countdown, e.g. while a demo is playing.</summary>
    public void SetTimerPaused(bool paused)
    {
        timerPaused = paused;
    }

    /// <summary>Called by Room5ListManager once the last puzzle is solved.</summary>
    public void AllowFinish()
    {
        canFinish = true;

        // Only now does the way out exist for the player to see.
        if (finishPortal != null) finishPortal.SetActive(true);
    }

    /// <summary>Called by FinishZone when the player reaches the ground after the last room.</summary>
    public void Win()
    {
        // Ignore the finish zone until the last room has actually been solved,
        // otherwise the player could win by walking over it at the start.
        if (!canFinish) return;

        EndGame(true, "Well done!");
    }

    /// <summary>Called by ScoreManager when the score reaches zero.</summary>
    public void LoseByScore()
    {
        EndGame(false, "You ran out of points!");
    }

    void EndGame(bool won, string message)
    {
        if (state == State.Finished) return;
        state = State.Finished;

        SetPlayerEnabled(false);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        float timeTaken = timeLimit - timeRemaining;

        leaderboard.Add(new ScoreEntry
        {
            playerName = currentPlayerName,
            timeTaken = timeTaken,
            won = won,
            score = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0
        });

        if (endMessage != null)
        {
            endMessage.text = won
                ? message + "\nYou escaped in " + FormatTime(timeTaken)
                : message + "\nYou did not escape.";
        }

        if (leaderboardText != null)
            leaderboardText.text = BuildLeaderboard();

        if (endPanel != null) endPanel.SetActive(true);
    }

    string BuildLeaderboard()
    {
        // Winners first: fastest time, then more points left on an equal time.
        var winners = leaderboard
            .Where(e => e.won)
            .OrderBy(e => e.timeTaken)
            .ThenByDescending(e => e.score);

        // Then everyone who did not finish, in the order they played. They cannot be
        // compared fairly, so they are not ranked and get no position number.
        var losers = leaderboard.Where(e => !e.won);

        var ordered = winners.Concat(losers).ToList();

        string text = "LEADERBOARD\n\n";
        for (int i = 0; i < ordered.Count; i++)
        {
            ScoreEntry e = ordered[i];
            string color = e.won ? escapedColor : failedColor;
            string position = e.won ? (i + 1) + ". " : "- ";
            string result = e.won ? FormatTime(e.timeTaken) + " (" + e.score + " pts)" : "did not escape";
            text += "<color=" + color + ">" + position + e.playerName + " — " + result + "</color>\n";
        }

        text += "\nPress SPACE for the next player";
        return text;
    }

    void SetPlayerEnabled(bool enabled)
    {
        if (playerController != null) playerController.enabled = enabled;
        if (playerPickUp != null) playerPickUp.enabled = enabled;
    }

    void UpdateTimerText()
    {
        if (timerText != null)
            timerText.text = timerStarted ? FormatTime(timeRemaining) : "--:--";
    }

    string FormatTime(float seconds)
    {
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        return string.Format("{0:00}:{1:00}", m, s);
    }
}
