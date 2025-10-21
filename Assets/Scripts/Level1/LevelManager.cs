using System.Collections;
using TMPro;
using Unity.VisualScripting;
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
    [SerializeField] private GameObject ghostTimer;
    [SerializeField] private TMP_Text ghostTimerText;

    [Header("Ghost Scared Settings")]
    [SerializeField] private float scaredDuration = 10f;
    [SerializeField] private float recoverTime = 3f;

    private Coroutine scaredTimerRoutine;
    private float scaredTimeRemaining;
    public bool IsGhostsScared => scaredTimeRemaining > 0f;

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

    public void StartGhostScaredTimer()
    {
        if (scaredTimerRoutine != null)
            StopCoroutine(scaredTimerRoutine);
        scaredTimerRoutine = StartCoroutine(GhostScaredTimerRoutine());
    }

    IEnumerator GhostScaredTimerRoutine()
    {
        scaredTimeRemaining = scaredDuration;
        ghostTimer?.gameObject.SetActive(true);

        GameManager.I.SetState(GameState.PowerMode);
        foreach (GhostStateManager g in FindObjectsByType<GhostStateManager>(FindObjectsSortMode.None))
            g.EnterScared();

        while (scaredTimeRemaining > 0f)
        {
            scaredTimeRemaining -= Time.deltaTime;
            int minutes = Mathf.FloorToInt(scaredTimeRemaining / 60f);
            int seconds = Mathf.FloorToInt(scaredTimeRemaining % 60f);
            int millis = Mathf.FloorToInt((scaredTimeRemaining * 100f) % 100f);

            ghostTimerText.text = $"Ghosts Scared: {minutes:00}:{seconds:00}:{millis:00}";

            if (scaredTimeRemaining <= recoverTime)
            {
                foreach (GhostStateManager g in FindObjectsByType<GhostStateManager>(FindObjectsSortMode.None))
                    g.EnterRecovering();
            }

            yield return null;
        }

        ghostTimer?.gameObject.SetActive(false);
        foreach (GhostStateManager g in FindObjectsByType<GhostStateManager>(FindObjectsSortMode.None))
            if (g.CurrentState != GhostStateManager.GhostState.Dead)
                g.EnterNormal();

        GameManager.I.SetState(GameState.Playing);
        scaredTimerRoutine = null;
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

        if (PacStudentController.I != null)
            PacStudentController.I.StopMovement();         

        if (PacStudentAnimDriver.I != null)
        {
            PacStudentAnimDriver.I.PlayDeath();
            yield return new WaitForSeconds(2f);      
            PacStudentAnimDriver.I.ClearDeath();   
            GameManager.I.SetState(GameState.Playing);
        }

        PacStudentController.I?.Respawn();
        timerRunning = true;
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

        if (PacStudentController.I != null)
            PacStudentController.I.StopMovement();
        if (PacStudentAnimDriver.I != null)
        {
            PacStudentAnimDriver.I.PlayDeath();
            yield return new WaitForSeconds(2f);
        }
            if (cherryControllerInstance != null)
        {
            Destroy(cherryControllerInstance);
            cherryControllerInstance = null;
        }

        ShowOverlay("GAME OVER");

        AudioManager.I?.OnGameStateChanged(GameState.LevelCleared);
        SaveIfBestScore();

        yield return new WaitForSeconds(6f);
        SceneManager.LoadScene("StartScene");
        GameManager.I.SetState(GameState.Boot);
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
