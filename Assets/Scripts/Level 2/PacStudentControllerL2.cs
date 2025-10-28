using UnityEngine;

[RequireComponent(typeof(PacStudentAnimDriverL2))]
public class PacStudentControllerL2 : MonoBehaviour
{
    public static PacStudentControllerL2 I { get; private set; }

    public float moveSpeed = 6f;
    public float cellSize = 1f;
    public float snapEps = 0.01f;
    public float teleportCooldown = 0.2f;
    public GameObject pelletPrefab;
    public float pelletSpeed = 12f;
    public float pelletDistance = 1000f;

    private Vector2Int gridPos;
    private Vector2Int lastInput = Vector2Int.zero;
    private Vector2Int currentInput = Vector2Int.zero;

    private Tweener tweener;
    private PacStudentAnimDriverL2 animDriver;

    private bool hasPendingTeleport;
    private Vector2Int pendingTeleportDest;
    private float lastTeleportTime = -999f;

    [SerializeField] private WallTilemapControllerL2 wallTilemap;
    private Vector2Int? lastWallHitDir = null;
    private Vector2Int facingDir = Vector2Int.right;

    // Invulnerability system
    private bool isInvulnerable = false;
    public bool IsInvulnerable => isInvulnerable;

    public bool IsDead => animDriver != null ? animDriver.IsDead : false;

    void Awake()
    {
        I = this;
        tweener = Tweener.FindFirstObjectByType<Tweener>();
        if (wallTilemap == null)
            wallTilemap = FindFirstObjectByType<WallTilemapControllerL2>();
        animDriver = GetComponent<PacStudentAnimDriverL2>();
    }

    void Start()
    {
        var level = TilemapLevel2.I;
        gridPos = level.WorldToGrid(transform.position);
        transform.position = level.GridToWorld(gridPos);
        SetFacing(Vector2Int.right);
        animDriver.StopAnimation();
    }

    void Update()
    {
        if (animDriver.IsDead || GameManager.I.CurrentState == GameState.LevelCleared) return;
        ReadInput();
        HandleShooting();
        var level = TilemapLevel2.I;

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

    private void HandleShooting()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow)) Shoot(Vector2.up);
        else if (Input.GetKeyDown(KeyCode.DownArrow)) Shoot(Vector2.down);
        else if (Input.GetKeyDown(KeyCode.LeftArrow)) Shoot(Vector2.left);
        else if (Input.GetKeyDown(KeyCode.RightArrow)) Shoot(Vector2.right);
    }

    private void Shoot(Vector2 dir)
    {
        if ((pelletPrefab == null || tweener == null) || Level2Manager.I.BulletCount <= 0) return;

        GameObject pellet = Instantiate(pelletPrefab, transform.position, Quaternion.identity);
        Vector3 start = transform.position;
        Vector3 end = start + (Vector3)(dir.normalized * pelletDistance);
        float duration = pelletDistance / (pelletSpeed + PlayerPrefs.GetFloat("Upgrade_BS_Value", 0));
        tweener.AddTween(pellet.transform, start, end, duration);
        PelletL2 bullet = pellet.GetComponent<PelletL2>();
        AudioManager.I?.PlaySfx(SfxEvent.Shoot, gameObject);
        Level2Manager.I.UseBullet();
        if (bullet != null) bullet.Init(Camera.main, duration);
    }

    private bool TryBeginMove(Vector2Int dir)
    {
        if (animDriver.IsDead) return false;
        if (dir == Vector2Int.zero) return false;

        var level = TilemapLevel2.I;
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
                Vector3 wobbleEnd = wobbleStart + new Vector3(dir.x * 0.5f, dir.y * 0.5f, 0f);
                float wobbleTime = 0.1f;
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
            animDriver.SetFacing(dir.x > 0 ? PacStudentAnimDriverL2.Dir.Right : PacStudentAnimDriverL2.Dir.Left);
        else
            animDriver.SetFacing(dir.y > 0 ? PacStudentAnimDriverL2.Dir.Up : PacStudentAnimDriverL2.Dir.Down);
    }

    public void StopMovement()
    {
        if (tweener != null) tweener.CancelTween(transform);
        lastInput = Vector2Int.zero;
        currentInput = Vector2Int.zero;
    }

    public void StopAnimation()
    {
        if (animDriver != null)
        {
            animDriver.StopAnimation();
        }
    }

    public void SetInvulnerable(bool invulnerable)
    {
        isInvulnerable = invulnerable;
        if (animDriver != null)
        {
            if (invulnerable) animDriver.StartBlinking();
            else animDriver.StopBlinking();
        }
    }

    public void Respawn(Vector3 spawnPos)
    {
        tweener.CancelTween(transform);
        gridPos = TilemapLevel2.I.WorldToGrid(spawnPos);
        transform.position = spawnPos;
        lastInput = Vector2Int.zero;
        currentInput = Vector2Int.zero;
        hasPendingTeleport = false;
        lastWallHitDir = null;
        facingDir = Vector2Int.right;
        animDriver.ClearDeath();
        animDriver.StopAnimation();
        animDriver.SetFacing(PacStudentAnimDriverL2.Dir.Right);
        lastTeleportTime = -999f;
    }
}
