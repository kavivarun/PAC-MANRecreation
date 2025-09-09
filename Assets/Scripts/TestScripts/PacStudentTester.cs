using System.Collections;
using UnityEngine;

public class PacStudentTester : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject pacStudentPrefab;

    [Header("Cycle Settings")]
    [SerializeField] private Vector2 spawnPos = Vector2.zero;
    [SerializeField] private float faceHoldSeconds = 1.0f;
    [SerializeField] private float betweenDelay = 0.1f;
    [SerializeField] private string deathStateName = "PacStudentDie"; 
    [SerializeField] private float deathFallbackSeconds = 1.5f;

    GameObject go;
    Animator anim;
    PacStudentAnimDriver driver;

    void Start() => StartCoroutine(Run());

    IEnumerator Run()
    {
        if (!pacStudentPrefab) { Debug.LogError("[PacStudentTester] Assign prefab"); yield break; }

        go = Instantiate(pacStudentPrefab, spawnPos, Quaternion.identity);
        anim = go.GetComponent<Animator>() ?? go.GetComponentInChildren<Animator>(true);
        driver = go.GetComponent<PacStudentAnimDriver>();
        if (!anim) { Debug.LogError("[PacStudentTester] Prefab missing Animator"); yield break; }
        if (!driver) { Debug.LogError("[PacStudentTester] Prefab missing PacStudentAnimDriver"); yield break; }

        // Let the driver be param-driven (no RB), keep anims running
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

            // Respawn/reset and loop
            go.transform.position = spawnPos;
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
            // Use the current clip length if available
            float timeout = deathFallbackSeconds;
            var info = anim.GetCurrentAnimatorClipInfo(0);
            if (info.Length > 0 && info[0].clip) timeout = Mathf.Max(0.1f, info[0].clip.length / Mathf.Max(0.001f, anim.speed));

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
