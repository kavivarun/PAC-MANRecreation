using UnityEngine;

[RequireComponent(typeof(PacStudentAnimDriver))]
public class PacStudentController : MonoBehaviour
{
    public float moveSpeed = 6f;
    public float cellSize = 1f;
    public float snapEps = 0.01f;

    private Vector2Int gridPos;
    private Vector2Int lastInput = Vector2Int.right;
    private Vector2Int currentInput = Vector2Int.zero;

    private Tweener tweener;
    private PacStudentAnimDriver animDriver;

    private bool hasPendingTeleport;
    private Vector2Int pendingTeleportDest;

    void Awake()
    {
        tweener = Tweener.FindFirstObjectByType<Tweener>();
        animDriver = GetComponent<PacStudentAnimDriver>();
    }

    void Start()
    {
        var level = TilemapLevel.I;
        gridPos = level.WorldToGrid(transform.position);
        transform.position = level.GridToWorld(gridPos);
        SetFacing(Vector2Int.right);
    }

    void Update()
    {
        ReadInput();

        if (!tweener.TweenExists(transform))
        {
            var level = TilemapLevel.I;
            transform.position = level.GridToWorld(gridPos);

            if (hasPendingTeleport)
            {
                hasPendingTeleport = false;
                gridPos = pendingTeleportDest;
                transform.position = level.GridToWorld(gridPos);
                if (TryBeginMove(currentInput)) return;
            }

            if (TryBeginMove(lastInput)) return;
            if (TryBeginMove(currentInput)) return;

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
        if (Input.GetKeyDown(KeyCode.W)) lastInput = Vector2Int.up;
        else if (Input.GetKeyDown(KeyCode.S)) lastInput = Vector2Int.down;
        else if (Input.GetKeyDown(KeyCode.A)) lastInput = Vector2Int.left;
        else if (Input.GetKeyDown(KeyCode.D)) lastInput = Vector2Int.right;
    }

    private bool TryBeginMove(Vector2Int dir)
    {
        if (dir == Vector2Int.zero) return false;

        var level = TilemapLevel.I;
        Vector2Int next = gridPos + dir;
        if (!level.IsWalkableForPlayer(gridPos, next)) return false;

        if ((level.GetTile(gridPos) & TileFlags.Teleporter) != 0 &&
            level.TryTeleport(gridPos, out var dest))
        {
            hasPendingTeleport = true;
            pendingTeleportDest = dest;
        }

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
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            animDriver.SetFacing(dir.x > 0 ? PacStudentAnimDriver.Dir.Right : PacStudentAnimDriver.Dir.Left);
        else
            animDriver.SetFacing(dir.y > 0 ? PacStudentAnimDriver.Dir.Up : PacStudentAnimDriver.Dir.Down);
    }
}
