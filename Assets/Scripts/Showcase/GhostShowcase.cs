using System.Collections;
using UnityEngine;

[RequireComponent(typeof(GhostVisuals))]
public class GhostShowcase : MonoBehaviour
{
    [Header("Cycle Settings")]
    [SerializeField] private float faceHoldSeconds = 1.0f;   
    [SerializeField] private float betweenDelay = 0.2f;      
    [SerializeField] private float frightenedSeconds = 2.0f; 
    [SerializeField] private float deadSeconds = 2.0f;       
    [SerializeField] private bool loop = true;

    private GhostVisuals ghost;

    void Awake()
    {
        ghost = GetComponent<GhostVisuals>();
    }

    void OnEnable()
    {
        StartCoroutine(RunShowcase());
    }

    private IEnumerator RunShowcase()
    {
        do
        {
            // Normal mode, cycle directions
            ghost.EnterNormal();

            yield return Face(0); 
            yield return Face(1); 
            yield return Face(2); 
            yield return Face(3);

            yield return new WaitForSeconds(betweenDelay);

            // Frightened mode
            ghost.EnterFrightened(true);
            yield return new WaitForSeconds(frightenedSeconds);

            // Dead mode
            ghost.EnterDead(true);
            yield return new WaitForSeconds(deadSeconds);

            // Reset
            ghost.EnterNormal();
            ghost.SetDirection(0); 
            yield return new WaitForSeconds(betweenDelay);

        } while (loop);
    }

    private IEnumerator Face(int dir)
    {
        ghost.SetDirection(dir);
        yield return new WaitForSeconds(faceHoldSeconds);
    }
}
