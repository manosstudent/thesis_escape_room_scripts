using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Room4 sink task. The dishes start spread inside the purple sink. The stack itself is the
/// middle point between the two sinks:
///   push(Color) = take that dish from the purple sink and put it on top of the middle stack.
///   pop()       = the player drags the TOP dish of the middle stack into the blue sink, where it vanishes.
/// When the player steps on the portal, a demo plays the demo code automatically. Then the dishes
/// go back to the purple sink and the player must run the task code themselves, one step at a time.
/// </summary>
public class SinkTaskManager : MonoBehaviour
{
    public enum Op { Push, Pop }

    [System.Serializable]
    public class Step
    {
        public Op op;
        [Tooltip("Only used for Push. Must match a dish's colour name.")]
        public string color;
    }

    [Header("Sinks")]
    [Tooltip("Where all dishes start. Push picks a dish from here; dropping a dish back here is never a valid move.")]
    public SinkZone washSink;
    [Tooltip("Where popped dishes are dragged to. The dish vanishes once it lands here.")]
    public SinkZone drySink;

    [Header("Middle stack")]
    [Tooltip("The zone between the two sinks where pushed dishes stack up. Pop takes the top dish from here.")]
    public SinkZone stackZone;

    [Header("Portal")]
    public SinkSeat seat;

    [Header("Screens (drag the text object itself; TextMeshPro or legacy Text both work)")]
    public GameObject codeScreen;
    public GameObject statusScreen;
    [Tooltip("How long Correct! / Wrong move! stays on the status screen before it clears, in seconds.")]
    public float feedbackDuration = 1f;
    [Tooltip("Always shown under the status message. Leave empty to hide it. Supports <color=#RRGGBB> tags.")]
    [TextArea(2, 4)]
    public string sinkHint = "<color=#FF66FF>Purple</color> sink → middle: push()\nMiddle → <color=#00E5FF>blue</color> sink: pop()";

    [Header("Watch prompt (shown before the demo starts)")]
    [Tooltip("Blinks for watchPromptDuration seconds before the countdown, so the player has time to read it.")]
    public string watchPrompt = "Watch carefully first!";
    [Tooltip("How long the watch prompt blinks before the countdown, in seconds. Does not count against the timer.")]
    public float watchPromptDuration = 3f;
    [Tooltip("How long the watch prompt stays on and off while blinking, in seconds.")]
    public float watchPromptBlinkInterval = 0.5f;
    [Tooltip("How long each \"Demo starts in N\" number is shown, in seconds.")]
    public float countdownStepDuration = 0.7f;

    [Header("Start prompt (blinks until the player steps on the portal)")]
    public string startPrompt = "Stand on the portal to start.";
    [Tooltip("Size of the start prompt, in percent of the screen's normal font size.")]
    public int startPromptSizePercent = 150;
    public float startBlinkInterval = 0.6f;

    [Header("Finish message")]
    public string solvedMessage = "Well done! The door is open.";

    [Header("Demo (plays automatically when the player steps on the portal)")]
    public Step[] demoSteps =
    {
        new Step { op = Op.Push, color = "Red" },
        new Step { op = Op.Push, color = "Blue" },
        new Step { op = Op.Pop },
        new Step { op = Op.Push, color = "Green" },
        new Step { op = Op.Pop },
        new Step { op = Op.Pop },
    };
    [Tooltip("Pause between two demo steps, in seconds.")]
    public float demoStepDelay = 1.2f;

    [Header("Player task (each colour can be pushed only once)")]
    public Step[] taskSteps =
    {
        new Step { op = Op.Push, color = "Yellow" },
        new Step { op = Op.Push, color = "White" },
        new Step { op = Op.Pop },
        new Step { op = Op.Push, color = "Red" },
        new Step { op = Op.Push, color = "Blue" },
        new Step { op = Op.Pop },
        new Step { op = Op.Pop },
        new Step { op = Op.Push, color = "Green" },
        new Step { op = Op.Pop },
        new Step { op = Op.Pop },
    };

    [Header("Animation")]
    [Tooltip("Duration of a dish move in the demo and when resetting, in seconds.")]
    public float moveDuration = 0.8f;
    [Tooltip("Duration of the snap after the player drops a dish, in seconds.")]
    public float snapDuration = 0.25f;
    [Tooltip("How high dishes arc during demo moves.")]
    public float arcHeight = 0.2f;

    [Header("Door")]
    public DoorController door;
    [Tooltip("Optional. Shows the idle text below until the task is solved, then blinks the door message.")]
    public GameObject doorScreen;
    [Tooltip("Shown on the door screen while the task is not solved yet.")]
    public string doorIdleText = "Code:";
    public string doorOpenText = "Door is unlocked!";
    [Tooltip("How long the door message stays on and off, in seconds.")]
    public float doorBlinkInterval = 0.5f;

