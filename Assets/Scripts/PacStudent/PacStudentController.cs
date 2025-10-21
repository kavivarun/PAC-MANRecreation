using UnityEngine;

[RequireComponent(typeof(PacStudentAnimDriver))]
public class PacStudentController : MonoBehaviour
{
    public static PacStudentController I { get; private set; }

    public float moveSpeed = 6f;
    public float cellSize = 1f;
    public float snapEps = 0.01f;
    public float teleportCooldown = 0.2f;

    private Vector2Int gridPos;
    private Vector2Int lastInput = Vector2Int.zero;
    private Vector2Int currentInput = Vector2Int.zero;

    private Tweener tweener;
    private PacStudentAnimDriver animDriver;

    private bool hasPendingTeleport;
    private Vector2Int pendingTeleportDest;
    private float lastTeleportTime = -999f;

    [SerializeField] private WallTilemapController wallTilemap;
    private Vector2Int? lastWallHitDir = null;
    private Vector2Int facingDir = Vector2Int.right;

    private Vector3 spawnPosition;

    void Awake()
    {
        I = this;
        tweener = Tweener.FindFirstObjectByType<Tweener>();
        animDriver = GetComponent<PacStudentAnimDriver>();
    }

    void Start()
    {
        var level = TilemapLevel.I;
        gridPos = level.WorldToGrid(transform.position);
        transform.position = level.GridToWorld(gridPos);
        spawnPosition = transform.position;
        SetFacing(Vector2Int.right);
        animDriver.StopAnimation();
    }

    void Update()
    {
        if (animDriver.IsDead) return;

        ReadInput();
        var level = TilemapLevel.I;

        if (!tweener.TweenExists(transform))
        {
            transform.position = level.GridToWorld(gridPos);

            if (Time.time - lastTeleportTime > teleportCooldown)
            {
                if ((level.GetTile(gridPos) & TileFlags.Teleporter) != 0 &&
                    level.TryTeleport(gridPos, out var dest))
                {
                    gridPos = dest;
                    transform.position = level.GridToWorld(gridPos);
                    lastTeleportTime = Time.time;
                }
            }

            if (hasPendingTeleport)
            {
                hasPendingTeleport = false;
                gridPos = pendingTeleportDest;
                transform.position = level.GridToWorld(gridPos);
                if (TryBeginMove(currentInput)) return;
            }

            if (lastInput != Vector2Int.zero && TryBeginMove(lastInput)) return;
            if (currentInput != Vector2Int.zero && TryBeginMove(currentInput)) return;

            animDriver.StopAnimation();
            currentInput = Vector2Int.zero;
        }
        else
        {
            animDriver.StartAnimation();
        }
    }

    private void ReadInput()
    {
        Vector2Int newInput = Vector2Int.zero;

        if (Input.GetKeyDown(KeyCode.W)) newInput = Vector2Int.up;
        else if (Input.GetKeyDown(KeyCode.S)) newInput = Vector2Int.down;
        else if (Input.GetKeyDown(KeyCode.A)) newInput = Vector2Int.left;
        else if (Input.GetKeyDown(KeyCode.D)) newInput = Vector2Int.right;

        if (newInput != Vector2Int.zero)
            lastInput = newInput;
    }

    private bool TryBeginMove(Vector2Int dir)
    {
        if (animDriver.IsDead) return false;
        if (dir == Vector2Int.zero) return false;

        var level = TilemapLevel.I;
        Vector2Int next = gridPos + dir;
        bool isWall = !level.IsWalkableForPlayer(gridPos, next);

        if (isWall)
        {
            if (lastWallHitDir != dir && dir == facingDir)
            {
                Vector3 hitPos = level.GridToWorld(next) - new Vector3(dir.x * 0.25f, dir.y * 0.25f, 0f);
                if (wallTilemap != null)
                    wallTilemap.PlayWallHit(hitPos);
                AudioManager.I?.PlaySfx(SfxEvent.WallHit, gameObject);

                Vector3 wobbleStart = transform.position;
                Vector3 wobbleEnd = wobbleStart + new Vector3(dir.x * 0.2f, dir.y * 0.2f, 0f);
                float wobbleTime = 0.05f;
                if (!tweener.TweenExists(transform))
                {
                    tweener.AddTween(transform, wobbleStart, wobbleEnd, wobbleTime);
                    tweener.AddTween(transform, wobbleEnd, wobbleStart, wobbleTime);
                }

                lastWallHitDir = dir;
            }
            return false;
        }

        lastWallHitDir = null;

        Vector3 a = level.GridToWorld(gridPos);
        Vector3 b = level.GridToWorld(next);
        float duration = (b - a).magnitude / (moveSpeed * cellSize);

        SetFacing(dir);
        if (tweener.AddTween(transform, a, b, duration))
        {
            currentInput = dir;
            gridPos = next;
            animDriver.StartAnimation();
            return true;
        }
        return false;
    }

    private void SetFacing(Vector2Int dir)
    {
        facingDir = dir;
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            animDriver.SetFacing(dir.x > 0 ? PacStudentAnimDriver.Dir.Right : PacStudentAnimDriver.Dir.Left);
        else
            animDriver.SetFacing(dir.y > 0 ? PacStudentAnimDriver.Dir.Up : PacStudentAnimDriver.Dir.Down);
    }

    public void StopMovement()
    {
        if (tweener != null) tweener.CancelTween(transform);
        lastInput = Vector2Int.zero;
        currentInput = Vector2Int.zero;
    }

    public void Respawn()
    {
        tweener.CancelTween(transform);
        gridPos = TilemapLevel.I.WorldToGrid(spawnPosition);
        transform.position = spawnPosition;
        lastInput = Vector2Int.zero;
        currentInput = Vector2Int.zero;
        hasPendingTeleport = false;
        lastWallHitDir = null;
        facingDir = Vector2Int.right;
        animDriver.ClearDeath();
        animDriver.StopAnimation();
        animDriver.SetFacing(PacStudentAnimDriver.Dir.Right);
        lastTeleportTime = -999f;
    }
}
