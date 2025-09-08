using System.Collections;
using UnityEngine;

public class GhostTester : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject greenGhostPrefab;

    [Header("Path & Motion")]
    [SerializeField] private Vector2 startPos = Vector2.zero;
    [SerializeField] private float speed = 3f;
    [SerializeField] private float sideSeconds = 1.25f;   // time spent per edge

    [Header("Timeline (in squares/laps or seconds)")]
    [SerializeField] private int normalLaps_1 = 2;
    [SerializeField] private float frightenedSeconds = 6f; // will loop squares during this time
    [SerializeField] private int normalLaps_2 = 1;
    [SerializeField] private float deadSeconds = 2.5f;
    [SerializeField] private int normalLaps_afterRespawn = 1;

    [Header("Gizmos")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private Color gizmoColor = new Color(0f, 1f, 0.5f, 0.6f);

    // runtime
    private GameObject ghostGO;
    private Rigidbody2D rb;
    private GhostVisuals visuals;
    private Animator anim;

    void Start()
    {
        StartCoroutine(RunSequence());
    }

    IEnumerator RunSequence()
    {
        if (greenGhostPrefab == null)
        {
            Debug.LogError("[GhostTest] Assign a Green Ghost prefab.");
            yield break;
        }

        // Spawn
        ghostGO = Instantiate(greenGhostPrefab, startPos, Quaternion.identity);
        rb = ghostGO.GetComponent<Rigidbody2D>() ?? ghostGO.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        visuals = ghostGO.GetComponent<GhostVisuals>();
        anim = ghostGO.GetComponent<Animator>();

        if (visuals != null) visuals.EnterNormal();
        Log($"Spawned at {startPos}. Enter NORMAL."); DumpState();

        // Phase A: Normal laps
        yield return MoveSquare(normalLaps_1, "NORMAL A");

        // Phase B: Frightened for some seconds (eyes off per your GhostVisuals)
        if (visuals != null) visuals.EnterFrightened(true);
        Log($"Enter FRIGHTENED for ~{frightenedSeconds:0.0}s."); DumpState();

        float t = 0f;
        while (t < frightenedSeconds)
        {
            yield return MoveSquare(1, "FRIGHTENED");
            t += 4f * sideSeconds;
        }

        // Phase C: Back to normal
        if (visuals != null) visuals.EnterFrightened(false);
        if (visuals != null) visuals.EnterNormal();
        Log("Back to NORMAL."); DumpState();
        yield return MoveSquare(normalLaps_2, "NORMAL B");

        // Phase D: Dead
        if (visuals != null) visuals.EnterDead(true);
        rb.velocity = Vector2.zero;
        Log($"Enter DEAD for {deadSeconds:0.0}s (stop)."); DumpState();
        yield return new WaitForSeconds(deadSeconds);

        // Phase E: Respawn at start, back to normal, one more lap
        ghostGO.transform.position = startPos;
        if (visuals != null) visuals.EnterDead(false);
        if (visuals != null) visuals.EnterNormal();
        Log($"Respawn at {startPos}. Back to NORMAL."); DumpState();
        yield return MoveSquare(normalLaps_afterRespawn, "NORMAL C");

        // Finish
        rb.velocity = Vector2.zero;
        Log("Sequence complete.");
    }

    IEnumerator MoveSquare(int laps, string phaseLabel)
    {
        Vector2[] dirs = { Vector2.right, Vector2.up, Vector2.left, Vector2.down };
        float edge = speed * sideSeconds;

        for (int lap = 0; lap < laps; lap++)
        {
            for (int i = 0; i < 4; i++)
            {
                Vector2 d = dirs[i];
                rb.velocity = d * speed;
                Log($"{phaseLabel} | lap {lap + 1} dir {DirName(d)} | pos {ghostGO.transform.position.ToString("F2")} vel {rb.velocity.ToString("F2")}");
                yield return new WaitForSeconds(sideSeconds);

                // Corner snap (helps reduce drift over long tests)
                Vector3 p = ghostGO.transform.position;
                p.x = Mathf.Round((p.x - startPos.x) / edge) * edge + startPos.x;
                p.y = Mathf.Round((p.y - startPos.y) / edge) * edge + startPos.y;
                ghostGO.transform.position = p;
            }
        }
    }

    string DirName(Vector2 d)
    {
        if (d == Vector2.right) return "RIGHT";
        if (d == Vector2.up) return "UP";
        if (d == Vector2.left) return "LEFT";
        return "DOWN";
    }

    void Log(string msg) => Debug.Log($"[GhostTest t={Time.time:0.00}] {msg}");

    void DumpState()
    {
        if (anim == null) return;
        bool n = anim.GetBool("IsNormal");
        bool f = anim.GetBool("IsFrightened");
        bool d = anim.GetBool("IsDead");
        Log($"Animator: IsNormal={n} IsFrightened={f} IsDead={d}");
    }

    // --- Gizmos: draws the square path based on speed * sideSeconds ---
    void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        float side = speed * sideSeconds;
        Vector3 p0 = new Vector3(startPos.x, startPos.y, 0f);
        Vector3 p1 = p0 + new Vector3(side, 0f, 0f);
        Vector3 p2 = p1 + new Vector3(0f, side, 0f);
        Vector3 p3 = p2 + new Vector3(-side, 0f, 0f);

        Gizmos.color = gizmoColor;
        Gizmos.DrawLine(p0, p1);
        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p0);

        Gizmos.DrawSphere(p0, 0.06f);
        Gizmos.DrawSphere(p1, 0.06f);
        Gizmos.DrawSphere(p2, 0.06f);
        Gizmos.DrawSphere(p3, 0.06f);
    }
}
