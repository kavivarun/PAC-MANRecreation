using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(GhostVisuals))]
public class GhostStateManager : MonoBehaviour
{
    public enum GhostState { Spawn, Normal, Scared, Recovering, Dead }

    [Header("Spawn/Exit Tilemaps")]
    [SerializeField] private string spawnTilemapName;
    [SerializeField] private string exitTilemapName;
    [SerializeField] private bool startInSpawn = true;

    [Header("Timing & Scoring")]
    [SerializeField] private float ghostDeadDuration = 3f;
    [SerializeField] private int ghostKillPoints = 300;

    GhostVisuals visuals;
    Tweener tweener;
    TilemapLevel level;
    Tilemap spawnTilemap;
    Tilemap exitTilemap;

    enum NavMode { None, ExitFromSpawnInitial, ReturnToSpawn, ExitAfterRespawn }
    NavMode navMode = NavMode.None;

    Queue<Vector2Int> navQueue = new Queue<Vector2Int>();
    Vector2Int gridPos;
    Vector2Int lastDir = Vector2Int.right;
    static readonly Vector2Int[] dirs = { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down };

    Coroutine deadRoutine;
    bool isFrozen;
    bool postSpawnResumePending;
    GhostState queuedResumeState = GhostState.Normal;

    public GhostState CurrentState { get; private set; } = GhostState.Normal;
    public bool MovementOverrideActive => navMode != NavMode.None || tweener.TweenExists(transform);
    public bool PostSpawnResumePending => postSpawnResumePending;
    public bool IsFrozen => isFrozen;

    void Awake()
    {
        visuals = GetComponent<GhostVisuals>();
        tweener = FindFirstObjectByType<Tweener>();
        level = TilemapLevel.I;
        if (level != null)
        {
            var maps = level.GetComponentsInChildren<Tilemap>(true);
            foreach (var tm in maps)
            {
                if (!string.IsNullOrEmpty(spawnTilemapName) && tm.name == spawnTilemapName) spawnTilemap = tm;
                if (!string.IsNullOrEmpty(exitTilemapName) && tm.name == exitTilemapName) exitTilemap = tm;
            }
        }
    }

    void Start()
    {
        gridPos = level.WorldToGrid(transform.position);
        if (startInSpawn && exitTilemap != null)
        {
            CurrentState = GhostState.Spawn;
            BeginExitFromSpawnInitial();
        }
        else
        {
            EnterNormal();
        }
    }

    void Update()
    {
        if (isFrozen) return;

        if (!tweener.TweenExists(transform))
        {
            gridPos = level.WorldToGrid(transform.position);

            if (navMode == NavMode.ReturnToSpawn)
            {
                navMode = NavMode.ExitAfterRespawn;
                BuildPathToAny(exitTilemap);
            }

            if (navMode == NavMode.ExitFromSpawnInitial || navMode == NavMode.ExitAfterRespawn)
            {
                if (navQueue.Count > 0)
                {
                    StepNav();
                }
                else
                {
                    if (navMode == NavMode.ExitFromSpawnInitial)
                    {
                        navMode = NavMode.None;
                        EnterNormalNoState();
                    }
                    else
                    {
                        //navMode = NavMode.None;
                        //NotifyExitedSpawn();
                    }
                }
            }
        }
    }

    public void EnterNormal()
    {
        if (isFrozen) return;
        CurrentState = GhostState.Normal;
        visuals.EnterNormal();
        GameManager.I?.SetState(GameState.Playing);
    }

