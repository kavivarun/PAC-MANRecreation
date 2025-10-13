using UnityEngine;

public class PacStudentAnimDriver : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] public bool moveAlways = true;
    [SerializeField] private string deathStateName = "PacStudentDie";

    static readonly int P_MoveX = Animator.StringToHash("MoveX");
    static readonly int P_MoveY = Animator.StringToHash("MoveY");
    static readonly int P_IsDead = Animator.StringToHash("IsDead");

    public enum Dir { Right, Up, Left, Down }

    Vector2 externalFacing = Vector2.right;

    void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (animator.GetBool(P_IsDead))
        {
            animator.speed = 1f;
            return;
        }

        animator.SetFloat(P_MoveX, externalFacing.x);
        animator.SetFloat(P_MoveY, externalFacing.y);

        if (moveAlways)
            animator.speed = 1f;
    }

    public void OnStep()
    {
        AudioManager.I?.PlaySfx(SfxEvent.Step, gameObject);
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

        animator.SetFloat(P_MoveX, externalFacing.x);
        animator.SetFloat(P_MoveY, externalFacing.y);
    }

    public void PlayDeath()
    {
        animator.speed = 1f;
        animator.SetBool(P_IsDead, true);
        if (!string.IsNullOrEmpty(deathStateName))
            animator.CrossFadeInFixedTime(deathStateName, 0.05f, 0);
    }

    public void ClearDeath()
    {
        animator.SetBool(P_IsDead, false);
        animator.speed = 1f;
    }

    public void StopAnimation()
    {
        animator.speed = 0f;
    }

    public void StartAnimation()
    {
        animator.speed = 1f;
    }
}
