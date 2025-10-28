using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GhostAiControllerL2 : MonoBehaviour
{
    public enum MovementStyle { AwayFromPac, TowardPac, Random, ClockwisePerimeter, RandomTeleport, SpeedBoost }

    [SerializeField] float tickDelay = 0.01f;

    MovementStyle movementStyle = MovementStyle.Random;
    Tweener tweener;
    GhostStateManagerL2 gsm;
    GhostVisuals visuals;
    Vector2Int gridPos;
    Vector2Int lastDir = Vector2Int.right;
    Queue<Vector2Int> pathQueue = new Queue<Vector2Int>();
    bool onPerimeter;
    float nextTick;
    TilemapLevel2 level;
    static readonly Vector2Int[] dirs = { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down };

    float lastTeleportTime;
    float teleportCooldown = 10f;

    float lastSpeedBoostTime;
    float speedBoostCooldown = 20f;
    float speedBoostDuration = 5f;
    bool isSpeedBoosted;
    float speedBoostEndTime;

    void Awake()
    {
        tweener = FindFirstObjectByType<Tweener>();
        gsm = GetComponent<GhostStateManagerL2>();
        visuals = GetComponent<GhostVisuals>();
        level = TilemapLevel2.I;
    }

    void Start()
    {
        gridPos = level.WorldToGrid(transform.position);
        onPerimeter = level.IsOutsidePerimeter(gridPos);
        UpdateVisualDir(lastDir);
        Array movementStyles = Enum.GetValues(typeof(MovementStyle));
        movementStyle = (MovementStyle)movementStyles.GetValue(UnityEngine.Random.Range(0, movementStyles.Length));
        
        lastTeleportTime = Time.time;
        lastSpeedBoostTime = Time.time;
    }

    void Update()
    {
        if (gsm != null && gsm.IsFrozen) return;
        if (gsm.MovementOverrideActive) return;

        if (movementStyle == MovementStyle.SpeedBoost)
        {
            if (isSpeedBoosted && Time.time >= speedBoostEndTime)
            {
                isSpeedBoosted = false;
            }
        }

        if (Time.time < nextTick) return;
        nextTick = Time.time + tickDelay;

        if (!tweener.TweenExists(transform))
            gridPos = level.WorldToGrid(transform.position);

        if (gsm.CurrentState == GhostStateManagerL2.GhostState2.Dead) return;
        if (tweener.TweenExists(transform)) return;

        var style = movementStyle;

        switch (style)
        {
            case MovementStyle.ClockwisePerimeter:
                onPerimeter = level.IsOutsidePerimeter(gridPos);
                HandleClockwisePerimeter();
                break;

            case MovementStyle.Random:
                HandleRandomMovement();
                break;

            case MovementStyle.AwayFromPac:
                pathQueue.Clear();
                HandleDirectionalMovement(awayFromPac: true);
                break;

            case MovementStyle.TowardPac:
                HandleDirectionalMovement(awayFromPac: false);
                break;

            case MovementStyle.RandomTeleport:
                HandleRandomTeleportMovement();
                break;

            case MovementStyle.SpeedBoost:
                HandleSpeedBoostMovement();
                break;

            default:
                HandleRandomMovement();
                break;
        }
    }

    void HandleRandomTeleportMovement()
    {
        if (Time.time - lastTeleportTime >= teleportCooldown)
        {
            TeleportToRandomLocation();
            lastTeleportTime = Time.time;
        }
        else
        {
            HandleRandomMovement();
        }
    }

    void HandleSpeedBoostMovement()
    {
        if (!isSpeedBoosted && Time.time - lastSpeedBoostTime >= speedBoostCooldown)
        {
            isSpeedBoosted = true;
            speedBoostEndTime = Time.time + speedBoostDuration;
            lastSpeedBoostTime = Time.time;
        }
        
        HandleRandomMovement();
    }

    void TeleportToRandomLocation()
    {
        List<Vector2Int> validPositions = new List<Vector2Int>();
        var bounds = level.Bounds;
        
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (IsWalkable(pos))
                {
                    validPositions.Add(pos);
                }
            }
        }

        if (validPositions.Count > 0)
        {
            Vector3 oldPosition = transform.position;
            Vector2Int randomPos = validPositions[UnityEngine.Random.Range(0, validPositions.Count)];
            Vector3 newPosition = level.GridToWorld(randomPos);
    
            PlayTeleportEffect(oldPosition);
            
            tweener.CancelTween(transform);
            transform.position = newPosition;
            gridPos = randomPos;
            
            PlayTeleportEffect(newPosition);
        }
    }

    void PlayTeleportEffect(Vector3 position)
    {
        if (level.TeleportEffectPrefab != null)
        {
            AudioManager.I?.PlaySfx(SfxEvent.PowerPickup, gameObject);
            GameObject effect = Instantiate(level.TeleportEffectPrefab, position, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }

    void HandleClockwisePerimeter()
    {
        if (!onPerimeter)
        {
            if (pathQueue.Count == 0) BuildPathToPerimeter();
            if (pathQueue.Count > 0) StepPath();
            if (pathQueue.Count == 0)
                onPerimeter = level.IsOutsidePerimeter(level.WorldToGrid(transform.position));
            return;
        }

        var dirCW = NextClockwiseDirPerimeter();
        TryStep(dirCW);
    }

    void HandleRandomMovement()
    {
        var candidates = ValidNeighbors(gridPos);
        if (candidates.Count == 0) return;

        var dir = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        TryStep(dir);
    }

    void HandleDirectionalMovement(bool awayFromPac)
    {
        var candidates = ValidNeighbors(gridPos);
        if (candidates.Count == 0) return;

        Vector3 pacPos = PacStudentControllerL2.I ? PacStudentControllerL2.I.transform.position : transform.position;
        Vector2 pac = new Vector2(pacPos.x, pacPos.y);
        Vector2 curWorld = level.GridToWorld(gridPos);
        float curDist = Vector2.Distance(curWorld, pac);

        List<Vector2Int> filtered = new List<Vector2Int>();
        foreach (var d in candidates)
        {
            var n = gridPos + d;
            float nd = Vector2.Distance(level.GridToWorld(n), pac);

            if (awayFromPac && nd >= curDist)
                filtered.Add(d);
            else if (!awayFromPac && nd <= curDist)
                filtered.Add(d);
        }

        if (filtered.Count == 0)
            filtered = candidates;

        var chosen = filtered[UnityEngine.Random.Range(0, filtered.Count)];
        TryStep(chosen);
    }

    void StepPath()
    {
        if (gsm != null && gsm.IsFrozen) return;
        if (pathQueue.Count == 0) return;
        var next = pathQueue.Dequeue();
        var dir = next - gridPos;
        TryStep(dir);
        if (pathQueue.Count == 0) onPerimeter = level.IsOutsidePerimeter(next);
    }

    void TryStep(Vector2Int dir)
    {
        if (gsm != null && gsm.IsFrozen) return;
        var next = gridPos + dir;
        if (!IsWalkable(next)) return;

        float speed = GetSpeed();
        Vector3 a = level.GridToWorld(gridPos);
        Vector3 b = level.GridToWorld(next);
        float dur = (b - a).magnitude / (speed * TilemapLevel2.I.cellSize);

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
        float pac = PacStudentControllerL2.I ? PacStudentControllerL2.I.moveSpeed : 6f;
        float normal = 0.9f * pac;
        
        if (movementStyle == MovementStyle.SpeedBoost && isSpeedBoosted)
        {
            normal *= 2f;
        }
        
        if (gsm.CurrentState == GhostStateManagerL2.GhostState2.Normal) return normal;
        if (gsm.CurrentState == GhostStateManagerL2.GhostState2.Dead) return 0.5f * normal;
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

        if (list.Count > 1)
        {
            var back = -lastDir;
            for (int i = list.Count - 1; i >= 0; i--)
                if (list[i] == back) list.RemoveAt(i);
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
        var b = level.Bounds;
        for (int x = b.xMin; x < b.xMax; x++)
            for (int y = b.yMin; y < b.yMax; y++)
            {
                var c = new Vector2Int(x, y);
                if (!IsWalkable(c)) continue;
                if (level.IsOutsidePerimeter(c)) goals.Add(c);
            }
        var path = BFSNearest(gridPos, goals);
        pathQueue.Clear();
        if (path != null) for (int i = 1; i < path.Count; i++) pathQueue.Enqueue(path[i]);
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

    Vector2Int NextClockwiseDirPerimeter()
    {
        Vector2Int right = RightOf(lastDir);
        Vector2Int straight = lastDir;
        Vector2Int left = LeftOf(lastDir);
        Vector2Int back = -lastDir;

        var r = gridPos + right;
        var s = gridPos + straight;
        var l = gridPos + left;
        var b = gridPos + back;

        if (IsWalkable(r) && level.IsOutsidePerimeter(r)) return right;
        if (IsWalkable(s) && level.IsOutsidePerimeter(s)) return straight;
        if (IsWalkable(l) && level.IsOutsidePerimeter(l)) return left;
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
        onPerimeter = level.IsOutsidePerimeter(gridPos);
        nextTick = Time.time + tickDelay;
    }

    public void ResetMovementForNewState()
    {
        pathQueue.Clear();
        onPerimeter = false;
        nextTick = Time.time + tickDelay;
        gridPos = level.WorldToGrid(transform.position);
        tweener.CancelTween(transform);
        isSpeedBoosted = false;
        lastTeleportTime = Time.time;
        lastSpeedBoostTime = Time.time;
    }
}
