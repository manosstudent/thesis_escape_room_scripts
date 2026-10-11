using UnityEngine;

public class PuzzleManager : MonoBehaviour
{
    public static PuzzleManager Instance;

    [Header("Screens")]
    public ScreenController screen1;
    public ScreenController screen2;
    public ScreenController screen3;
    public ScreenController screen4;

    [Header("Screen Interactions")]
    public ScreenInteraction interaction1;
    public ScreenInteraction interaction2;
    public ScreenInteraction interaction3;
    public ScreenInteraction interaction4;

    [Header("Door")]
    public DoorController door;

    private int[] correctAnswers = { 2, 1, 3, 1 };

    [SerializeField] private int currentScreen = 0;
    private ScreenController[] screens;
    private ScreenInteraction[] interactions;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        screens = new ScreenController[] { screen1, screen2, screen3, screen4 };
        interactions = new ScreenInteraction[] { interaction1, interaction2, interaction3, interaction4 };

        screens[0].SetState(ScreenController.ScreenState.Active);
        for (int i = 1; i < screens.Length; i++)
            screens[i].SetState(ScreenController.ScreenState.Pending);

        Debug.Log("PuzzleManager Start - Screen1 state: " + screens[0].GetCurrentState());
    }

    public void OnLeverPulled(int screenIndex, int answerIndex)
    {
        if (screenIndex != currentScreen) return;

        if (answerIndex == correctAnswers[currentScreen])
        {
            interactions[currentScreen].Unlock();
            screens[currentScreen].SetState(ScreenController.ScreenState.Completed);
            currentScreen++;

            if (currentScreen < screens.Length)
            {
                screens[currentScreen].SetState(ScreenController.ScreenState.Active);
            }
            else
            {
                Debug.Log("Όλα σωστά! Η πόρτα ανοίγει!");
                if (door != null) door.OpenDoor();
            }
        }
        else
        {
            if (ScoreManager.Instance != null)
                ScoreManager.Instance.RemovePoints(1);
            Debug.Log("Λάθος! -1 πόντος.");
        }
    }
}