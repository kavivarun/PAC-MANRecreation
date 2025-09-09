using UnityEngine;

public class PacStudentAnimDriver : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] public bool driveFromVelocity = true;
    [SerializeField] public bool moveAlways = true;
    [SerializeField] private string deathStateName = "PacStudentDie"; 

    static readonly int P_MoveX = Animator.StringToHash("MoveX");
    static readonly int P_MoveY = Animator.StringToHash("MoveY");
    static readonly int P_IsDead = Animator.StringToHash("IsDead");
    static readonly int P_Speed = Animator.StringToHash("Speed");

    public enum Dir { Right, Up, Left, Down }

    Vector2 lastDir = Vector2.right;
    Vector2 externalFacing = Vector2.right;

    void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (rb)
        {
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
    }

    void Update()
    {
        // If dead, keep the animator running so the death clip plays.
        if (animator.GetBool(P_IsDead))
        {
            animator.speed = 1f;
            return;
        }

        Vector2 v = Vector2.zero;

        if (driveFromVelocity && rb)
        {
            v = rb.velocity;
            if (v.sqrMagnitude > 0.0001f)
                lastDir = Mathf.Abs(v.x) >= Mathf.Abs(v.y)
                        ? new Vector2(Mathf.Sign(v.x), 0)
                        : new Vector2(0, Mathf.Sign(v.y));
        }
        else
        {
            lastDir = externalFacing;
        }

        animator.SetFloat(P_MoveX, lastDir.x);
        animator.SetFloat(P_MoveY, lastDir.y);

        if (moveAlways)
        {
            animator.speed = 1f;             
        }
        else
        {
            float s = driveFromVelocity ? v.magnitude : 0f;
            animator.speed = s > 0.05f ? Mathf.Clamp01(s / 3f) : 0f;
            animator.SetFloat(P_Speed, s);
        }
    }

    public void SetFacing(Dir dir)
    {
        switch (dir)
        {
            case Dir.Right: externalFacing = new Vector2(1, 0); break;
            case Dir.Up: externalFacing = new Vector2(0, 1); break;
            case Dir.Left: externalFacing = new Vector2(-1, 0); break;
            case Dir.Down: externalFacing = new Vector2(0, -1); break;
        }
        if (!driveFromVelocity)
        {
            animator.SetFloat(P_MoveX, externalFacing.x);
            animator.SetFloat(P_MoveY, externalFacing.y);
        }
    }

    public void PlayDeath()
    {
        animator.speed = 1f;                 
        animator.SetBool(P_IsDead, true);
        if (rb) rb.velocity = Vector2.zero;

        // Force entry if the transition is ever flaky:
        if (!string.IsNullOrEmpty(deathStateName))
            animator.CrossFadeInFixedTime(deathStateName, 0.05f, 0);
    }

    public void ClearDeath()
    {
        animator.SetBool(P_IsDead, false);
        animator.speed = 1f;
    }
}
