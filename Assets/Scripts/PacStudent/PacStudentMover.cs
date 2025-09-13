using UnityEngine;

[RequireComponent(typeof(PacStudentAnimDriver))]
public class PacStudentMover : MonoBehaviour
{
    [SerializeField] private Vector3[] pathCorners;  // TL → TR → BR → BL in clockwise order
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float snapEps = 0.001f;

    private PacStudentAnimDriver animDriver;
    private Tweener tweener;
    private int idx;

    void Awake()
    {
        animDriver = GetComponent<PacStudentAnimDriver>();
        animDriver.driveFromVelocity = false;
        tweener = Tweener.FindFirstObjectByType<Tweener>();
    }

    void Start()
    {
        if (tweener == null || pathCorners == null || pathCorners.Length < 4) { enabled = false; return; }
        idx = 0;
        transform.position = pathCorners[idx];
        StartNextMovement();
    }

    void Update()
    {
        if (!tweener.TweenExists(transform)) StartNextMovement();
    }

    private void StartNextMovement()
    {
        int next = (idx + 1) % pathCorners.Length;
        Vector3 a = pathCorners[idx];
        Vector3 b = pathCorners[next];
        Vector3 d = b - a;

        if (d.sqrMagnitude <= snapEps * snapEps) { idx = next; return; }

        SetFacing(d);
        float duration = d.magnitude / Mathf.Max(0.0001f, moveSpeed);
        if (tweener.AddTween(transform, a, b, duration)) idx = next;
    }

    private void SetFacing(Vector3 dir)
    {
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            animDriver.SetFacing(dir.x > 0 ? PacStudentAnimDriver.Dir.Right : PacStudentAnimDriver.Dir.Left);
        else
            animDriver.SetFacing(dir.y > 0 ? PacStudentAnimDriver.Dir.Up : PacStudentAnimDriver.Dir.Down);
    }
}
