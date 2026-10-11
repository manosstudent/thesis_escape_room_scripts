using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [Header("Settings")]
    public int startingScore = 40;

    [Header("UI")]
    public Text[] scoreTexts;

    private int currentScore;

    public int CurrentScore => currentScore;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        currentScore = startingScore;
        UpdateUI();
    }

    public void AddPoints(int points)
    {
        currentScore += points;
        currentScore = Mathf.Max(0, currentScore);
        UpdateUI();
    }

    public void RemovePoints(int points)
    {
        currentScore -= points;
        currentScore = Mathf.Max(0, currentScore);
        UpdateUI();

        if (currentScore <= 0 && GameManager.Instance != null)
            GameManager.Instance.LoseByScore();
    }

    private void UpdateUI()
    {
        foreach (Text t in scoreTexts)
            if (t != null)
                t.text = "Score: " + currentScore;
    }
}