    // Code screen colours: finished lines, the current line. Pending lines use the screen's own colour.
    private const string doneColor = "#7A7A7A";
    private const string currentColor = "#FFD54A";

    private enum Phase { Idle, Demo, PlayerTurn, Solved }
    private Phase phase = Phase.Idle;
    private bool busy;              // a dish is animating: no dragging meanwhile
    private int stepIndex;
    private Coroutine feedbackRoutine;
    private Coroutine startPromptRoutine;
    private bool showHint;          // the sink hint only makes sense once the player takes over
    private string restingStatus = "";   // what the status screen goes back to after a short message

    private SinkDish[] dishes;
    private readonly List<SinkDish> middleStack = new List<SinkDish>();

    // Where the dragged dish was before it was picked up, to put it back if the move is refused.
    private Vector3 grabbedFromPosition;
    private Quaternion grabbedFromRotation;

    void Awake()
    {
        dishes = FindObjectsOfType<SinkDish>();
    }

    void Start()
    {
        ValidateSteps(demoSteps, "Demo");
        ValidateSteps(taskSteps, "Task");

        SetText(codeScreen, "");
        SetText(doorScreen, doorIdleText);
        startPromptRoutine = StartCoroutine(BlinkStartPrompt());
    }

    IEnumerator BlinkStartPrompt()
    {
        string prompt = "<size=" + startPromptSizePercent + "%><color=" + currentColor + ">"
                        + startPrompt + "</color></size>";
        while (true)
        {
            SetStatus(prompt);
            yield return new WaitForSeconds(startBlinkInterval);
            SetStatus("");
            yield return new WaitForSeconds(startBlinkInterval);
        }
    }

    // ---------- Used by SinkSeat and SinkDrag ----------

    /// <summary>The camera to drag with, or null when the player may not drag right now.</summary>
    public Camera DragCamera =>
        phase == Phase.PlayerTurn && !busy && seat != null && seat.IsSeated ? seat.seatCamera : null;

    public float SinksTopY => Mathf.Max(washSink.TopY, drySink.TopY, stackZone.TopY);

    public void OnSeated()
    {
        if (phase == Phase.Idle)
            StartCoroutine(RunDemo());
    }

    public void OnStood()
    {
        // Progress is kept; the player can step back on the portal and continue.
    }

    public void OnDishGrabbed(SinkDish dish)
    {
        grabbedFromPosition = dish.transform.position;
        grabbedFromRotation = dish.transform.rotation;
    }

    public void CancelDrag(SinkDish dish)
    {
        StartCoroutine(PutBack(dish, false));
    }

    public void OnDishReleased(SinkDish dish)
    {
        Vector3 p = dish.transform.position;
        bool inStack = stackZone.ContainsXZ(p);
        bool inDry = drySink.ContainsXZ(p);
        SinkZone droppedIn = inStack ? stackZone
                           : inDry ? drySink
                           : null;

        Debug.Log("[SINK-TEMP] Released '" + dish.name + "' at " + p
                  + " | inStack=" + inStack + " inDry=" + inDry
                  + " | zoneOf=" + ZoneOf(dish) + " stepIndex=" + stepIndex
                  + " (step=" + (stepIndex < taskSteps.Length ? taskSteps[stepIndex].op + " " + taskSteps[stepIndex].color : "none") + ")");   // TEMP

        // Dropped outside the two drop zones, or back where it came from: just put it back, no penalty.
        if (droppedIn == null || droppedIn == ZoneOf(dish) || stepIndex >= taskSteps.Length)
        {
            StartCoroutine(PutBack(dish, false));
            return;
        }

        Step step = taskSteps[stepIndex];

        bool correctPush = droppedIn == stackZone && step.op == Op.Push
                           && ZoneOf(dish) == null && SameColor(dish, step.color);

        bool correctPop = droppedIn == drySink && step.op == Op.Pop
                          && middleStack.Count > 0 && middleStack[middleStack.Count - 1] == dish;

        Debug.Log("[SINK-TEMP] correctPush=" + correctPush + " correctPop=" + correctPop);   // TEMP

        if (correctPush || correctPop)
            StartCoroutine(PlayerMove(step, dish));
        else
            StartCoroutine(PutBack(dish, true));
    }

    // ---------- Flow ----------

