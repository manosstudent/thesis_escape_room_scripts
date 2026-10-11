using UnityEngine;
using System.Collections;
using TMPro;

/// <summary>
/// Plays a one-off tutorial the first time the player sits in Room 2: a hand pops in and presses
/// a few switches to show how the binary value is built, including pressing one switch twice to
/// show it going back to 0. It never submits a real answer: at the end every switch is reset off
/// and the real problem text is shown again, exactly as it would be for a fresh turn.
///
/// The hand's position AND rotation at every step come from empty Transforms you place by hand in
/// the Scene view (drag the hand there until it looks right, then drop an empty object at that
/// spot) — nothing is computed or guessed.
/// </summary>
public class Room2HandDemo : MonoBehaviour
{
    [Header("References")]
    public AutoSitView autoSitView;
    [Tooltip("Optional. A BlinkText \"Sit on Chair!\" prompt, turned off once the demo starts.")]
    public BlinkText sitPrompt;
    [Tooltip("The root of the hand model (duplicated from Room 1). Moved and scaled as a whole.")]
    public Transform handRoot;

    [Header("Hand poses (empty Transforms placed exactly where you want the hand)")]
    [Tooltip("Where the hand pops in: far from the switches.")]
    public Transform startPose;
    [Tooltip("Passed through between two presses, closer to the chair, so the hand visibly retreats " +
             "and then approaches again. Can be the same Transform as Start Pose.")]
    public Transform retreatPose;
    [Tooltip("Where the hand goes to \"press\" Light5 (Enter). Never actually presses it.")]
    public Transform enterPose;

    [Header("Switches to press, in order")]
    [Tooltip("The switches, in the order the hand should press them. Repeat one (e.g. Light1, Light2, " +
             "Light1) to show it toggling back to 0.")]
    public SwitchToggle[] pressSequence;
    [Tooltip("One pose per entry above, same order and length: exactly where the hand should be to press that switch.")]
    public Transform[] pressPoses;

    [Header("Timing")]
    public float popDuration = 0.8f;
    public float moveDuration = 1.2f;
    public float pressPauseDuration = 0.6f;
    [Tooltip("After every new yellow caption appears, everything waits this long so the player can read it, in seconds.")]
    public float captionReadDuration = 4f;
    [Tooltip("How long the hand rests on the Enter button before it disappears, in seconds.")]
    public float enterPromptDuration = 2f;

    [Header("Hand")]
    [Tooltip("Local scale the hand pops in to.")]
    public Vector3 handScale = Vector3.one;

    [Header("Box screen text")]
    [Tooltip("Fixed font size for the yellow demo captions. 0 = keep the screen's own size.")]
    public float demoFontSize = 0f;
    [Tooltip("The \"Read the question...\" caption is longer, so it uses Demo Font Size times this. 1 = same size as the other captions.")]
    [Range(0.3f, 1f)]
    public float readQuestionSizeScale = 0.7f;

    private bool hasPlayed = false;
    private TextAlignmentOptions originalAlignment;
    private bool originalAutoSize;
    private float originalFontSize;

    void Update()
    {
        if (hasPlayed || autoSitView == null || !autoSitView.IsSitting) return;
        hasPlayed = true;
        StartCoroutine(PlayDemo());
    }

