using UnityEngine;
using System.Collections;

[RequireComponent(typeof(GhostVisuals))]
public class GhostStateManager : MonoBehaviour
{
    public enum GhostState { Normal, Scared, Recovering, Dead }

    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float respawnDelay = 3f;
    [SerializeField] private int ghostKillPoints = 300;

    private GhostVisuals visuals;
    private Coroutine deadRoutine;
    public GhostState CurrentState { get; private set; } = GhostState.Normal;

    void Awake()
    {
        visuals = GetComponent<GhostVisuals>();
    }

    public void EnterNormal()
    {
        CurrentState = GhostState.Normal;
        visuals.EnterNormal();
    }

    public void EnterScared()
    {
        if (CurrentState == GhostState.Dead) return;
        CurrentState = GhostState.Scared;
        visuals.EnterFrightened(true);
    }

    public void EnterRecovering()
    {
        if (CurrentState == GhostState.Dead) return;
        CurrentState = GhostState.Recovering;
        visuals.EnterFrightened(false);
    }

    public void EnterDead()
    {
        if (CurrentState == GhostState.Dead) return;

        CurrentState = GhostState.Dead;
        visuals.EnterDead(true);
        LevelManager.I?.AddScore(ghostKillPoints);
        GameManager.I?.SetState(GameState.Dying);

        if (deadRoutine != null)
            StopCoroutine(deadRoutine);
        deadRoutine = StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        if (LevelManager.I != null && LevelManager.I.IsGhostsScared)
            EnterScared();
        else
            EnterNormal();

        if (spawnPoint)
            transform.position = spawnPoint.position;

        deadRoutine = null;
    }

    public void ResetGhost()
    {
        StopAllCoroutines();
        CurrentState = GhostState.Normal;
        visuals.EnterNormal();
        if (spawnPoint)
            transform.position = spawnPoint.position;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (CurrentState == GhostState.Scared || CurrentState == GhostState.Recovering)
        {
            EnterDead();
        }
        else if (CurrentState == GhostState.Normal)
        {
            GameManager.I?.SetState(GameState.Dying);
            LevelManager.I?.LoseLife();
        }
    }
}
