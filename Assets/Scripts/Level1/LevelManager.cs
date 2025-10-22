using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
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
    public float ScaredTimeRemaining => scaredTimeRemaining;
    public bool IsGhostsScared => scaredTimeRemaining > 0f;

    [Header("Cherry Controller")]
    [SerializeField] private GameObject cherryControllerPrefab;

    [Header("Spawn Tilemaps")]
    [SerializeField] private Tilemap pacStudentSpawn;
    [SerializeField] private Tilemap redGhostSpawn;
    [SerializeField] private Tilemap pinkGhostSpawn;
    [SerializeField] private Tilemap yellowGhostSpawn;
    [SerializeField] private Tilemap greenGhostSpawn;

    [Header("Character Prefabs")]
    [SerializeField] private GameObject pacStudentPrefab;
    [SerializeField] private GameObject redGhostPrefab;
    [SerializeField] private GameObject pinkGhostPrefab;
    [SerializeField] private GameObject yellowGhostPrefab;
    [SerializeField] private GameObject greenGhostPrefab;

    [Header("Ghost Canvases")]
    [SerializeField] private CanvasFollower redGhostCanvas;
    [SerializeField] private CanvasFollower pinkGhostCanvas;
    [SerializeField] private CanvasFollower yellowGhostCanvas;
    [SerializeField] private CanvasFollower greenGhostCanvas;

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

        if (pacStudentPrefab != null && pacStudentSpawn != null)
        {
            Vector3 pos = GetTilemapCenter(pacStudentSpawn);
            Instantiate(pacStudentPrefab, pos, Quaternion.identity);
        }

        GameObject red = null, pink = null, yellow = null, green = null;

        if (redGhostPrefab != null && redGhostSpawn != null)
            red = Instantiate(redGhostPrefab, GetTilemapCenter(redGhostSpawn), Quaternion.identity);

        if (pinkGhostPrefab != null && pinkGhostSpawn != null)
            pink = Instantiate(pinkGhostPrefab, GetTilemapCenter(pinkGhostSpawn), Quaternion.identity);

        if (yellowGhostPrefab != null && yellowGhostSpawn != null)
            yellow = Instantiate(yellowGhostPrefab, GetTilemapCenter(yellowGhostSpawn), Quaternion.identity);

        if (greenGhostPrefab != null && greenGhostSpawn != null)
            green = Instantiate(greenGhostPrefab, GetTilemapCenter(greenGhostSpawn), Quaternion.identity);

        if (red != null && redGhostCanvas != null)
            redGhostCanvas.SetTarget(red.transform);
        if (pink != null && pinkGhostCanvas != null)
            pinkGhostCanvas.SetTarget(pink.transform);
        if (yellow != null && yellowGhostCanvas != null)
            yellowGhostCanvas.SetTarget(yellow.transform);
        if (green != null && greenGhostCanvas != null)
            greenGhostCanvas.SetTarget(green.transform);

        if (cherryControllerPrefab != null)
            cherryControllerInstance = Instantiate(cherryControllerPrefab);

        timerRunning = true;
        timer = 0f;
    }

    private Vector3 GetTilemapCenter(Tilemap tilemap)
    {
        if (tilemap == null) return Vector3.zero;
        foreach (Vector3Int pos in tilemap.cellBounds.allPositionsWithin)
            if (tilemap.HasTile(pos))
                return tilemap.GetCellCenterWorld(pos);
        return Vector3.zero;
    }

    void Update()
    {
        if (timerRunning)
        {
            timer += Time.deltaTime;
            UpdateTimerUI();
        }

        if (!IsGameOver)
        {
            bool allPelletsCleared = GameObject.FindGameObjectsWithTag("Pellet").Length == 0 &&
                                     GameObject.FindGameObjectsWithTag("PowerPellet").Length == 0;
            CheckForGameOverCondition(allPelletsCleared);
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
                    if (g.CurrentState != GhostStateManager.GhostState.Dead)
                        g.EnterRecovering();
            }
            yield return null;
        }
        ghostTimer?.gameObject.SetActive(false);
        foreach (GhostStateManager g in FindObjectsByType<GhostStateManager>(FindObjectsSortMode.None))
            if (g.CurrentState != GhostStateManager.GhostState.Dead)
                g.EnterNormalNoState();
        if (GameManager.I.CurrentState != GameState.AlienDead)
            GameManager.I.SetState(GameState.Playing);
        scaredTimerRoutine = null;
    }

    public void LoseLife()
    {
        if (IsGameOver || PacStudentController.I.IsDead) return;
        CurrentLives--;
        UpdateLivesUI();
        if (CurrentLives > 0)
            StartCoroutine(RespawnRoutine());
        else
            StartCoroutine(GameOverRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        timerRunning = false;;
        if (PacStudentController.I != null)
            PacStudentController.I.StopMovement();
        foreach (GhostStateManager g in FindObjectsByType<GhostStateManager>(FindObjectsSortMode.None))
            g.StopAllMovement();
        if (PacStudentAnimDriver.I != null)
        {
            PacStudentAnimDriver.I.PlayDeath();
            yield return new WaitForSeconds(2f);
            PacStudentAnimDriver.I.ClearDeath();
            GameManager.I.SetState(GameState.Playing);
        }
        Vector3 respawnPos = GetTilemapCenter(pacStudentSpawn);
        PacStudentController.I?.Respawn(respawnPos);
        foreach (GhostStateManager g in FindObjectsByType<GhostStateManager>(FindObjectsSortMode.None))
            g.ResetGhost();
        timerRunning = true;
    }

    public void CheckForGameOverCondition(bool allPelletsCleared)
    {
        if (allPelletsCleared && !IsGameOver)
            StartCoroutine(GameWinRoutine());
    }

    IEnumerator GameOverRoutine()
    {
        IsGameOver = true;
        timerRunning = false;
        FreezeAllCharacters();
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

    IEnumerator GameWinRoutine()
    {
        IsGameOver = true;
        timerRunning = false;
        FreezeAllCharacters();
        if (PacStudentController.I != null)
        {  
            PacStudentController.I.StopMovement(); 
            PacStudentController.I.StopAnimation();
        }
        if (cherryControllerInstance != null)
        {
            Destroy(cherryControllerInstance);
            cherryControllerInstance = null;
        }
        ShowOverlay("GAME OVER, You WIN!");
        GameManager.I.SetState(GameState.LevelCleared);
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

    void FreezeAllCharacters()
    {
        if (PacStudentController.I != null)
            PacStudentController.I.StopMovement();

        foreach (GhostStateManager g in FindObjectsByType<GhostStateManager>(FindObjectsSortMode.None))
            g.StopAllMovement();
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
