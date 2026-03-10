using UnityEngine;
using UnityEngine.UI;

public class UIMenuController : MonoBehaviour
{
    [SerializeField] private CardsController cardsController;
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private GameObject gameRoot;
    [SerializeField] private Text menuLastLevelText;
    [SerializeField] private Text menuHighScoreText;
    [SerializeField] private Text menuLastScoreText;

    private const string LastScoreKey = "LastScore";

    private void Awake()
    {
        if (cardsController == null)
            cardsController = FindObjectOfType<CardsController>();
    }

    private void Start()
    {
        ShowMenu();
    }

    public void ShowMenu()
    {
        if (menuRoot != null) menuRoot.SetActive(true);
        if (gameRoot != null) gameRoot.SetActive(false);
        UpdateMenuUI();
    }

    public void StartGameFromMenu()
    {
        if (menuRoot != null) menuRoot.SetActive(false);
        if (gameRoot != null) gameRoot.SetActive(true);
        if (cardsController != null)
            cardsController.StartLevel();
    }

    public void BackToMenu()
    {
        if (cardsController != null)
        {
            cardsController.SaveLastScore(cardsController.Score);
            cardsController.SaveCurrentLevel();
        }
        ShowMenu();
    }

    private void UpdateMenuUI()
    {
        if (menuLastLevelText != null && cardsController != null)
        {
            int levelsCount = Mathf.Max(cardsController.LevelsCount, 1);
            int displayLevel = Mathf.Clamp(cardsController.CurrentLevel, 0, levelsCount - 1) + 1;
            menuLastLevelText.text = "Last Level: " + displayLevel + "/" + levelsCount;
        }
        if (menuHighScoreText != null && cardsController != null)
        {
            menuHighScoreText.text = "High Score: " + cardsController.HighScore;
        }
        if (menuLastScoreText != null)
        {
            int lastScore = PlayerPrefs.GetInt(LastScoreKey, 0);
            menuLastScoreText.text = "Last Score: " + lastScore;
        }
    }
}
