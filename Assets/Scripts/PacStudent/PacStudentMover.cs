using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PacStudentAnimDriver))]
public class PacStudentMover : MonoBehaviour
{
    [Header("Path Settings")]
    [SerializeField] private Vector3[] pathCorners;  
    [SerializeField] private float moveSpeed = 2f;   

    private PacStudentAnimDriver animDriver;
    private int targetIndex = 0;

    void Awake()
    {
        animDriver = GetComponent<PacStudentAnimDriver>();
        animDriver.driveFromVelocity = false; 
    }

    void Start()
    {
        if (pathCorners == null || pathCorners.Length < 2)
        {
            Debug.LogError("[PacStudentMover] Please assign at least 2 path points.");
            enabled = false;
            return;
        }

        transform.position = pathCorners[0];
        targetIndex = 1; 
        SetFacing(pathCorners[targetIndex] - transform.position);
    }

    void Update()
    {
        if (pathCorners == null || pathCorners.Length == 0) return;

        // Move linearly towards the target corner
        Vector3 targetPos = pathCorners[targetIndex];
        Vector3 moveDir = (targetPos - transform.position).normalized;
        transform.position += moveDir * moveSpeed * Time.deltaTime;

        SetFacing(moveDir);

        if (Vector3.Distance(transform.position, targetPos) < 0.05f)
        {
            transform.position = targetPos; 
            targetIndex = (targetIndex + 1) % pathCorners.Length; 
        }
    }

    private void SetFacing(Vector3 dir)
    {
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            animDriver.SetFacing(dir.x > 0 ? PacStudentAnimDriver.Dir.Right : PacStudentAnimDriver.Dir.Left);
        }
        else
        {
            animDriver.SetFacing(dir.y > 0 ? PacStudentAnimDriver.Dir.Up : PacStudentAnimDriver.Dir.Down);
        }
    }
}