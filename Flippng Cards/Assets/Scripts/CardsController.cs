using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class LevelConfig
{
    public GridLayoutGroup.Constraint gridConstraint = GridLayoutGroup.Constraint.FixedRowCount;
    public int gridConstraintCount = 0;
    public Vector2 gridCellSize = Vector2.zero;
    public Vector2 gridSpacing = Vector2.zero;
    public int maxUniqueSprites = 0;
}

public class CardsController : MonoBehaviour
{

    [SerializeField] Cards cardPrefab;
    [SerializeField] private Transform cardsParent;
    [SerializeField] private Sprite[] sprites;
    [SerializeField] private Text matchCountText;
    [SerializeField] private Text turnsCountText;
    [SerializeField] private Text comboText;
    [SerializeField] private Text scoreText;
    [SerializeField] private Text highScoreText;
    [SerializeField] private Text currentLevelText;
    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private AudioSource backgroundMusicSource;
    [SerializeField] private AudioClip matchSuccessSound;
    [SerializeField] private AudioClip matchFailSound;
    [SerializeField] private AudioClip comboSound;
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private int basePointsPerMatch = 100;

    [Header("Levels")]
    [SerializeField] private List<LevelConfig> levels = new List<LevelConfig>();
    [SerializeField] private int startingLevel = 0;

    // current runtime settings (copied from level config)
    private GridLayoutGroup.Constraint gridConstraint = GridLayoutGroup.Constraint.FixedRowCount;
    private int gridConstraintCount = 0;
    private Vector2 gridCellSize = Vector2.zero;
    private Vector2 gridSpacing = Vector2.zero;
    private int maxUniqueSprites = 0;

    private List<Sprite> spritePairs;

    Cards firstSelectedCard;
    Cards secondSelectedCard;
    bool inputLocked;

