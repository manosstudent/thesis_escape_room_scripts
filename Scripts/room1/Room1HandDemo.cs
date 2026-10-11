using UnityEngine;
using System.Collections;
using TMPro;

/// <summary>
/// Plays a one-off tutorial the first time the player sits in Room 1: a hand pops in, "grabs"
/// one of the 4 objects, carries it to the drop box, and drops it — purely to show the mechanic.
/// It never actually solves the real quiz: the object's collider is disabled while carried, and it
/// is put back exactly where it started once the demo ends.
/// </summary>
public class Room1HandDemo : MonoBehaviour
{
    [Header("References")]
    public AutoSitView autoSitView;
    [Tooltip("The BlinkText on TextPipe (\"Sit on Chair!\"). Turned off once the demo starts.")]
    public BlinkText sitPrompt;
    [Tooltip("The root of the hand model, e.g. 'Free Pack - VR Hands ( Rigged )'. Moved and scaled as a whole.")]
    public Transform handRoot;
    [Tooltip("One of Room1QuizManager's 4 objects, used only to demonstrate the drag-and-drop.")]
    public GameObject demoObject;
    [Tooltip("Empty Transform positioned just above the drop box.")]
    public Transform dropPoint;

    [Header("Timing")]
    public float popDuration = 0.8f;
    public float moveDuration = 1.6f;
    [Tooltip("Pause after the object is dropped, before the hand retreats, in seconds.")]
    public float grabPauseDuration = 0.6f;
    public float hoverHeight = 0.15f;
    [Tooltip("After every new yellow caption appears, everything waits this long so the player can read it, in seconds.")]
    public float captionReadDuration = 3.65f;
    [Tooltip("How long the hand holds the object right after it appears, before the next caption, in seconds.")]
    public float handAppearPauseDuration = 3f;

    [Header("Drop")]
    [Tooltip("How far down the object visibly drops into the pipe after being released.")]
    public float fallDepth = 0.3f;
    [Tooltip("How long that fall takes, in seconds.")]
    public float fallDuration = 0.5f;
    [Tooltip("How long \"Your turn!\" stays on the box screen before the real question appears.")]
    public float yourTurnDisplayDuration = 3f;

    [Header("Hand")]
    [Tooltip("Local scale the hand pops in to.")]
    public Vector3 handScale = Vector3.one;
    [Tooltip("Optional extra nudge on top of the auto-captured grab position.")]
    public Vector3 extraGrabOffset = Vector3.zero;

    // Captured at Start from however you positioned Hand Root relative to Demo Object in the
    // Scene view — no need to compute or type any offset by hand.
    private Vector3 grabOffset;
    private bool hasPlayed = false;

    void Start()
    {
        if (handRoot != null && demoObject != null)
            grabOffset = handRoot.position - demoObject.transform.position + extraGrabOffset;
    }

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
        SetBoxScreen("Watch Demo First!");

        if (demoObject == null || handRoot == null || dropPoint == null)
        {
            if (Room1QuizManager.Instance != null) Room1QuizManager.Instance.RefreshQuestionScreen();
            if (GameManager.Instance != null) GameManager.Instance.SetTimerPaused(false);
            yield break;
        }

        yield return new WaitForSeconds(captionReadDuration);
        SetBoxScreen("Click to grab 1 of 4 objects!");
        yield return new WaitForSeconds(captionReadDuration);

        Transform obj = demoObject.transform;
        Transform originalParent = obj.parent;
        Vector3 originalPos = obj.position;
        Quaternion originalRot = obj.rotation;

        Collider objCollider = demoObject.GetComponent<Collider>();
        Rigidbody objRb = demoObject.GetComponent<Rigidbody>();
        bool wasKinematic = objRb != null && objRb.isKinematic;

        if (objCollider != null) objCollider.enabled = false;
        if (objRb != null)
        {
            objRb.isKinematic = true;
            objRb.useGravity = false;
            objRb.velocity = Vector3.zero;
            objRb.angularVelocity = Vector3.zero;
        }

        Vector3 aboveObject = originalPos + Vector3.up * hoverHeight;
        Vector3 aboveDropPoint = dropPoint.position + Vector3.up * hoverHeight;

        // Pop in already gripping the object. The object is attached only after the pop-in, so it
        // is not squashed by the hand's zero scale.
        handRoot.gameObject.SetActive(true);
        handRoot.position = originalPos + grabOffset;
        handRoot.localScale = Vector3.zero;
        yield return ScaleTo(handRoot, handScale, popDuration);
        obj.SetParent(handRoot, true);
        yield return new WaitForSeconds(handAppearPauseDuration);

        // Carry it over to the box, arriving directly at drop height (no extra bump upward).
        SetBoxScreen("Move cursor to drag");
        yield return new WaitForSeconds(captionReadDuration);
        yield return MoveTo(handRoot, aboveObject, moveDuration * 0.5f);
        yield return MoveTo(handRoot, dropPoint.position + grabOffset, moveDuration);

        SetBoxScreen("Drop the object in the box!");
        yield return new WaitForSeconds(captionReadDuration);

        // Drop it, and let it fall the rest of the way into the pipe.
        obj.SetParent(originalParent, true);
        yield return MoveTo(obj, obj.position + Vector3.down * fallDepth, fallDuration);
        yield return new WaitForSeconds(grabPauseDuration);

        // Retreat and pop out.
        yield return MoveTo(handRoot, aboveDropPoint, moveDuration * 0.5f);
        yield return ScaleTo(handRoot, Vector3.zero, popDuration);
        handRoot.gameObject.SetActive(false);

        // Put the object back exactly as it was, ready for the player's real turn.
        obj.SetPositionAndRotation(originalPos, originalRot);
        if (objRb != null)
        {
            objRb.isKinematic = wasKinematic;
            objRb.useGravity = !wasKinematic;
            objRb.velocity = Vector3.zero;
            objRb.angularVelocity = Vector3.zero;
        }
        if (objCollider != null) objCollider.enabled = true;

        SetBoxScreen("Your turn!");
        yield return new WaitForSeconds(yourTurnDisplayDuration);

        if (Room1QuizManager.Instance != null) Room1QuizManager.Instance.RefreshQuestionScreen();
        if (GameManager.Instance != null) GameManager.Instance.SetTimerPaused(false);
    }

    void SetBoxScreen(string text)
    {
        TMP_Text screen = Room1QuizManager.Instance != null ? Room1QuizManager.Instance.questionScreen : null;
        if (screen != null) screen.text = "<color=#FFD54A>" + text + "</color>";
    }

    IEnumerator MoveTo(Transform t, Vector3 target, float duration)
    {
        Vector3 start = t.position;
        duration = Mathf.Max(duration, 0.01f);
        for (float time = 0f; time < duration; time += Time.deltaTime)
        {
            t.position = Vector3.Lerp(start, target, time / duration);
            yield return null;
        }
        t.position = target;
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