    IEnumerator RunDemo()
    {
        phase = Phase.Demo;
        busy = true;

        if (startPromptRoutine != null) StopCoroutine(startPromptRoutine);

        // The player is only watching, so the demo does not cost them any time.
        if (GameManager.Instance != null) GameManager.Instance.SetTimerPaused(true);

        ShowSteps(demoSteps, -1);
        yield return StartCoroutine(BlinkWatchPrompt());
        yield return StartCoroutine(CountdownToDemo());

        SetStatus("<color=" + currentColor + ">Watch Demo carefully!</color>");

        for (int i = 0; i < demoSteps.Length; i++)
        {
            yield return StartCoroutine(WaitWhileAway());

            ShowSteps(demoSteps, i);
            SinkDish dish = DishForStep(demoSteps[i]);
            if (dish != null)
                yield return StartCoroutine(ExecuteStep(demoSteps[i], dish, moveDuration, arcHeight));
            yield return new WaitForSeconds(demoStepDelay);
        }
        ShowSteps(demoSteps, demoSteps.Length);

        yield return StartCoroutine(WaitWhileAway());

        SetStatus("<color=" + currentColor + ">Now it's your turn!</color>");
        yield return new WaitForSeconds(demoStepDelay);
        yield return StartCoroutine(ResetDishes());

        stepIndex = 0;
        phase = Phase.PlayerTurn;
        ShowSteps(taskSteps, 0);
        showHint = true;
        SetStatus("Run this code with the dishes.");
        busy = false;

        if (GameManager.Instance != null) GameManager.Instance.SetTimerPaused(false);
    }

    IEnumerator BlinkWatchPrompt()
    {
        string prompt = "<color=" + currentColor + ">" + watchPrompt + "</color>";
        float elapsed = 0f;
        while (elapsed < watchPromptDuration)
        {
            SetStatus(prompt);
            yield return new WaitForSeconds(watchPromptBlinkInterval);
            SetStatus("");
            yield return new WaitForSeconds(watchPromptBlinkInterval);
            elapsed += watchPromptBlinkInterval * 2f;
        }
    }

    IEnumerator CountdownToDemo()
    {
        for (int n = 3; n >= 1; n--)
        {
            SetStatus("<color=" + currentColor + ">Demo starts in " + n + "</color>");
            yield return new WaitForSeconds(countdownStepDuration);
        }
    }

    // Holds the demo where it is while the player walks away, and lets the clock run meanwhile.
    IEnumerator WaitWhileAway()
    {
        if (seat == null || seat.IsSeated) yield break;

        if (GameManager.Instance != null) GameManager.Instance.SetTimerPaused(false);

        while (!seat.IsSeated)
            yield return null;

        if (GameManager.Instance != null) GameManager.Instance.SetTimerPaused(true);
    }

    IEnumerator PlayerMove(Step step, SinkDish dish)
    {
        busy = true;
        yield return StartCoroutine(ExecuteStep(step, dish, snapDuration, 0f));

        stepIndex++;
        ShowSteps(taskSteps, stepIndex);

        if (stepIndex >= taskSteps.Length)
        {
            phase = Phase.Solved;
            // Stop a pending clear, or it would wipe the final message a moment later.
            if (feedbackRoutine != null) StopCoroutine(feedbackRoutine);
            showHint = false;   // the task is over, the hint is no longer useful
            SetStatus(solvedMessage);
            if (door != null) door.OpenDoor();
            if (doorScreen != null) StartCoroutine(BlinkDoorScreen());
        }
        else
        {
            FlashStatus("Correct!");
        }
        busy = false;
    }

    // Shows a short message on the status screen and clears it after feedbackDuration.
    void FlashStatus(string text)
    {
        if (feedbackRoutine != null) StopCoroutine(feedbackRoutine);
        feedbackRoutine = StartCoroutine(FlashStatusRoutine(text));
    }

    IEnumerator FlashStatusRoutine(string text)
    {
        // Shown on its own, so the short message is not mixed with the hint.
        WriteStatus(text, false);
        yield return new WaitForSeconds(feedbackDuration);
        WriteStatus(restingStatus, showHint);
        feedbackRoutine = null;
    }

    IEnumerator BlinkDoorScreen()
    {
        string message = "<color=" + currentColor + ">" + doorOpenText + "</color>";
        while (true)
        {
            SetText(doorScreen, message);
            yield return new WaitForSeconds(doorBlinkInterval);
            SetText(doorScreen, "");
            yield return new WaitForSeconds(doorBlinkInterval);
        }
    }

    IEnumerator PutBack(SinkDish dish, bool wrongMove)
    {
        busy = true;
        if (wrongMove)
        {
            if (ScoreManager.Instance != null) ScoreManager.Instance.RemovePoints(1);
            FlashStatus("Wrong move! -1 point");
        }
        yield return StartCoroutine(Move(dish, grabbedFromPosition, grabbedFromRotation, snapDuration, 0f));
        busy = false;
    }

    // ---------- Stack operations ----------

