using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PacStudentAnimDriver))]
public class PacStudentShowcase : MonoBehaviour
{
    [Header("Cycle Settings")]
    [SerializeField] private float faceHoldSeconds = 1.0f;
    [SerializeField] private float betweenDelay = 0.1f;
    [SerializeField] private string deathStateName = "PacStudentDie";
    [SerializeField] private float deathFallbackSeconds = 1.5f;

    private Animator anim;
    private PacStudentAnimDriver driver;

    void Awake()
    {
        driver = GetComponent<PacStudentAnimDriver>();
        anim = GetComponent<Animator>();
    }

    void OnEnable() => StartCoroutine(Run());

    IEnumerator Run()
    {
        // Force showcase mode
        driver.driveFromVelocity = false;
        driver.moveAlways = true;

        while (true)
        {
            driver.ClearDeath();
            yield return new WaitForSeconds(betweenDelay);

            yield return Face(PacStudentAnimDriver.Dir.Right);
            yield return Face(PacStudentAnimDriver.Dir.Up);
            yield return Face(PacStudentAnimDriver.Dir.Left);
            yield return Face(PacStudentAnimDriver.Dir.Down);

            yield return new WaitForSeconds(betweenDelay);

            driver.PlayDeath();
            yield return WaitForDeathToFinish();

            driver.ClearDeath();
            driver.SetFacing(PacStudentAnimDriver.Dir.Right);
            yield return new WaitForSeconds(betweenDelay);
        }
    }

    IEnumerator Face(PacStudentAnimDriver.Dir dir)
    {
        driver.SetFacing(dir);
        yield return new WaitForSeconds(faceHoldSeconds);
    }

    IEnumerator WaitForDeathToFinish()
    {
        bool entered = false;
        float t = 0f;
        while (t < 0.6f)
        {
            var st = anim.GetCurrentAnimatorStateInfo(0);
            if (st.IsName(deathStateName)) { entered = true; break; }
            t += Time.deltaTime;
            yield return null;
        }

        if (entered)
        {
            float timeout = deathFallbackSeconds;
            var info = anim.GetCurrentAnimatorClipInfo(0);
            if (info.Length > 0 && info[0].clip)
                timeout = Mathf.Max(0.1f, info[0].clip.length / Mathf.Max(0.001f, anim.speed));

            float elapsed = 0f;
            while (elapsed < timeout)
            {
                var st = anim.GetCurrentAnimatorStateInfo(0);
                if (!st.IsName(deathStateName)) break;
                if (st.normalizedTime >= 1f) break;
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(deathFallbackSeconds);
        }
    }
}
