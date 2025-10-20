using UnityEngine;
using System.Collections;

public class CherryController : MonoBehaviour
{
    [SerializeField] private GameObject cherryPrefab;
    [SerializeField] private float spawnDelay = 5f;
    [SerializeField] private float moveDuration = 8f;

    private Tweener tweener;
    private Camera mainCam;
    private bool canSpawn = true;
    private Coroutine disableCoroutine;

    void Start()
    {
        tweener = FindFirstObjectByType<Tweener>();
        mainCam = Camera.main;
        StartCoroutine(SpawnAfterDelay(spawnDelay));
    }

    IEnumerator SpawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (canSpawn)
            SpawnCherry();
    }

    void SpawnCherry()
    {
        if (!cherryPrefab || !tweener || !mainCam)
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

        GameObject cherry = Instantiate(cherryPrefab, startPos, Quaternion.identity);
        cherry.GetComponent<SpriteRenderer>().sortingOrder = 20;

        Cherry cherryScript = cherry.GetComponent<Cherry>();
        cherryScript.OnCollected += HandleCherryCollected;

        tweener.AddTween(cherry.transform, startPos, endPos, moveDuration);
        disableCoroutine = StartCoroutine(DisableAfter(cherry, moveDuration));
    }

    IEnumerator DisableAfter(GameObject cherry, float duration)
    {
        yield return new WaitForSeconds(duration);
        if (cherry)
            Destroy(cherry);
        StartCoroutine(SpawnAfterDelay(spawnDelay));
    }

    void HandleCherryCollected(GameObject cherry)
    {
        if (disableCoroutine != null)
            StopCoroutine(disableCoroutine);

        if (cherry)
            Destroy(cherry);

        StartCoroutine(SpawnAfterDelay(spawnDelay));
    }

    void OnDestroy()
    {
        canSpawn = false;
    }
}
