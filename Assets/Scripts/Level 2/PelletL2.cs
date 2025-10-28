using UnityEngine;

public class PelletL2 : MonoBehaviour
{
    private Camera cam;
    private float lifetime;
    private float startTime;

    public void Init(Camera cameraRef, float travelTime)
    {
        cam = cameraRef;
        lifetime = travelTime + 1f;
        startTime = Time.time;
    }

    void Update()
    {
        if (cam == null) return;
        Vector3 screenPos = cam.WorldToViewportPoint(transform.position);
        if (screenPos.x < -0.1f || screenPos.x > 1.1f || screenPos.y < -0.1f || screenPos.y > 1.1f)
            Destroy(gameObject);
        if (Time.time - startTime > lifetime)
            Destroy(gameObject);
    }
}