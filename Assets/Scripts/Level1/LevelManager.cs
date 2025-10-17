using System.Collections;
using TMPro;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    public static LevelManager I { get; private set; }

    [Header("Level Settings")]
    [SerializeField] private int startingLives = 3;

    [Header("HUD References")]
    [SerializeField] private Image blurImage;
    [SerializeField] private TMP_Text overlayText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private Transform livesContainer;
    [SerializeField] private GameObject lifeIconPrefab;

    [Header("Cherry Controller")]
    [SerializeField] private GameObject cherryControllerPrefab;

    public int CurrentScore { get; private set; }
    public int HighScore { get; private set; }
    public int CurrentLives { get; private set; }
    public bool IsGameOver { get; private set; }

    private float timer;
    private bool timerRunning;
    private GameObject cherryControllerInstance;

    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;

        LoadHighScore();
        CurrentLives = startingLives;
        CurrentScore = 0;
        timer = 0f;

        UpdateScoreUI();
        UpdateLivesUI();
        UpdateTimerUI();

        GameManager.I?.SetState(GameState.Intro);
        StartCoroutine(StartRoundCountdown());
    }

    IEnumerator StartRoundCountdown()
    {
        ShowOverlay("3");
        timerRunning = false;

        string[] sequence = { "3", "2", "1", "GO!" };

        foreach (string s in sequence)
        {
            ShowOverlay(s);
            yield return new WaitForSeconds(1f);
        }
        yield return new WaitForSeconds(0.3f);
        HideOverlay();
        StartRound();
    }

    void StartRound()
    {
        GameManager.I?.SetState(GameState.Playing);
        if (cherryControllerPrefab != null)
            cherryControllerInstance = Instantiate(cherryControllerPrefab);
        timerRunning = true;
        timer = 0f;
    }

    void Update()
    {
        if (timerRunning)
        {
            timer += Time.deltaTime;
            UpdateTimerUI();
        }
    }

    public void AddScore(int amount)
    {
        if (amount <= 0) return;
        CurrentScore += amount;
        UpdateScoreUI();
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = $"{CurrentScore}";
    }

    void UpdateLivesUI()
    {
        foreach (Transform child in livesContainer)
            Destroy(child.gameObject);

        for (int i = 0; i < CurrentLives; i++)
            Instantiate(lifeIconPrefab, livesContainer);
    }

    void UpdateTimerUI()
    {
        if (timerText == null) return;
        int minutes = Mathf.FloorToInt(timer / 60f);
        int seconds = Mathf.FloorToInt(timer % 60f);
        int millis = Mathf.FloorToInt((timer * 100f) % 100f);
        timerText.text = $"TIMER : {minutes:00}:{seconds:00}:{millis:00}";
    }

    public void LoseLife()
    {
        if (IsGameOver ) return;
        CurrentLives--;
        UpdateLivesUI();

        if (CurrentLives > 0)
            StartCoroutine(RespawnRoutine());
        else
            StartCoroutine(GameOverRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        timerRunning = false;
        yield return new WaitForSeconds(2f);
        StartCoroutine(StartRoundCountdown());
    }

    public void CheckForGameOverCondition(bool allPelletsCleared)
    {
        if (allPelletsCleared && !IsGameOver)
            StartCoroutine(GameOverRoutine());
    }

    IEnumerator GameOverRoutine()
    {
        IsGameOver = true;
        timerRunning = false;

        if (cherryControllerInstance != null)
        {
            Destroy(cherryControllerInstance);
            cherryControllerInstance = null;
        }

        ShowOverlay("GAME OVER");

        AudioManager.I?.OnGameStateChanged(GameState.LevelCleared);
        SaveIfBestScore();

        yield return new WaitForSeconds(3f);
        SceneManager.LoadScene("StartScene");
    }

    void SaveIfBestScore()
    {
        float bestTime = PlayerPrefs.GetFloat($"L1BestTime", float.MaxValue);
        int bestScore = PlayerPrefs.GetInt($"L1HighScore", 0);
        bool isBetter = CurrentScore > bestScore || (CurrentScore == bestScore && timer < bestTime);

        if (isBetter)
        {
            PlayerPrefs.SetInt($"L1HighScore", CurrentScore);
            PlayerPrefs.SetFloat($"L1BestTime", timer);
            PlayerPrefs.Save();
        }
    }

    void LoadHighScore()
    {
        HighScore = PlayerPrefs.GetInt($"L1HighScore", 0);
    }

    void ShowOverlay(string t)
    {
        blurImage.enabled = true;
        overlayText.text = t;
    }
    void HideOverlay() 
    { 
        overlayText.text = "";
        blurImage.enabled = false;
    }

    void OnDestroy()
    {
        if (cherryControllerInstance != null)
        {
            Destroy(cherryControllerInstance);
            cherryControllerInstance = null;
        }
    }
}
