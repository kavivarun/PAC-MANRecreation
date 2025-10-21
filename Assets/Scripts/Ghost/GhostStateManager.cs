using UnityEngine;
using System.Collections;

[RequireComponent(typeof(GhostVisuals))]
public class GhostStateManager : MonoBehaviour
{
    public enum GhostState { Normal, Scared, Recovering, Dead }

    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float ghostDeadDuration = 3f;
    [SerializeField] private int ghostKillPoints = 300;

    private GhostVisuals visuals;
    private Coroutine deadRoutine;
    private Rigidbody2D rb;
    private bool isFrozen;

    public GhostState CurrentState { get; private set; } = GhostState.Normal;

    void Awake()
    {
        visuals = GetComponent<GhostVisuals>();
        rb = GetComponent<Rigidbody2D>();
    }

    public void EnterNormal()
    {
        if (isFrozen) return;
        CurrentState = GhostState.Normal;
        visuals.EnterNormal();
    }

    public void EnterScared()
    {
        if (isFrozen || CurrentState == GhostState.Dead) return;
        CurrentState = GhostState.Scared;
        visuals.EnterFrightened(true);
    }

    public void EnterRecovering()
    {
        if (isFrozen || CurrentState == GhostState.Dead) return;
        CurrentState = GhostState.Recovering;
        visuals.EnterRecovering();
    }

    public void EnterDead()
    {
        if (isFrozen || CurrentState == GhostState.Dead) return;
        CurrentState = GhostState.Dead;
        visuals.EnterDead(true);
        LevelManager.I?.AddScore(ghostKillPoints);
        GameManager.I?.SetState(GameState.AlienDead);
        if (deadRoutine != null)
            StopCoroutine(deadRoutine);
        deadRoutine = StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(ghostDeadDuration);
        if (LevelManager.I != null)
        {
            float remain = LevelManager.I.ScaredTimeRemaining;
            if (remain > 3f)
            {
                CurrentState = GhostState.Scared;
                visuals.EnterFrightened(true);
                GameManager.I?.SetState(GameState.PowerMode);
            }
            else if (remain > 0f)
            {
                CurrentState = GhostState.Recovering;
                visuals.EnterFrightened(true);
                GameManager.I?.SetState(GameState.PowerMode);
            }
            else
            {
                EnterNormal();
                GameManager.I?.SetState(GameState.Playing);
            }
        }
        else
        {
            EnterNormal();
        }

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

    public void StopAllMovement()
    {
        isFrozen = true;
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.simulated = false;
        }
    }

    public void ResumeMovement()
    {
        isFrozen = false;
        if (rb != null)
            rb.simulated = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isFrozen) return;
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
