using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class Level2Manager : MonoBehaviour
{
    public static Level2Manager I { get; private set; }

    [Header("Level Settings")]
    [SerializeField] private int startingLives = 3;

    [Header("HUD References")]
    [SerializeField] private Image blurImage;
    [SerializeField] private TMP_Text overlayText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private TMP_Text bulletsText;
    [SerializeField] private Transform livesContainer;
    [SerializeField] private GameObject lifeIconPrefab;



    [Header("Spawn Tilemaps")]
    [SerializeField] private Tilemap pacStudentSpawn;


    [Header("Character Prefabs")]
    [SerializeField] private GameObject pacStudentPrefab;
    [SerializeField] private GameObject redGhostPrefab;
    [SerializeField] private GameObject pinkGhostPrefab;
    [SerializeField] private GameObject yellowGhostPrefab;
    [SerializeField] private GameObject greenGhostPrefab;

    public int CurrentScore { get; private set; }
    public int HighScore { get; private set; }
    public int CurrentLives { get; private set; }
    public bool IsGameOver { get; private set; }

    public int BulletCount { get; private set; } = 1;

    public int RoundNumber { get; private set; } = 1;

    private float timer;
    private bool timerRunning;

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
        UpdateRoundUI();
        UpdateBulletsUI();
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

    void UpdateRoundUI()
    {
        if (roundText != null)
            roundText.text = $"ROUND : {RoundNumber}";
    }

    void UpdateBulletsUI()
    {
        if (bulletsText != null)
            bulletsText.text = $"BULLETS : {BulletCount}";
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
        if (IsGameOver || PacStudentControllerL2.I.IsDead) return;
        CurrentLives--;
        UpdateLivesUI();
        if (CurrentLives > 0)
            StartCoroutine(InvulnerableRoutine());
        else
            StartCoroutine(GameOverRoutine());
    }

    public void GetLife()
    {
        if (CurrentLives >= startingLives)
        {
            AddScore(30);
        }
        else
        {
            CurrentLives++;
            UpdateLivesUI();
        }
    }

    public void getBullet()
    {
        BulletCount++;
        UpdateBulletsUI();
    }

    public void UseBullet()
    {
        if (BulletCount > 0)
        {
            BulletCount--;
            UpdateBulletsUI();
        }
    }

    IEnumerator InvulnerableRoutine()
    {
        //Todo: make pacstudent blink and invulnerable for 3 seconds
        yield return null;
    }


    IEnumerator GameOverRoutine()
    {
        IsGameOver = true;
        timerRunning = false;
        FreezeAllCharacters();
        if (PacStudentControllerL2.I != null)
            PacStudentControllerL2.I.StopMovement();
        if (PacStudentAnimDriverL2.I != null)
        {
            PacStudentAnimDriverL2.I.PlayDeath();
            yield return new WaitForSeconds(2f);
        }
        ShowOverlay($"Game Over! You Reached Round {RoundNumber}");
        AudioManager.I?.OnGameStateChanged(GameState.LevelCleared);
        SaveMoney();
        SaveIfBestScore();
        yield return new WaitForSeconds(6f);
        SceneManager.LoadScene("StartScene");
        GameManager.I.SetState(GameState.Boot);
    }

    void SaveIfBestScore()
    {
        float bestTime = PlayerPrefs.GetFloat($"L2BestTime", float.MaxValue);
        int bestScore = PlayerPrefs.GetInt($"L2HighRound", 0);
        bool isBetter = CurrentScore > bestScore || (CurrentScore == bestScore && timer < bestTime);
        if (isBetter)
        {
            PlayerPrefs.SetInt($"L2HighRound", CurrentScore);
            PlayerPrefs.SetFloat($"L2BestTime", timer);
            PlayerPrefs.Save();
        }
    }

    void SaveMoney()
    {
        float playerMoney = PlayerPrefs.GetFloat($"PlayerMoney", float.MinValue);
        playerMoney += CurrentScore/10;
        PlayerPrefs.SetFloat($"PlayerMoney", playerMoney);
        PlayerPrefs.Save();
    }

    void LoadHighScore()
    {
        HighScore = PlayerPrefs.GetInt($"L2HighRound", 0);
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
        if (PacStudentControllerL2.I != null)
            PacStudentControllerL2.I.StopMovement();

        foreach (GhostStateManagerL2 g in FindObjectsByType<GhostStateManagerL2>(FindObjectsSortMode.None))
            g.StopAllMovement();
    }

}
