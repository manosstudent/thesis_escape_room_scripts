using UnityEngine;
using TMPro;

public class ScreenController : MonoBehaviour
{
    [Header("Text Objects")]
    public GameObject textEkfonisi;
    public GameObject textKodikas;
    public GameObject textEpilogi;

    public enum ScreenState { Pending, Active, Completed }
    private ScreenState currentState;

    public ScreenState GetCurrentState() { return currentState; }

    [Header("Task Text (για το Active state)")]
    [TextArea] public string kodikasText = "";

    void Awake()
    {
        if (textEkfonisi != null) textEkfonisi.SetActive(false);
        if (textEpilogi != null) textEpilogi.SetActive(false);
    }

    void Start()
    {
        // Ο PuzzleManager αναλαμβάνει τα states
    }

    public void SetState(ScreenState state)
    {
        currentState = state;

        switch (state)
        {
            case ScreenState.Pending:
                textEkfonisi.SetActive(false);
                textKodikas.SetActive(true);
                textKodikas.GetComponent<TMP_Text>().text = "<size=600%>←</size>";
                textEpilogi.SetActive(false);
                break;

            case ScreenState.Active:
                textEkfonisi.SetActive(true);
                textKodikas.SetActive(true);
                textKodikas.GetComponent<TMP_Text>().text = kodikasText;
                textEpilogi.SetActive(true);
                break;

            case ScreenState.Completed:
                textEkfonisi.SetActive(false);
                textKodikas.SetActive(true);
                textKodikas.GetComponent<TMP_Text>().text = "<size=600%>→</size>";
                textEpilogi.SetActive(false);
                break;
        }
    }
}