    IEnumerator PlayDemo()
    {
        if (sitPrompt != null) sitPrompt.enabled = false;
        if (GameManager.Instance != null) GameManager.Instance.SetTimerPaused(true);

        TMP_Text screen = EnterButton.Instance != null ? EnterButton.Instance.questionScreen : null;
        if (screen != null)
        {
            originalAlignment = screen.alignment;
            originalAutoSize = screen.enableAutoSizing;
            originalFontSize = screen.fontSize;
        }

        SetScreen("Watch Demo First!");

        if (handRoot == null || startPose == null || pressSequence == null || pressSequence.Length == 0)
        {
            if (screen != null) { screen.alignment = originalAlignment; screen.enableAutoSizing = originalAutoSize; screen.fontSize = originalFontSize; }
            if (EnterButton.Instance != null) EnterButton.Instance.RefreshScreens();
            if (GameManager.Instance != null) GameManager.Instance.SetTimerPaused(false);
            yield break;
        }

        yield return new WaitForSeconds(captionReadDuration);

        // Pop in, far from the switches.
        handRoot.gameObject.SetActive(true);
        handRoot.SetPositionAndRotation(startPose.position, startPose.rotation);
        handRoot.localScale = Vector3.zero;
        yield return ScaleTo(handRoot, handScale, popDuration);

        SetScreen("Read the question and choose an answer from screens A, B, C & D", readQuestionSizeScale);
        yield return new WaitForSeconds(captionReadDuration);

        SetScreen("Press switches to write the correct answer");
        yield return new WaitForSeconds(captionReadDuration);

        for (int i = 0; i < pressSequence.Length; i++)
        {
            SwitchToggle sw = pressSequence[i];
            if (sw == null)
            {
                Debug.Log("[HAND-TEMP] pressSequence[" + i + "] is null, skipping");   // TEMP
                continue;
            }

            // Retreat towards the chair between presses, then approach the next switch.
            if (i > 0 && retreatPose != null)
                yield return MoveRotateTo(handRoot, retreatPose.position, retreatPose.rotation, moveDuration * 0.5f);

            Transform pose = (pressPoses != null && i < pressPoses.Length) ? pressPoses[i] : null;
            Debug.Log("[HAND-TEMP] step " + i + " sw=" + sw.name + " pose=" + (pose != null ? pose.name + " @ " + pose.position : "NULL")
                      + " handRoot before=" + handRoot.position);   // TEMP
            if (pose != null)
                yield return MoveRotateTo(handRoot, pose.position, pose.rotation, moveDuration);
            Debug.Log("[HAND-TEMP] step " + i + " handRoot after=" + handRoot.position);   // TEMP

            sw.isOn = !sw.isOn;
            if (sw.binaryDisplay != null) sw.binaryDisplay.UpdateDisplay();

            yield return new WaitForSeconds(pressPauseDuration);
        }

        SetScreen("Press Enter to submit the value");
        yield return new WaitForSeconds(captionReadDuration);
        if (retreatPose != null)
            yield return MoveRotateTo(handRoot, retreatPose.position, retreatPose.rotation, moveDuration * 0.5f);
        if (enterPose != null)
            yield return MoveRotateTo(handRoot, enterPose.position, enterPose.rotation, moveDuration);
        yield return new WaitForSeconds(enterPromptDuration);

        // Pop out.
        yield return ScaleTo(handRoot, Vector3.zero, popDuration);
        handRoot.gameObject.SetActive(false);

        // Leave everything exactly as a fresh turn should start.
        if (screen != null) { screen.alignment = originalAlignment; screen.enableAutoSizing = originalAutoSize; screen.fontSize = originalFontSize; }
        if (EnterButton.Instance != null)
        {
            EnterButton.Instance.ResetAllSwitches();
            EnterButton.Instance.RefreshScreens();
        }

        if (GameManager.Instance != null) GameManager.Instance.SetTimerPaused(false);
    }

    void SetScreen(string text, float sizeScale = 1f)
    {
        TMP_Text screen = EnterButton.Instance != null ? EnterButton.Instance.questionScreen : null;
        if (screen == null) return;
        screen.alignment = TextAlignmentOptions.Center;
        if (demoFontSize > 0f)
        {
            screen.enableAutoSizing = false;
            screen.fontSize = demoFontSize * sizeScale;
        }
        else
        {
            screen.enableAutoSizing = true;
        }
        screen.text = "<color=#FFD54A>" + text + "</color>";
    }

    IEnumerator MoveRotateTo(Transform t, Vector3 targetPos, Quaternion targetRot, float duration)
    {
        Vector3 startP = t.position;
        Quaternion startR = t.rotation;
        duration = Mathf.Max(duration, 0.01f);
        for (float time = 0f; time < duration; time += Time.deltaTime)
        {
            float f = time / duration;
            t.position = Vector3.Lerp(startP, targetPos, f);
            t.rotation = Quaternion.Slerp(startR, targetRot, f);
            yield return null;
        }
        t.SetPositionAndRotation(targetPos, targetRot);
    }

    IEnumerator ScaleTo(Transform t, Vector3 target, float duration)
    {
        Vector3 start = t.localScale;
        duration = Mathf.Max(duration, 0.01f);
        for (float time = 0f; time < duration; time += Time.deltaTime)
        {
            t.localScale = Vector3.Lerp(start, target, time / duration);
            yield return null;
        }
        t.localScale = target;
    }
}
