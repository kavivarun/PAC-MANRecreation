using UnityEngine;

public class GhostVisuals : MonoBehaviour
{
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private SpriteRenderer eyes;
    [SerializeField] private Sprite[] eyesDir = new Sprite[4]; // [Right, Up, Left, Down]
    [SerializeField] private Animator animator;                
    [SerializeField] private bool useVelocityForEyes = true;


    // Animator params
    private static readonly int P_IsFrightened = Animator.StringToHash("IsFrightened");
    private static readonly int P_IsDead = Animator.StringToHash("IsDead");
    private static readonly int P_IsNormal = Animator.StringToHash("IsNormal");
    private static readonly int P_Speed = Animator.StringToHash("Speed");

    private enum VisualState { Normal, Frightened, Dead }
    private VisualState state = VisualState.Normal;

    private Vector3 lastPosition;
    private Vector3 velocity;

    private int lastDir = 0;

    void Awake()
    {
        if (!animator) animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>(true);
        if (!animator) { Debug.LogError($"[GhostVisuals] No Animator on {name}"); enabled = false; return; }

        lastPosition = transform.position;
    }

    void OnEnable() => ApplyState();

    void Update()
    {
        velocity = (transform.position - lastPosition) / Time.deltaTime;
        lastPosition = transform.position;

        animator.speed = 1f; // always animate

        animator.SetFloat(P_Speed, velocity.magnitude);

        bool showEyes = state == VisualState.Normal;
        if (eyes) eyes.enabled = showEyes;

        if (showEyes && eyesDir != null && eyesDir.Length == 4)
        {
            int dir = useVelocityForEyes ? DirFromVelocity(velocity, lastDir) : lastDir;
            if (dir != lastDir)
            {
                lastDir = dir;
                eyes.sprite = eyesDir[dir];
            }
        }
    }

    // Public API
    public void EnterNormal() => SetState(VisualState.Normal);
    public void EnterFrightened(bool on = true) => SetState(on ? VisualState.Frightened : VisualState.Normal);
    public void EnterDead(bool on = true) => SetState(on ? VisualState.Dead : VisualState.Normal);

    public void SetDirection(int dir)
    {
        lastDir = Mathf.Clamp(dir, 0, 3);
        if (eyes && eyes.enabled && eyesDir != null && eyesDir.Length == 4)
            eyes.sprite = eyesDir[lastDir];
    }

    // Internal Methods
    private void SetState(VisualState s)
    {
        state = s;
        ApplyState();
    }

    private void ApplyState()
    {
        if (!animator) return;

        bool isNormal = state == VisualState.Normal;
        bool isFrightened = state == VisualState.Frightened;
        bool isDead = state == VisualState.Dead;

        animator.SetBool(P_IsNormal, isNormal);
        animator.SetBool(P_IsFrightened, isFrightened);
        animator.SetBool(P_IsDead, isDead);

        if (eyes) eyes.enabled = isNormal;
        if (eyes && eyes.enabled && eyesDir != null && eyesDir.Length == 4)
            eyes.sprite = eyesDir[lastDir];
    }

    private static int DirFromVelocity(Vector2 v, int fallback)
    {
        if (v.sqrMagnitude < 1e-6f) return fallback;
        return Mathf.Abs(v.x) >= Mathf.Abs(v.y) ? (v.x >= 0 ? 0 : 2) : (v.y >= 0 ? 1 : 3);
    }
}