    int matchCounts;
    int turnsCount;
    int comboCount;
    int score;
    int highScore;
    // Start is called before the first frame update
    void Start()
    {
        matchCounts = 0;
        turnsCount = 0;
        comboCount = 0;
        score = 0;
        LoadHighScore();
        LoadCurrentLevel();
        ApplyLevelConfig();
        PlayBackgroundMusic();
    }
    private void PrepareSprites()
    {
        spritePairs = new List<Sprite>();
        int count = sprites.Length;
        if (maxUniqueSprites > 0 && maxUniqueSprites < count)
            count = maxUniqueSprites;

        for (int i = 0; i < count; i++)
        {
            spritePairs.Add(sprites[i]);
            spritePairs.Add(sprites[i]);
        }
        ShuffleSprite(spritePairs);
    }
    private void ClearCards()
    {
        foreach (Transform child in cardsParent)
        {
            Destroy(child.gameObject);
        }
    }
    public void StartLevel()
    {
        StopAllCoroutines();
        ClearCards();
        PrepareSprites();
        AutoConfigureGrid();
        CreateCards();
        matchCounts = 0;
        turnsCount = 0;
        comboCount = 0;
        score = 0;
        UpdateUI();
        inputLocked = true;
        StartCoroutine(RevealAllThenHide());
    }
    void CreateCards()
    {
        for (int i = 0; i < spritePairs.Count; i++)
        {
            Cards newCard = Instantiate(cardPrefab, cardsParent);
            newCard.SetIconSoprite(spritePairs[i]);
            newCard.cardsController = this;
        }
    }
    private void AutoConfigureGrid()
    {
        GridLayoutGroup grid = cardsParent.GetComponent<GridLayoutGroup>();
        if (grid == null) return;

        int total = spritePairs != null ? spritePairs.Count : 0;
        if (total <= 0) return;

        // determine constraint count
        if (gridConstraintCount > 0)
        {
            grid.constraintCount = gridConstraintCount;
        }
        else
        {
            int cols = Mathf.CeilToInt(Mathf.Sqrt(total));
            grid.constraintCount = cols;
        }
        grid.constraint = gridConstraint;//GridLayoutGroup.Constraint.FixedRowCount;

        // apply overrides if provided
        if (gridCellSize != Vector2.zero)
            grid.cellSize = gridCellSize;
        if (gridSpacing != Vector2.zero)
            grid.spacing = gridSpacing;
    }
    public void SetSelectedCard(Cards selectedCard)
    {
        // Implement logic to handle card selection and matching
        if (inputLocked) return;
        if(selectedCard.isSelected ==  false)
        {
            selectedCard.ShowIcon();
            if(firstSelectedCard == null)
            {
                firstSelectedCard = selectedCard;
                return;
            }
            if(secondSelectedCard == null)
            {
                secondSelectedCard = selectedCard;
                turnsCount++;
                UpdateUI();
                StartCoroutine(CheckForMatch(firstSelectedCard, secondSelectedCard));
                firstSelectedCard = null;
                secondSelectedCard = null;
            }
        }
    } 
    IEnumerator CheckForMatch(Cards a, Cards b)
    {
        yield return new WaitForSeconds(0.3f);
        if(a.iconSprite == b.iconSprite)
        {
            // Cards match, keep them revealed or disable them
            matchCounts++;
            comboCount++;
            
            // Calculate score: base points multiplied by combo count
            int matchPoints = basePointsPerMatch * (1 + comboCount);
            score += matchPoints;
            
            UpdateUI();
            
            // Play match success sound
            if (sfxAudioSource != null && matchSuccessSound != null)
            {
                sfxAudioSource.PlayOneShot(matchSuccessSound);
            }
            
            // Play combo sound on significant combo milestones
            if (comboCount > 1 && comboCount % 3 == 0)
            {
                if (sfxAudioSource != null && comboSound != null)
                {
                    sfxAudioSource.PlayOneShot(comboSound);
                }
            }
            
            if(matchCounts >= spritePairs.Count / 2)
            {
                // All cards matched! Play scale animation
                // Save high score if current score is higher
                if (score > highScore)
                {
                    highScore = score;
                    SaveHighScore();
                }
                SaveLastScore(score);
                yield return PlayWinAnimation();
                // advance to next level automatically
                ProceedToNextLevel();
            }

        }
        else
        {
            // Cards do not match, hide their icons again
            // Play fail sound
            if (sfxAudioSource != null && matchFailSound != null)
            {
                sfxAudioSource.PlayOneShot(matchFailSound);
            }
            
            // Reset combo on mismatch
            comboCount = 0;
            UpdateUI();
            
            a.HideIcon();
            b.HideIcon();
        }
        //firstSelectedCard = null;
        //secondSelectedCard = null;
    }

    private IEnumerator RevealAllThenHide()
    {
        Cards[] allCards = cardsParent.GetComponentsInChildren<Cards>();
        for (int i = 0; i < allCards.Length; i++)
        {
            if (allCards[i] != null)
                allCards[i].ShowIcon();
        }
        yield return new WaitForSeconds(1f);
        for (int i = 0; i < allCards.Length; i++)
        {
            if (allCards[i] != null)
                allCards[i].HideIcon();
        }
        // allow time for flip animation to finish
        yield return new WaitForSeconds(0.45f);
        inputLocked = false;
    }
    
    void ShuffleSprite(List<Sprite> spritesList)
    {
        for (int i = 0; i < spritesList.Count; i++)
        {
            Sprite temp = spritesList[i];
            int randomIndex = Random.Range(i, spritesList.Count);
            spritesList[i] = spritesList[randomIndex];
            spritesList[randomIndex] = temp;
        }
    }
    private IEnumerator PlayWinAnimation()
    {
        Cards[] allCards = cardsParent.GetComponentsInChildren<Cards>();
        float animationDuration = 0.5f;
        float elapsedTime = 0f;
        Vector3 originalScale = Vector3.one;
        Vector3 targetScale = new Vector3(1.2f, 1.2f, 1.2f);

        // Scale up
        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / animationDuration;
            Vector3 newScale = Vector3.Lerp(originalScale, targetScale, progress);

            foreach (Cards card in allCards)
            {
                card.transform.localScale = newScale;
            }
            yield return null;
        }

