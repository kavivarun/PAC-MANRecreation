using UnityEngine;
using System.Collections;

public class CherryController : MonoBehaviour
{
    [SerializeField] private GameObject cherryPrefab;
    [SerializeField] private float spawnDelay = 5f;
    [SerializeField] private float moveDuration = 8f;

    private Tweener tweener;
    private GameObject currentCherry;
    private Camera mainCam;

    void Start()
    {
        tweener = FindFirstObjectByType<Tweener>();
        mainCam = Camera.main;

        currentCherry = Instantiate(cherryPrefab);
        currentCherry.SetActive(false);
        currentCherry.GetComponent<SpriteRenderer>().sortingOrder = 20;

        StartCoroutine(SpawnAfterDelay(spawnDelay));
    }

    IEnumerator SpawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnCherry();
    }

    void SpawnCherry()
    {
        if (!currentCherry || !tweener || !mainCam)
            return;

        Vector3 center = mainCam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0));
        float camHeight = mainCam.orthographicSize;
        float camWidth = camHeight * mainCam.aspect;
        float radius = Mathf.Max(camWidth, camHeight) + 2f;

        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
        Vector3 startPos = center + dir * radius;
        Vector3 endPos = center - dir * radius;

        startPos.z = 0;
        endPos.z = 0;

        currentCherry.transform.position = startPos;
        currentCherry.SetActive(true);

        tweener.AddTween(currentCherry.transform, startPos, endPos, moveDuration);
        StartCoroutine(DisableAfter(moveDuration));
    }

    IEnumerator DisableAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (currentCherry)
            currentCherry.SetActive(false);
        StartCoroutine(SpawnAfterDelay(spawnDelay));
    }
}
