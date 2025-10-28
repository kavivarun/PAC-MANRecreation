using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class Level2Manager : MonoBehaviour
{
    public static Level2Manager I { get; private set; }

    [Header("Level Settings")]
    private int startingLives = 3;

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
    [SerializeField] private Tilemap ghostSpawnTilemap;

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
    private List<GameObject> activeGhosts = new List<GameObject>();
    private GameObject[] ghostPrefabs;
    private bool waveSpawned = false;

    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        LoadHighScore();
        startingLives += (int)PlayerPrefs.GetFloat("Upgrade_H_Value", 0);
        CurrentLives = startingLives;
        CurrentScore = 0;
        timer = 0f;
        ghostPrefabs = new GameObject[] { redGhostPrefab, pinkGhostPrefab, yellowGhostPrefab, greenGhostPrefab };
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
        if (pacStudentPrefab != null && pacStudentSpawn != null && GameObject.FindWithTag("Player") == null)
        {
            Vector3 pos = GetTilemapCenter(pacStudentSpawn);
            Instantiate(pacStudentPrefab, pos, Quaternion.identity);
        }
        timerRunning = true;
        timer = 0f;
        SpawnWave();
    }

    void SpawnWave()
    {
        activeGhosts.Clear();
        int count = Mathf.CeilToInt(RoundNumber / 2f);
        int hp = Mathf.FloorToInt((RoundNumber + 1) / 2f);
        for (int i = 0; i < count; i++)
        {
            GameObject prefab = ghostPrefabs[Random.Range(0, ghostPrefabs.Length)];
            GameObject ghost = Instantiate(prefab);
            var gsm = ghost.GetComponent<GhostStateManagerL2>();
            if (gsm != null)
            {
                // Use the new method instead of reflection
                gsm.SetGhostLife(hp);
            }
            activeGhosts.Add(ghost);
        }
        waveSpawned = true;
        UpdateRoundUI();
    }


    void Update()
    {
        if (timerRunning)
        {
            timer += Time.deltaTime;
            UpdateTimerUI();
        }
        CheckWaveCompletion();
    }

    public void OnGhostDestroyed(GameObject ghost)
    {
        if (activeGhosts.Contains(ghost))
        {
            activeGhosts.Remove(ghost);
        }
    }

    void CheckWaveCompletion()
    {
        activeGhosts.RemoveAll(ghost => ghost == null);

        if (activeGhosts.Count == 0 && !IsGameOver && waveSpawned)
        {
            RoundNumber++;
            waveSpawned = false;
            StartCoroutine(StartNextRound());
        }
    }

    IEnumerator StartNextRound()
    {
        ShowOverlay($"ROUND {RoundNumber}");
        yield return new WaitForSeconds(.5f);
        HideOverlay();
        SpawnWave();
    }

    public void AddScore(int amount)
    {
        if (amount <= 0) return;
        CurrentScore += amount;
        UpdateScoreUI();
    }

    void UpdateScoreUI()
    {
        if (scoreText != null) scoreText.text = $"{CurrentScore}";
    }

    void UpdateRoundUI()
    {
        if (roundText != null) roundText.text = $"ROUND : {RoundNumber}";
    }

    void UpdateBulletsUI()
    {
        if (bulletsText != null) bulletsText.text = $"BULLETS : {BulletCount}";
    }

    void UpdateLivesUI()
    {
        foreach (Transform child in livesContainer) Destroy(child.gameObject);
        for (int i = 0; i < CurrentLives; i++) Instantiate(lifeIconPrefab, livesContainer);
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
        AudioManager.I?.PlaySfx(SfxEvent.Hit, gameObject);
        if (IsGameOver || PacStudentControllerL2.I.IsDead) return;
        CurrentLives--;
        UpdateLivesUI();
        if (CurrentLives > 0) StartCoroutine(InvulnerableRoutine());
        else StartCoroutine(GameOverRoutine());
    }

    public void GetLife()
    {
        if (CurrentLives >= startingLives) AddScore(30);
        else
        {
            CurrentLives++;
            UpdateLivesUI();
        }
    }

    public void getBullet()
    {
        BulletCount += 1 + (int)PlayerPrefs.GetFloat("Upgrade_BC_Value", 0);
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
        if (PacStudentControllerL2.I == null) yield break;
        PacStudentControllerL2.I.SetInvulnerable(true);
        if (PacStudentAnimDriverL2.I != null)
            PacStudentAnimDriverL2.I.StartBlinking();
        yield return new WaitForSeconds(3f);
        PacStudentControllerL2.I.SetInvulnerable(false);
        if (PacStudentAnimDriverL2.I != null)
            PacStudentAnimDriverL2.I.StopBlinking();
    }

    IEnumerator GameOverRoutine()
    {
        IsGameOver = true;
        timerRunning = false;
        FreezeAllCharacters();
        if (PacStudentControllerL2.I != null) PacStudentControllerL2.I.StopMovement();
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
        int bestRound = PlayerPrefs.GetInt($"L2HighRound", 0);
        bool isBetter = RoundNumber > bestRound || (RoundNumber == bestRound && timer < bestTime);
        if (isBetter)
        {
            PlayerPrefs.SetInt($"L2HighRound", RoundNumber);
            PlayerPrefs.SetFloat($"L2BestTime", timer);
            PlayerPrefs.Save();
        }
    }

    void SaveMoney()
    {
        float playerMoney = PlayerPrefs.GetFloat($"PlayerMoney", 0);
        playerMoney += CurrentScore / 10;
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
        if (PacStudentControllerL2.I != null) PacStudentControllerL2.I.StopMovement();
        foreach (GhostStateManagerL2 g in FindObjectsByType<GhostStateManagerL2>(FindObjectsSortMode.None))
            g.StopAllMovement();
    }

    Vector3 GetTilemapCenter(Tilemap tilemap)
    {
        if (tilemap == null) return Vector3.zero;
        foreach (Vector3Int pos in tilemap.cellBounds.allPositionsWithin)
            if (tilemap.HasTile(pos)) return tilemap.GetCellCenterWorld(pos);
        return Vector3.zero;
    }
}