        // Scale back down
        elapsedTime = 0f;
        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / animationDuration;
            Vector3 newScale = Vector3.Lerp(targetScale, originalScale, progress);

            foreach (Cards card in allCards)
            {
                card.transform.localScale = newScale;
            }
            yield return null;
        }

        // Ensure scale is reset to normal
        foreach (Cards card in allCards)
        {
            card.transform.localScale = originalScale;
        }
    }
    private void UpdateUI()
    {
        if (matchCountText != null)
        {
            matchCountText.text = "Matches: " + matchCounts;
        }
        if (turnsCountText != null)
        {
            turnsCountText.text = "Turns: " + turnsCount;
        }
        if (comboText != null)
        {
            if (comboCount > 0)
            {
                comboText.text = "COMBO x" + comboCount;
                comboText.gameObject.SetActive(true);
            }
            else
            {
                comboText.gameObject.SetActive(false);
            }
        }
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score;
        }
        if (highScoreText != null)
        {
            highScoreText.text = "High Score: " + highScore;
        }
        if (currentLevelText != null)
        {
            int displayLevel = Mathf.Clamp(currentLevel, 0, Mathf.Max(levels.Count - 1, 0)) + 1;
            int totalLevels = Mathf.Max(levels.Count, 1);
            currentLevelText.text = "Level: " + displayLevel + "/" + totalLevels;
        }
    }
    private void PlayBackgroundMusic()
    {
        if (backgroundMusicSource != null && backgroundMusic != null)
        {
            backgroundMusicSource.clip = backgroundMusic;
            backgroundMusicSource.loop = true;
            backgroundMusicSource.Play();
        }
    }

    private void SaveHighScore()
    {
        PlayerPrefs.SetInt("HighScore", highScore);
        PlayerPrefs.Save();
    }

    public void SaveLastScore(int value)
    {
        PlayerPrefs.SetInt("LastScore", value);
        PlayerPrefs.Save();
    }

    private int currentLevel = 0;

    public void SaveCurrentLevel()
    {
        PlayerPrefs.SetInt("CurrentLevel", currentLevel);
        PlayerPrefs.Save();
    }

    private void LoadCurrentLevel()
    {
        currentLevel = PlayerPrefs.GetInt("CurrentLevel", startingLevel);
        if (currentLevel < 0) currentLevel = 0;
        if (currentLevel >= levels.Count) currentLevel = levels.Count - 1;
    }

    private void ApplyLevelConfig()
    {
        if (levels == null || levels.Count == 0)
            return;

        LevelConfig cfg = levels[Mathf.Clamp(currentLevel, 0, levels.Count - 1)];
        gridConstraintCount = cfg.gridConstraintCount;
        gridCellSize = cfg.gridCellSize;
        gridSpacing = cfg.gridSpacing;
        maxUniqueSprites = cfg.maxUniqueSprites;
    }

    public void ProceedToNextLevel()
    {
        if (currentLevel < levels.Count - 1)
        {
            currentLevel++;
            SaveCurrentLevel();
            ApplyLevelConfig();
            StartLevel();
        }
    }

    public void GoToPreviousLevel()
    {
        if (currentLevel > 0)
        {
            currentLevel--;
            SaveCurrentLevel();
            ApplyLevelConfig();
            StartLevel();
        }
    }

    public void AddLevelBeforeCurrent(LevelConfig newLevel)
    {
        if (levels == null)
            levels = new List<LevelConfig>();
        levels.Insert(currentLevel, newLevel);
        // player stays on same logical level but index shifts
    }

    private void LoadHighScore()
    {
        highScore = PlayerPrefs.GetInt("HighScore", 0);
    }

    public int CurrentLevel => currentLevel;
    public int LevelsCount => levels != null ? levels.Count : 0;
    public int HighScore => highScore;
    public int Score => score;

}
