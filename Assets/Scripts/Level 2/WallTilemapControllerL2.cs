using UnityEngine;
using UnityEngine.Tilemaps;

public class WallTilemapControllerL2 : MonoBehaviour
{
    [SerializeField] private ParticleSystem wallHitEffectPrefab;

    private Tilemap tilemap;

    void Awake()
    {
        tilemap = GetComponent<Tilemap>();
    }

    public void PlayWallHit(Vector3 worldPos)
    {
        if (wallHitEffectPrefab == null) return;

        var effect = Instantiate(wallHitEffectPrefab, worldPos, Quaternion.identity);
        effect.Play();
        Destroy(effect.gameObject, 1f);
    }
}
