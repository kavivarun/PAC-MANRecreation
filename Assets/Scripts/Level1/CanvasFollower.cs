using UnityEngine;

public class CanvasFollower : MonoBehaviour
{
    [SerializeField] private Transform target; 
    [SerializeField] private Vector3 offset = new Vector3(0, -1, 0);

    void LateUpdate()
    {
        if (target == null) return;
        transform.position = target.position + offset;
        transform.forward = Camera.main.transform.forward; 
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}