    IEnumerator ExecuteStep(Step step, SinkDish dish, float duration, float arc)
    {
        if (step.op == Op.Push)
        {
            middleStack.Add(dish);
            Vector3 target = stackZone.SlotPosition(middleStack.Count - 1);
            yield return StartCoroutine(Move(dish, target, dish.HomeRotation, duration, arc));
        }
        else
        {
            middleStack.Remove(dish);
            Vector3 target = drySink.SlotPosition(0);
            yield return StartCoroutine(Move(dish, target, dish.HomeRotation, duration, arc));
            dish.gameObject.SetActive(false);   // popped: the dish vanishes in the blue sink
        }
    }

    IEnumerator ResetDishes()
    {
        middleStack.Clear();
        foreach (SinkDish dish in dishes)
        {
            dish.gameObject.SetActive(true);   // bring back any dish that vanished in the blue sink
            StartCoroutine(Move(dish, dish.HomePosition, dish.HomeRotation, moveDuration, arcHeight));
        }
        yield return new WaitForSeconds(moveDuration);
        yield return null;   // let the last move finish its final frame before dragging is allowed
    }

    IEnumerator Move(SinkDish dish, Vector3 to, Quaternion toRotation, float duration, float arc)
    {
        Vector3 from = dish.transform.position;
        Quaternion fromRotation = dish.transform.rotation;
        duration = Mathf.Max(duration, 0.01f);

        for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
        {
            Vector3 p = Vector3.Lerp(from, to, t);
            p.y += arc * 4f * t * (1f - t);   // simple arc: 0 at both ends, highest in the middle
            dish.transform.SetPositionAndRotation(p, Quaternion.Slerp(fromRotation, toRotation, t));
            yield return null;
        }
        dish.transform.SetPositionAndRotation(to, toRotation);
    }

    // ---------- Helpers ----------

    SinkDish DishForStep(Step step)
    {
        if (step.op == Op.Pop)
            return middleStack.Count > 0 ? middleStack[middleStack.Count - 1] : null;
        return FindDish(step.color);
    }

    /// <summary>The zone a dish is in, or null if it is still in the purple sink.</summary>
    SinkZone ZoneOf(SinkDish dish)
    {
        if (middleStack.Contains(dish)) return stackZone;
        return null;
    }

    SinkDish FindDish(string color)
    {
        foreach (SinkDish dish in dishes)
            if (SameColor(dish, color))
                return dish;
        return null;
    }

    static bool SameColor(SinkDish dish, string color)
    {
        return string.Equals((dish.colorName ?? "").Trim(), (color ?? "").Trim(),
                             System.StringComparison.OrdinalIgnoreCase);
    }

    void ShowSteps(Step[] steps, int current)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < steps.Length; i++)
        {
            string line = steps[i].op == Op.Push ? "push(" + steps[i].color + ");" : "pop();";

            if (i < current)       sb.Append("<color=" + doneColor + ">  " + line + "</color>");
            else if (i == current) sb.Append("<color=" + currentColor + ">> " + line + "</color>");
            else                   sb.Append("  " + line);

            sb.Append('\n');
        }
        SetText(codeScreen, sb.ToString());
    }

    // The lasting message of the status screen: shown with the hint, and returned to after a flash.
    void SetStatus(string message)
    {
        restingStatus = message ?? "";
        WriteStatus(restingStatus, showHint);
    }

    void WriteStatus(string message, bool withHint)
    {
        string text = message ?? "";
        if (withHint && !string.IsNullOrEmpty(sinkHint))
            text = string.IsNullOrEmpty(text) ? sinkHint : text + "\n\n" + sinkHint;
        SetText(statusScreen, text);
    }

    // Works with both TextMeshPro and legacy Text, on the object itself or one of its children.
    static void SetText(GameObject target, string text)
    {
        if (target == null) return;

        TMP_Text tmp = target.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null) { tmp.text = text; return; }

        Text legacy = target.GetComponentInChildren<Text>(true);
        if (legacy != null) legacy.text = text;
    }

    // Warns once at start about steps that cannot be played, so mistakes in the Inspector show up early.
    void ValidateSteps(Step[] steps, string label)
    {
        int stackSize = 0;
        var pushed = new HashSet<string>();

        for (int i = 0; i < steps.Length; i++)
        {
            Step s = steps[i];
            if (s.op == Op.Pop)
            {
                if (stackSize == 0)
                    Debug.LogWarning("SinkTask " + label + " step " + (i + 1) + ": pop() on an empty stack.");
                else
                    stackSize--;
                continue;
            }

            if (FindDish(s.color) == null)
                Debug.LogWarning("SinkTask " + label + " step " + (i + 1) + ": no dish with colour '" + s.color + "'.");
            else if (!pushed.Add((s.color ?? "").Trim().ToLowerInvariant()))
                Debug.LogWarning("SinkTask " + label + " step " + (i + 1) + ": '" + s.color + "' is pushed twice; each dish can be pushed only once.");

            stackSize++;
        }
    }
}
