using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GhostAiController : MonoBehaviour
{
    public enum MovementStyle { AwayFromPac, TowardPac, Random, ClockwisePerimeter }

    [SerializeField] MovementStyle movementStyle = MovementStyle.Random;
    [SerializeField] float tickDelay = 0.01f;

    Tweener tweener;
    GhostStateManager gsm;
    GhostVisuals visuals;
    Vector2Int gridPos;
    Vector2Int lastDir = Vector2Int.right;
    Queue<Vector2Int> pathQueue = new Queue<Vector2Int>();
    bool onPerimeter;
    float nextTick;
    TilemapLevel level;
    static readonly Vector2Int[] dirs = { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down };

    void Awake()
    {
        tweener = FindFirstObjectByType<Tweener>();
        gsm = GetComponent<GhostStateManager>();
        visuals = GetComponent<GhostVisuals>();
        level = TilemapLevel.I;
    }

    void Start()
    {
        gridPos = level.WorldToGrid(transform.position);
        onPerimeter = IsPerimeter(gridPos);
        UpdateVisualDir(lastDir);
    }

    void Update()
    {
        if (gsm != null && gsm.IsFrozen) return;
        
        if (gsm.MovementOverrideActive) return;
        if (Time.time < nextTick) return;
        nextTick = Time.time + tickDelay;

        if (!tweener.TweenExists(transform)) gridPos = level.WorldToGrid(transform.position);

        if (gsm.CurrentState == GhostStateManager.GhostState.Dead) return;

        if (tweener.TweenExists(transform)) return;

        var style = EffectiveStyle();
        if (style == MovementStyle.ClockwisePerimeter)
        {
            if (!onPerimeter)
            {
                if (pathQueue.Count == 0) BuildPathToPerimeter();
                if (pathQueue.Count > 0) StepPath();
                if (pathQueue.Count == 0) onPerimeter = IsPerimeter(level.WorldToGrid(transform.position));
                return;
            }
            var dirCW = NextClockwiseDir();
            TryStep(dirCW);
            return;
        }

        var candidates = ValidNeighbors(gridPos);
        if (candidates.Count == 0) return;

        Vector3 pacPos = PacStudentController.I ? PacStudentController.I.transform.position : transform.position;
        Vector2 pac = new Vector2(pacPos.x, pacPos.y);
        Vector2 curWorld = level.GridToWorld(gridPos);
        float curDist = Vector2.Distance(curWorld, pac);

        if (style == MovementStyle.Random)
        {
            TryStep(candidates[UnityEngine.Random.Range(0, candidates.Count)]);
            return;
        }

        List<Vector2Int> filtered = new List<Vector2Int>();
        foreach (var d in candidates)
        {
            var n = gridPos + d;
            float nd = Vector2.Distance(level.GridToWorld(n), pac);
            if (style == MovementStyle.AwayFromPac && nd >= curDist) filtered.Add(d);
            else if (style == MovementStyle.TowardPac && nd <= curDist) filtered.Add(d);
        }
        if (filtered.Count == 0) filtered = candidates;
        TryStep(filtered[UnityEngine.Random.Range(0, filtered.Count)]);
    }

    MovementStyle EffectiveStyle()
    {
        if (gsm.CurrentState == GhostStateManager.GhostState.Scared) return MovementStyle.AwayFromPac;
        if (gsm.CurrentState == GhostStateManager.GhostState.Recovering) return MovementStyle.AwayFromPac;
        return movementStyle;
    }

    void StepPath()
    {
        if (gsm != null && gsm.IsFrozen) return;
        if (pathQueue.Count == 0) return;
        var next = pathQueue.Dequeue();
        var dir = next - gridPos;
        TryStep(dir);
        if (pathQueue.Count == 0) onPerimeter = IsPerimeter(next);
    }

    void TryStep(Vector2Int dir)
    {
        if (gsm != null && gsm.IsFrozen) return;
        var next = gridPos + dir;
        if (!IsWalkable(next)) return;
        float speed = GetSpeed();
        float dur = Mathf.Max(0.0001f, TilemapLevel.I.cellSize / speed);
        Vector3 a = transform.position;
        Vector3 b = level.GridToWorld(next);
        if (tweener.AddTween(transform, a, b, dur))
        {
            gridPos = next;
            lastDir = dir;
            UpdateVisualDir(dir);
            if (level.TryTeleport(gridPos, out var tp))
            {
                tweener.CancelTween(transform);
                transform.position = level.GridToWorld(tp);
                gridPos = tp;
            }
        }
    }

    float GetSpeed()
    {
        float pac = PacStudentController.I ? PacStudentController.I.moveSpeed : 6f;
        float normal = 0.9f * pac;
        if (gsm.CurrentState == GhostStateManager.GhostState.Normal) return normal;
        if (gsm.CurrentState == GhostStateManager.GhostState.Scared) return 0.5f * normal;
        if (gsm.CurrentState == GhostStateManager.GhostState.Recovering) return 0.5f * normal;
        if (gsm.CurrentState == GhostStateManager.GhostState.Dead) return 0.5f * normal;
        return normal;
    }

    List<Vector2Int> ValidNeighbors(Vector2Int c)
    {
        List<Vector2Int> list = new List<Vector2Int>(4);
        for (int i = 0; i < 4; i++)
        {
            var d = dirs[i];
            var n = c + d;
            if (IsWalkable(n)) list.Add(d);
        }
        return list;
    }

    bool IsWalkable(Vector2Int c)
    {
        if (!level.InBounds(c)) return false;
        var v3 = new Vector3Int(c.x, c.y, 0);
        if (level.walls && level.walls.HasTile(v3)) return false;
        if (level.floor && level.floor.HasTile(v3)) return true;
        if (level.ghostHome && level.ghostHome.HasTile(v3)) return false;
        if (level.ghostGate && level.ghostGate.HasTile(v3)) return false;
        if (level.teleporters && level.teleporters.HasTile(v3)) return false;
        return false;
    }

    void BuildPathToPerimeter()
    {
        HashSet<Vector2Int> goals = new HashSet<Vector2Int>();
        BoundsInt b = level.walls.cellBounds;
        for (int x = b.xMin; x < b.xMax; x++)
            for (int y = b.yMin; y < b.yMax; y++)
            {
                var c = new Vector2Int(x, y);
                if (!IsWalkable(c)) continue;
                if (IsPerimeter(c)) goals.Add(c);
            }
        var path = BFSNearest(gridPos, goals);
        pathQueue.Clear();
        if (path != null) for (int i = 1; i < path.Count; i++) pathQueue.Enqueue(path[i]);
    }

    bool IsPerimeter(Vector2Int c)
    {
        if (!IsWalkable(c)) return false;
        for (int i = 0; i < 4; i++)
        {
            var n = c + dirs[i];
            if (!level.InBounds(n)) return true;
            var v3 = new Vector3Int(n.x, n.y, 0);
            if (level.walls && level.walls.HasTile(v3)) return true;
            if (!IsWalkable(n)) return true;
        }
        return false;
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

    Vector2Int NextClockwiseDir()
    {
        Vector2Int right = RightOf(lastDir);
        Vector2Int straight = lastDir;
        Vector2Int left = LeftOf(lastDir);
        Vector2Int back = -lastDir;
        if (IsWalkable(gridPos + right)) return right;
        if (IsWalkable(gridPos + straight)) return straight;
        if (IsWalkable(gridPos + left)) return left;
        return back;
    }

    Vector2Int RightOf(Vector2Int d)
    {
        if (d == Vector2Int.right) return Vector2Int.down;
        if (d == Vector2Int.up) return Vector2Int.right;
        if (d == Vector2Int.left) return Vector2Int.up;
        return Vector2Int.left;
    }

    Vector2Int LeftOf(Vector2Int d)
    {
        if (d == Vector2Int.right) return Vector2Int.up;
        if (d == Vector2Int.up) return Vector2Int.left;
        if (d == Vector2Int.left) return Vector2Int.down;
        return Vector2Int.right;
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

    public void StopAiMovement()
    {
        if (tweener != null)
        {
            tweener.CancelTween(transform);
        }
        pathQueue.Clear();
        onPerimeter = false;
        nextTick = Time.time + tickDelay;
    }

    public void ResumeAiMovement()
    {
        gridPos = level.WorldToGrid(transform.position);
        onPerimeter = IsPerimeter(gridPos);
        nextTick = Time.time + tickDelay;
    }
}
