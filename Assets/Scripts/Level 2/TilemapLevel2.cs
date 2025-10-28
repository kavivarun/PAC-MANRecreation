using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapLevel2 : MonoBehaviour
{
    public static TilemapLevel2 I { get; private set; }

    [Header("Tilemaps")]
    public Tilemap walls;
    public Tilemap floor;
    public Tilemap pellets;
    public Tilemap powerPellets;
    public Tilemap teleporters;
    public Tilemap ghostHome;
    public Tilemap ghostGate;
    public Tilemap outsidePerimeter;

    [Header("Prefabs")]
    public GameObject BulletCollectPrefab;
    public GameObject LifeCollectPrefab;
    public GameObject TeleportEffectPrefab;

    [Header("Grid Settings")]
    public Vector2 gridOrigin = Vector2.zero;
    public float cellSize = 1f;

    [Header("Collectible Spawn Settings")]
    [SerializeField] private float bulletRespawnDelay = 5f;
    [SerializeField] private float lifeSpawnInterval = 60f;
    [SerializeField] private float lifeCollectibleDuration = 10f;

    private Vector2Int? teleporterA;
    private Vector2Int? teleporterB;

    private TileFlags[,] map;
    private BoundsInt bounds;

    // Collectible spawning system
    private List<Vector2Int> validFloorTiles = new List<Vector2Int>();
    private GameObject currentBulletCollectible;
    private GameObject currentLifeCollectible;
    private Coroutine bulletSpawnCoroutine;
    private Coroutine lifeSpawnCoroutine;

    public BoundsInt Bounds => bounds;

    void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        Build();
    }

    void Start()
    {
        StartCollectibleSystems();
    }

    private void Build()
    {
        var tms = new List<Tilemap> { walls, floor, pellets, powerPellets, teleporters, ghostHome, ghostGate, outsidePerimeter };
        bounds = tms[0] != null ? tms[0].cellBounds : new BoundsInt(0, 0, 0, 1, 1, 1);
        foreach (var tm in tms)
        {
            if (tm == null) continue;
            bounds.xMin = Mathf.Min(bounds.xMin, tm.cellBounds.xMin);
            bounds.xMax = Mathf.Max(bounds.xMax, tm.cellBounds.xMax);
            bounds.yMin = Mathf.Min(bounds.yMin, tm.cellBounds.yMin);
            bounds.yMax = Mathf.Max(bounds.yMax, tm.cellBounds.yMax);
        }

        map = new TileFlags[bounds.size.x, bounds.size.y];

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int c = new Vector3Int(x, y, 0);
                TileFlags f = TileFlags.None;

                if (walls != null && walls.HasTile(c)) f |= TileFlags.Wall;
                if (floor != null && floor.HasTile(c))
                {
                    f |= TileFlags.WalkPlayer | TileFlags.WalkGhost;
                    validFloorTiles.Add(new Vector2Int(x, y));
                }
                if (ghostHome != null && ghostHome.HasTile(c)) f |= TileFlags.GhostHouse;
                if (ghostGate != null && ghostGate.HasTile(c)) f |= TileFlags.GhostGate;
                if (teleporters != null && teleporters.HasTile(c))
                {
                    f |= TileFlags.Teleporter;
                    Vector2Int cell = new Vector2Int(x, y);
                    if (teleporterA == null) teleporterA = cell;
                    else if (teleporterB == null) teleporterB = cell;
                }
                if (pellets != null && pellets.HasTile(c)) f |= TileFlags.Pellet;
                if (powerPellets != null && powerPellets.HasTile(c)) f |= TileFlags.PowerPellet;

                map[x - bounds.xMin, y - bounds.yMin] = f;
            }
        }
    }

    private void StartCollectibleSystems()
    {
        if (BulletCollectPrefab != null)
        {
            SpawnBulletCollectible();
        }

        if (LifeCollectPrefab != null)
        {
            lifeSpawnCoroutine = StartCoroutine(LifeCollectibleSpawnLoop());
        }
    }

    private void SpawnBulletCollectible()
    {
        if (BulletCollectPrefab == null || validFloorTiles.Count == 0) return;

        Vector2Int spawnTile = GetRandomFloorTile();
        Vector3 worldPos = GridToWorld(spawnTile);

        currentBulletCollectible = Instantiate(BulletCollectPrefab, worldPos, Quaternion.identity);

        if (bulletSpawnCoroutine != null)
        {
            StopCoroutine(bulletSpawnCoroutine);
        }
        bulletSpawnCoroutine = StartCoroutine(MonitorBulletCollection());
    }

    private IEnumerator MonitorBulletCollection()
    {
        while (currentBulletCollectible != null)
        {
            yield return null;
        }

        yield return new WaitForSeconds(bulletRespawnDelay);
        SpawnBulletCollectible();
    }

    private IEnumerator LifeCollectibleSpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(lifeSpawnInterval);

            SpawnLifeCollectible();

            yield return new WaitForSeconds(lifeCollectibleDuration);

            if (currentLifeCollectible != null)
            {
                Destroy(currentLifeCollectible);
                currentLifeCollectible = null;
            }
        }
    }

    private void SpawnLifeCollectible()
    {
        if (LifeCollectPrefab == null || validFloorTiles.Count == 0) return;

        if (currentLifeCollectible != null)
        {
            Destroy(currentLifeCollectible);
        }

        Vector2Int spawnTile = GetRandomFloorTile();
        Vector3 worldPos = GridToWorld(spawnTile);

        currentLifeCollectible = Instantiate(LifeCollectPrefab, worldPos, Quaternion.identity);
    }

    private Vector2Int GetRandomFloorTile()
    {
        if (validFloorTiles.Count == 0)
        {
            Debug.LogWarning("No valid floor tiles available for spawning collectibles!");
            return Vector2Int.zero;
        }

        int randomIndex = Random.Range(0, validFloorTiles.Count);
        return validFloorTiles[randomIndex];
    }

    public void TriggerBulletRespawn()
    {
        if (bulletSpawnCoroutine != null)
        {
            StopCoroutine(bulletSpawnCoroutine);
        }
        bulletSpawnCoroutine = StartCoroutine(DelayedBulletSpawn());
    }

    private IEnumerator DelayedBulletSpawn()
    {
        yield return new WaitForSeconds(bulletRespawnDelay);
        SpawnBulletCollectible();
    }

    void OnDestroy()
    {
        if (bulletSpawnCoroutine != null)
        {
            StopCoroutine(bulletSpawnCoroutine);
        }
        if (lifeSpawnCoroutine != null)
        {
            StopCoroutine(lifeSpawnCoroutine);
        }
    }

    public Vector2Int WorldToGrid(Vector3 world)
    {
        float gx = (world.x - gridOrigin.x) / cellSize;
        float gy = (world.y - gridOrigin.y) / cellSize;
        return new Vector2Int(Mathf.RoundToInt(gx), Mathf.RoundToInt(gy));
    }

    public Vector3 GridToWorld(Vector2Int cell)
    {
        return new Vector3(gridOrigin.x + cell.x * cellSize, gridOrigin.y + cell.y * cellSize, 0f);
    }

    public bool InBounds(Vector2Int c)
    {
        return c.x >= bounds.xMin && c.x < bounds.xMax && c.y >= bounds.yMin && c.y < bounds.yMax;
    }

    public TileFlags GetTile(Vector2Int c)
    {
        if (!InBounds(c)) return TileFlags.None;
        return map[c.x - bounds.xMin, c.y - bounds.yMin];
    }

    public bool IsWalkableForPlayer(Vector2Int from, Vector2Int to)
    {
        if (!InBounds(to)) return false;
        var t = GetTile(to);
        if ((t & TileFlags.Wall) != 0) return false;
        if ((t & TileFlags.GhostHouse) != 0) return false;
        if ((t & TileFlags.GhostGate) != 0) return false;
        return (t & TileFlags.WalkPlayer) != 0 || (t & TileFlags.Teleporter) != 0;
    }

    public bool TryTeleport(Vector2Int from, out Vector2Int dest)
    {
        dest = from;
        if ((GetTile(from) & TileFlags.Teleporter) == 0) return false;
        if (teleporterA.HasValue && teleporterB.HasValue)
        {
            if (from == teleporterA.Value) { dest = teleporterB.Value; return true; }
            if (from == teleporterB.Value) { dest = teleporterA.Value; return true; }
        }
        return false;
    }

    public bool IsOutsidePerimeter(Vector2Int c)
    {
        if (!InBounds(c)) return false;
        if (outsidePerimeter == null) return false;
        return outsidePerimeter.HasTile(new Vector3Int(c.x, c.y, 0));
    }
}