    public void EnterNormalNoState()
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
        GameManager.I?.SetState(GameState.PowerMode);
    }

    public void EnterRecovering()
    {
        if (isFrozen || CurrentState == GhostState.Dead) return;
        CurrentState = GhostState.Recovering;
        visuals.EnterRecovering();
        GameManager.I?.SetState(GameState.PowerMode);
    }

    public void EnterDead()
    {
        if (isFrozen || CurrentState == GhostState.Dead) return;
        CurrentState = GhostState.Dead;
        visuals.EnterDead(true);
        LevelManager.I?.AddScore(ghostKillPoints);
        GameManager.I?.SetState(GameState.AlienDead);
        if (deadRoutine != null) StopCoroutine(deadRoutine);
        navMode = NavMode.None;
        navQueue.Clear();
        postSpawnResumePending = false;
        deadRoutine = StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        if (spawnTilemap != null)
        {
            GameState gameState;
            Vector2Int spawnCell = ResolveNearest(spawnTilemap, level.WorldToGrid(transform.position));
            Vector3 target = level.GridToWorld(spawnCell);
            float dur = ghostDeadDuration;
            tweener.CancelTween(transform);
            tweener.AddTween(transform, transform.position, target, dur);
            yield return tweener.WaitUntilTweenDone(transform);
            AudioManager.I?.UnlockDeadAudio();
            float remain = LevelManager.I != null ? LevelManager.I.ScaredTimeRemaining : 0f;
            if (remain > 3f)
            {
                CurrentState = GhostState.Scared;
                visuals.EnterFrightened(true);
                gameState = GameState.PowerMode;
            }
            else if (remain > 0f)
            {
                CurrentState = GhostState.Recovering;
                visuals.EnterRecovering();
                gameState = GameState.PowerMode;
            }
            else
            {
                CurrentState = GhostState.Normal;
                visuals.EnterNormal();
                gameState = GameState.Playing;
            }
            GameManager.I.SetState(gameState);
            navMode = NavMode.ReturnToSpawn;
        }
        else
        {
            postSpawnResumePending = true;
            NotifyExitedSpawn();
        }

        deadRoutine = null;
        yield return null;
    }

    public void NotifyExitedSpawn()
    {
        float remain = LevelManager.I != null ? LevelManager.I.ScaredTimeRemaining : 0f;
        if (remain > 3f)
        {
            CurrentState = GhostState.Scared;
            visuals.EnterFrightened(true);
            GameManager.I?.SetState(GameState.PowerMode);
        }
        else if (remain > 0f)
        {
            CurrentState = GhostState.Recovering;
            visuals.EnterRecovering();
            GameManager.I?.SetState(GameState.PowerMode);
        }
        else
        {
            EnterNormalNoState();
        }
        postSpawnResumePending = false;
    }

    public void ResetGhost()
    {
        StopAllCoroutines();
        tweener?.CancelTween(transform);
        navQueue.Clear();
        visuals.EnterNormal();
        navMode = NavMode.None;
        isFrozen = false;
        postSpawnResumePending = false;
        queuedResumeState = GhostState.Normal;

        if (spawnTilemap != null)
        {
            var spawnCell = ResolveAny(spawnTilemap);
            transform.position = level.GridToWorld(spawnCell);
        }

        gridPos = level.WorldToGrid(transform.position);

        if (startInSpawn && exitTilemap != null)
        {
            CurrentState = GhostState.Spawn;
            BeginExitFromSpawnInitial();
        }
        else
        {
            EnterNormal();
        }
    }

    public void StopAllMovement()
    {
        isFrozen = true;
        if (tweener != null)
        {
            tweener.CancelTween(transform);
        }
        navQueue.Clear();
        navMode = NavMode.None;
        if (deadRoutine != null)
        {
            StopCoroutine(deadRoutine);
            deadRoutine = null;
        }
        postSpawnResumePending = false;
        var aiController = GetComponent<GhostAiController>();
        if (aiController != null)
        {
            aiController.StopAiMovement();
        }
    }

    public void ResumeMovement()
    {
        isFrozen = false;
        var aiController = GetComponent<GhostAiController>();
        if (aiController != null)
        {
            aiController.ResumeAiMovement();
        }
    }

    void BeginExitFromSpawnInitial()
    {
        navQueue.Clear();
        BuildPathToAny(exitTilemap);
        navMode = NavMode.ExitFromSpawnInitial;
    }

    void StepNav()
    {
        if (isFrozen) return;
        if (navQueue.Count == 0) return;
        var next = navQueue.Dequeue();
        var dir = next - gridPos;
        Vector3 a = transform.position;
        Vector3 b = level.GridToWorld(next);
        float speed = GetSpeedForState(CurrentState);
        float dur = Mathf.Max(0.0001f, TilemapLevel.I.cellSize / speed);
        if (tweener.AddTween(transform, a, b, dur))
        {
            gridPos = next;
            UpdateVisualDir(dir);
            if (level.TryTeleport(gridPos, out var tp))
            {
                tweener.CancelTween(transform);
                transform.position = level.GridToWorld(tp);
                gridPos = tp;
            }
        }
    }

    float GetSpeedForState(GhostState s)
    {
        float pac = PacStudentController.I ? PacStudentController.I.moveSpeed : 6f;
        float normal = 0.9f * pac;
        if (s == GhostState.Normal) return normal;
        if (s == GhostState.Scared) return 0.5f * normal;
        if (s == GhostState.Recovering) return 0.5f * normal;
        if (s == GhostState.Dead) return 0.5f * normal;
        return normal;
    }

    void BuildPathToAny(Tilemap tm)
    {
        navQueue.Clear();
        if (tm == null) return;
        List<Vector2Int> goals = new List<Vector2Int>();
        foreach (var pos in tm.cellBounds.allPositionsWithin)
            if (tm.HasTile(pos)) goals.Add(new Vector2Int(pos.x, pos.y));
        var path = BFSNearest(gridPos, goals);
        if (path != null) for (int i = 1; i < path.Count; i++) navQueue.Enqueue(path[i]);
    }

    Vector2Int ResolveAny(Tilemap tm)
    {
        foreach (var p in tm.cellBounds.allPositionsWithin)
            if (tm.HasTile(p)) return new Vector2Int(p.x, p.y);
        return level.WorldToGrid(transform.position);
    }

    Vector2Int ResolveNearest(Tilemap tm, Vector2Int from)
    {
        Vector2Int best = from;
        float bestDist = float.PositiveInfinity;
        foreach (var p in tm.cellBounds.allPositionsWithin)
        {
            if (!tm.HasTile(p)) continue;
            var c = new Vector2Int(p.x, p.y);
            float d = Vector2.Distance(level.GridToWorld(from), level.GridToWorld(c));
            if (d < bestDist) { bestDist = d; best = c; }
        }
        return best;
    }

    List<Vector2Int> BFSNearest(Vector2Int start, IEnumerable<Vector2Int> goals)
    {
        HashSet<Vector2Int> goalSet = new HashSet<Vector2Int>(goals);
        Queue<Vector2Int> q = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> prev = new Dictionary<Vector2Int, Vector2Int>();
        q.Enqueue(start);
        prev[start] = start;
        while (q.Count > 0)
        {
            var c = q.Dequeue();
            if (goalSet.Contains(c)) return Reconstruct(prev, start, c);
            for (int i = 0; i < 4; i++)
            {
                var n = c + dirs[i];
                if (!IsWalkable(n)) continue;
                if (prev.ContainsKey(n)) continue;
                prev[n] = c;
                q.Enqueue(n);
            }
        }
        return null;
    }

    List<Vector2Int> Reconstruct(Dictionary<Vector2Int, Vector2Int> prev, Vector2Int start, Vector2Int goal)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        var c = goal;
        while (true)
        {
            path.Add(c);
            if (c == start) break;
            c = prev[c];
        }
        path.Reverse();
        return path;
    }

    bool IsWalkable(Vector2Int c)
    {
        if (!level.InBounds(c)) return false;
        var v3 = new Vector3Int(c.x, c.y, 0);
        if (level.walls && level.walls.HasTile(v3)) return false;
        if (level.floor && level.floor.HasTile(v3)) return true;
        if (level.ghostHome && level.ghostHome.HasTile(v3)) return true;
        if (level.ghostGate && level.ghostGate.HasTile(v3)) return true;
        if (level.teleporters && level.teleporters.HasTile(v3)) return true;
        return false;
    }

    void UpdateVisualDir(Vector2Int d)
    {
        int idx = 0;
        if (d == Vector2Int.right) idx = 0;
        else if (d == Vector2Int.up) idx = 1;
        else if (d == Vector2Int.left) idx = 2;
        else idx = 3;
        visuals.SetDirection(idx);
    }
    void OnTriggerEnter2D(Collider2D other) 
    { 
        if (isFrozen) return;
        if (!other.CompareTag("Player")) 
            return; 
        if (CurrentState == GhostState.Scared || CurrentState == GhostState.Recovering)
        { 
            EnterDead(); 
        } else if (CurrentState == GhostState.Normal) 
        { 
            GameManager.I?.SetState(GameState.Dying);
            LevelManager.I?.LoseLife();
        } 
    }
}

