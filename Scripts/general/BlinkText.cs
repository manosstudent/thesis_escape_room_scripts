using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// Makes a text blink. Put it on the text object itself (TextMeshPro or legacy Text both work),
/// or on any object and point Target at the text.
/// </summary>
public class BlinkText : MonoBehaviour
{
    [Tooltip("Leave empty to use the text on this same object.")]
    public GameObject target;

    [TextArea(1, 3)]
    public string message = "When you are ready: Press Q!";

    [Tooltip("How long the text stays on and off, in seconds.")]
    public float interval = 0.6f;

    [Tooltip("Optional colour, e.g. #FFD54A. Leave empty to keep the text's own colour.")]
    public string colorHex = "";

    [Header("Optional look while blinking (TextMeshPro only)")]
    [Tooltip("Centers the text horizontally while this is blinking, restoring the original alignment when it stops.")]
    public bool centerWhileBlinking = false;
    [Tooltip("Turns on TextMeshPro's Auto Size (best fit) while this is blinking, restoring the original setting when it stops. Ignored if Font Size While Blinking is set above 0.")]
    public bool bestFitWhileBlinking = false;
    [Tooltip("If greater than 0, use this fixed font size while blinking instead of Best Fit, restoring the original size when it stops.")]
    public float fontSizeWhileBlinking = 0f;

    private TMP_Text overriddenText;
    private TextAlignmentOptions originalAlignment;
    private bool originalAutoSize;
    private float originalFontSize;

    void OnEnable()
    {
        ApplyLookOverride();
        StartCoroutine(Blink());
    }

    // A running coroutine is not stopped just by disabling the script; stop it explicitly,
    // so turning this component off (e.g. once the player sits down) actually stops the blinking.
    void OnDisable()
    {
        StopAllCoroutines();
        RestoreLookOverride();
    }

    void ApplyLookOverride()
    {
        if (!centerWhileBlinking && !bestFitWhileBlinking && fontSizeWhileBlinking <= 0f) return;

        GameObject textObject = target != null ? target : gameObject;
        overriddenText = textObject.GetComponentInChildren<TMP_Text>(true);
        if (overriddenText == null) return;

        originalAlignment = overriddenText.alignment;
        originalAutoSize = overriddenText.enableAutoSizing;
        originalFontSize = overriddenText.fontSize;

        if (centerWhileBlinking) overriddenText.alignment = TextAlignmentOptions.Center;

        if (fontSizeWhileBlinking > 0f)
        {
            overriddenText.enableAutoSizing = false;
            overriddenText.fontSize = fontSizeWhileBlinking;
        }
        else if (bestFitWhileBlinking)
        {
            overriddenText.enableAutoSizing = true;
        }
    }

    void RestoreLookOverride()
    {
        if (overriddenText == null) return;

        if (centerWhileBlinking) overriddenText.alignment = originalAlignment;

        if (bestFitWhileBlinking || fontSizeWhileBlinking > 0f)
        {
            // Auto Size (or our own fixed size) overwrites fontSize; turning it off does not put
            // the original size back on its own, so restore it explicitly.
            overriddenText.enableAutoSizing = originalAutoSize;
            overriddenText.fontSize = originalFontSize;
        }

        overriddenText = null;
    }

    IEnumerator Blink()
    {
        GameObject textObject = target != null ? target : gameObject;

        string shown = string.IsNullOrEmpty(colorHex)
            ? message
            : "<color=" + colorHex + ">" + message + "</color>";

        while (true)
        {
            SetText(textObject, shown);
            yield return new WaitForSeconds(interval);
            SetText(textObject, "");
            yield return new WaitForSeconds(interval);
        }
    }

    // Works with both TextMeshPro and legacy Text.
    static void SetText(GameObject target, string text)
    {
        if (target == null) return;

        TMP_Text tmp = target.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null) { tmp.text = text; return; }

        Text legacy = target.GetComponentInChildren<Text>(true);
        if (legacy != null) legacy.text = text;
    }
}
