using System.Collections;
using UnityEngine;

public class PacStudentAnimDriverL2 : MonoBehaviour
{
    public static PacStudentAnimDriverL2 I { get; private set; }

    [SerializeField] private Animator animator;
    [SerializeField] private bool moveAlways = true;
    [SerializeField] private string deathStateName = "PacStudentDie";
    [SerializeField] private ParticleSystem dustEffect;
    [SerializeField] private ParticleSystem deathEffect;

    static readonly int P_MoveX = Animator.StringToHash("MoveX");
    static readonly int P_MoveY = Animator.StringToHash("MoveY");
    static readonly int P_IsDead = Animator.StringToHash("IsDead");

    public enum Dir { Right, Up, Left, Down }

    Vector2 externalFacing = Vector2.right;

    // Blinking system
    private bool isBlinking = false;
    private Coroutine blinkingRoutine;
    private SpriteRenderer spriteRenderer;
    private Color baseColor;

    public bool IsDead => animator != null && animator.GetBool(P_IsDead);

    void Awake()
    {
        I = this;
        if (!animator) animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) baseColor = spriteRenderer.color;
        if (dustEffect != null) dustEffect.Stop();
    }

    void Update()
    {
        if (IsDead)
        {
            animator.speed = 1f;
            StopDust();
            return;
        }

        animator.SetFloat(P_MoveX, externalFacing.x);
        animator.SetFloat(P_MoveY, externalFacing.y);

        if (moveAlways) animator.speed = 1f;

        if (dustEffect != null)
        {
            bool isMoving = animator.speed > 0.01f;

            if (isMoving && !dustEffect.isPlaying) dustEffect.Play();
            else if (!isMoving && dustEffect.isPlaying) dustEffect.Stop();

            UpdateDustOrientation();
        }
    }

    void UpdateDustOrientation()
    {
        if (dustEffect == null) return;

        Vector3 offset = Vector3.zero;
        float rotationZ = 0f;

        switch (GetCurrentDirection())
        {
            case Dir.Up: offset = new Vector3(0.2f, -0.2f, 0); rotationZ = 0f; break;
            case Dir.Down: offset = new Vector3(-0.2f, -0.2f, 0); rotationZ = 180f; break;
            case Dir.Left: offset = new Vector3(0f, -0.3f, 0); rotationZ = 90f; break;
            default: offset = new Vector3(0f, -0.3f, 0); rotationZ = -90f; break;
        }

        dustEffect.transform.localPosition = offset;
        dustEffect.transform.localRotation = Quaternion.Euler(0, 0, rotationZ);
    }

    Dir GetCurrentDirection()
    {
        if (externalFacing == Vector2.right) return Dir.Right;
        if (externalFacing == Vector2.left) return Dir.Left;
        if (externalFacing == Vector2.up) return Dir.Up;
        if (externalFacing == Vector2.down) return Dir.Down;
        return Dir.Right;
    }

    public void OnStep() => AudioManager.I?.PlaySfx(SfxEvent.Step, gameObject);

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
        if (deathEffect != null)
        {
            var effect = Instantiate(deathEffect, transform.position, Quaternion.identity);
            effect.Play();
            Destroy(effect.gameObject, 1f);
        }
        animator.speed = 1f;
        animator.SetBool(P_IsDead, true);
        if (!string.IsNullOrEmpty(deathStateName)) animator.Play(deathStateName, 0, 0f);
        StopDust();
    }

    public void ClearDeath()
    {
        animator.SetBool(P_IsDead, false);
        animator.speed = 1f;
    }

    public void StopAnimation()
    {
        animator.speed = 0f;
        StopDust();
    }

    public void StartAnimation() => animator.speed = 1f;

    void StopDust()
    {
        if (dustEffect != null && dustEffect.isPlaying) dustEffect.Stop();
    }

    public void StartBlinking()
    {
        if (isBlinking) return;
        isBlinking = true;
        if (blinkingRoutine != null) StopCoroutine(blinkingRoutine);
        blinkingRoutine = StartCoroutine(BlinkingRoutine());
    }

    public void StopBlinking()
    {
        isBlinking = false;
        if (blinkingRoutine != null)
        {
            StopCoroutine(blinkingRoutine);
            blinkingRoutine = null;
        }
        if (spriteRenderer != null)
            spriteRenderer.color = baseColor;
    }

    private IEnumerator BlinkingRoutine()
    {
        float flashSpeed = 10f;
        float minBrightness = 0.4f;
        float maxBrightness = 1.2f;
        float t = 0f;

        while (isBlinking)
        {
            t += Time.deltaTime * flashSpeed;
            float brightness = Mathf.Lerp(minBrightness, maxBrightness, (Mathf.Sin(t) + 1f) * 0.5f);

            if (spriteRenderer != null)
            {
                Color c = baseColor * brightness;
                c.a = baseColor.a;
                spriteRenderer.color = c;
            }

            yield return null;
        }
        if (spriteRenderer != null)
            spriteRenderer.color = baseColor;
    }
}
