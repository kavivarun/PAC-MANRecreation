using System;
using System.Collections.Generic;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Top-left quadrant layout only. Will be mirrored to a full map.")]
    [SerializeField]
    private int[,] levelMap = new int[,]
    {
        {1,2,2,2,2,2,2,2,2,2,2,2,2,7},
        {2,5,5,5,5,5,5,5,5,5,5,5,5,4},
        {2,5,3,4,4,3,5,3,4,4,4,3,5,4},
        {2,6,4,0,0,4,5,4,0,0,0,4,5,4},
        {2,5,3,4,4,3,5,3,4,4,4,3,5,3},
        {2,5,5,5,5,5,5,5,5,5,5,5,5,5},
        {2,5,3,4,4,3,5,3,3,5,3,4,4,4},
        {2,5,3,4,4,3,5,4,4,5,3,4,4,3},
        {2,5,5,5,5,5,5,4,4,5,5,5,5,4},
        {1,2,2,2,2,1,5,4,3,4,4,3,0,4},
        {0,0,0,0,0,2,5,4,3,4,4,3,0,3},
        {0,0,0,0,0,2,5,4,4,0,0,0,0,0},
        {0,0,0,0,0,2,5,4,4,0,3,4,4,8},
        {2,2,2,2,2,1,5,3,3,0,4,0,0,0},
        {0,0,0,0,0,0,5,0,0,0,4,0,0,0},
    };

    [Header("Prefabs by Type Id")]
    [SerializeField] private GameObject floorPrefab;           // 0
    [SerializeField] private GameObject outsideCornerPrefab;   // 1
    [SerializeField] private GameObject outsideWallPrefab;     // 2
    [SerializeField] private GameObject insideCornerPrefab;    // 3
    [SerializeField] private GameObject insideWallPrefab;      // 4
    [SerializeField] private GameObject pelletPrefab;          // 5 
    [SerializeField] private GameObject powerPelletPrefab;     // 6 
    [SerializeField] private GameObject tJunctionPrefab;       // 7
    [SerializeField] private GameObject ghostGatePrefab;       // 8
    [SerializeField] private GameObject pacStudentPrefab;

    [Header("Layout")]
    [SerializeField] private Vector2 spawnOrigin = Vector2.zero;
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private string existingLevelRootName = "Level 01";
    [SerializeField] private string generatedRootName = "Level_Generated";
    [SerializeField] private float cameraPadding = 1.0f;

    private Transform _root;

    void Start()
    {
        var old = GameObject.Find(existingLevelRootName);
        if (old != null) Destroy(old);

        var prev = GameObject.Find(generatedRootName);
        if (prev != null) Destroy(prev);

        _root = new GameObject(generatedRootName).transform;
        _root.position = Vector3.zero;

        int[,] full = BuildFullMap(levelMap);
        Generate(full);
        FitCameraTo(full);
        if (pacStudentPrefab)
            SpawnPacStudent(full);
    }

    private void SpawnPacStudent(int[,] map)
    {
        int rows = map.GetLength(0);
        int cols = map.GetLength(1);

        float width = cols * tileSize;
        float height = rows * tileSize;

        // Top-left world position of the map
        Vector3 topLeft = new Vector3(
            spawnOrigin.x,
            spawnOrigin.y,
            0f
        );

        // Define rectangle corners clockwise
        Vector3[] corners = new Vector3[4];
        corners[0] = topLeft + new Vector3(tileSize * 0.5f, -tileSize * 0.5f, 0f);         // top-left tile center
        corners[1] = topLeft + new Vector3(width - tileSize * 0.5f, -tileSize * 0.5f, 0f); // top-right
        corners[2] = topLeft + new Vector3(width - tileSize * 0.5f, -(height - tileSize * 0.5f), 0f); // bottom-right
        corners[3] = topLeft + new Vector3(tileSize * 0.5f, -(height - tileSize * 0.5f), 0f); // bottom-left

        // Instantiate PacStudent
        var pac = Instantiate(pacStudentPrefab, corners[0], Quaternion.identity);
        var mover = pac.GetComponent<PacStudentMover>();
        if (mover)
            mover.GetType().GetField("pathCorners", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(mover, corners);
    }

    //Mirroring 
    private static int[,] BuildFullMap(int[,] q)
    {
        int r = q.GetLength(0);
        int c = q.GetLength(1);

        //Horizontal mirror
        int rightCols = c;
        int topCols = 2 * c;
        int[,] top = new int[r, topCols];

        for (int y = 0; y < r; y++)
        {
            // Copy left half
            for (int x = 0; x < c; x++)
                top[y, x] = q[y, x];

            // Mirror horizontally
            for (int i = 0; i < rightCols; i++)
            {
                int srcX = c - 1 - i;
                int dstX = c + i;
                top[y, dstX] = q[y, srcX];
            }
        }

        //Vertical mirror
        int bottomRows = r - 1;             
        int fullRows = r + bottomRows;
        int[,] full = new int[fullRows, topCols];

        // Copy top
        for (int y = 0; y < r; y++)
            for (int x = 0; x < topCols; x++)
                full[y, x] = top[y, x];

        // Mirror bottom
        for (int i = 1; i <= bottomRows; i++)
        {
            int srcY = r - 1 - i;
            int dstY = r + i -1;

            for (int x = 0; x < topCols; x++)
                full[dstY, x] = top[srcY, x];
        }

        return full;
    }

    //Generation
    private void Generate(int[,] map)
    {
        int rows = map.GetLength(0);
        int cols = map.GetLength(1);

        float width = cols * tileSize;
        float height = rows * tileSize;
        Vector2 origin = new Vector2(
                spawnOrigin.x + tileSize * 0.5f,
                spawnOrigin.y - tileSize * 0.5f
            );

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                int id = map[y, x];
                Vector3 world = new Vector3(origin.x + x * tileSize, origin.y - y * tileSize, 0f);

                // Floor under walkable tiles
                if ((id == 0) && floorPrefab)
                    Instantiate(floorPrefab, world, Quaternion.identity, _root);

                switch (id)
                {
                    case 0:
                        if (floorPrefab)
                            Instantiate(floorPrefab, world, Quaternion.identity, _root); break;

                    case 5:
                        if (pelletPrefab) Instantiate(pelletPrefab, world, Quaternion.identity, _root);
                        break;

                    case 6:
                        if (powerPelletPrefab) Instantiate(powerPelletPrefab, world, Quaternion.identity, _root);
                        break;

                    case 1: // outside corner
                        {
                            int m = NeighborMaskFamily(map, x, y);
                            float rot = CornerRotationRobust(map, x, y, m);
                            if (outsideCornerPrefab) Instantiate(outsideCornerPrefab, world, Quaternion.Euler(0, 0, rot), _root);
                            break;
                        }
                    case 2: // outside wall
                        {
                            int m = NeighborMaskFamily(map, x, y);
                            float rot = WallRotationRobust(m);
                            if (outsideWallPrefab) Instantiate(outsideWallPrefab, world, Quaternion.Euler(0, 0, rot), _root);
                            break;
                        }
                    case 3: // inside corner
                        {
                            int m = NeighborMaskFamily(map, x, y);
                            float rot = CornerRotationRobust(map, x, y, m);
                            if (insideCornerPrefab) Instantiate(insideCornerPrefab, world, Quaternion.Euler(0, 0, rot), _root);
                            break;
                        }
                    case 4: // inside wall
                        {
                            int m = NeighborMaskFamily(map, x, y);
                            float rot = WallRotationRobust(m);
                            if (insideWallPrefab) Instantiate(insideWallPrefab, world, Quaternion.Euler(0, 0, rot), _root);
                            break;
                        }
                    case 7: // T junction
                        {
                            var rot = TJunctionRotation(map, x, y);
                            Instantiate(tJunctionPrefab, world, rot, _root);
                            break;
                        }
                    case 8: // ghost gate (assume inside network)
                        {
                            int m = NeighborMaskFamily(map, x, y);
                            float rot = GateRotationFromMask(m);
                            if (ghostGatePrefab) Instantiate(ghostGatePrefab, world, Quaternion.Euler(0, 0, rot), _root);
                            break;
                        }

                    default:
                        Debug.LogWarning($"[LevelGenerator] Unknown id {id} at ({x},{y})");
                        break;
                }
            }
        }
    }

    private enum TileFamily { Outside, Inside }

    // Build a neighbor mask but ONLY for the given family to avoid cross-family pollution.
    // Bits: Up=1, Right=2, Down=4, Left=8
    private static int NeighborMaskFamily(int[,] map, int x, int y)
    {
        int rows = map.GetLength(0);
        int cols = map.GetLength(1);

        bool Conn(int id)
        {
            return id == 1 || id == 2 || id == 7 || id == 3 || id == 4 || id == 8; 
        }

        int m = 0;
        if (y > 0 && Conn(map[y - 1, x])) m |= 1;
        if (x < cols - 1 && Conn(map[y, x + 1])) m |= 2;
        if (y < rows - 1 && Conn(map[y + 1, x])) m |= 4;
        if (x > 0 && Conn(map[y, x - 1])) m |= 8;
        return m;
    }

    private static float CornerRotationRobust(int[,] map, int x, int y, int m)
    {
        // If all 4 sides are connected, decide by which opposite diagonal is OPEN.
        if (PopCount4(m) == 4)
        {
            int rows = map.GetLength(0);
            int cols = map.GetLength(1);
            bool Conn(int id) => id == 1 || id == 2 || id == 7 || id == 3 || id == 4 || id == 8;

            bool UL = (x > 0 && y > 0) && Conn(map[y - 1, x - 1]);
            bool UR = (x < cols - 1 && y > 0) && Conn(map[y - 1, x + 1]);
            bool DR = (x < cols - 1 && y < rows - 1) && Conn(map[y + 1, x + 1]);
            bool DL = (x > 0 && y < rows - 1) && Conn(map[y + 1, x - 1]);

            if (!UL) return 180f;    
            if (!UR) return 90f;  
            if (!DR) return 0;  
            if (!DL) return 270f;   

            // All diagonals filled too → deterministic fallback
            return 0f;
        }

        // Adjacent-pair checks (Up=1, Right=2, Down=4, Left=8)
        if ((m & 2) != 0 && (m & 4) != 0) return 0f;    // Right + Down  
        if ((m & 4) != 0 && (m & 8) != 0) return 270f;  // Down + Left   
        if ((m & 8) != 0 && (m & 1) != 0) return 180f;  // Left + Up     
        if ((m & 1) != 0 && (m & 2) != 0) return 90f;   // Up + Right    

        return 0f;
    }

    // Wall default assumed horizontal at 0°.
    private static float WallRotationRobust(int m)
    {
        int horiz = ((m & 2) != 0 ? 1 : 0) + ((m & 8) != 0 ? 1 : 0);
        int vert = ((m & 1) != 0 ? 1 : 0) + ((m & 4) != 0 ? 1 : 0);
        return (vert > horiz) ? 90f : 0f;
    }

    private static Quaternion TJunctionRotation(int[,] map, int x, int y)
    {
        int rows = map.GetLength(0);
        int cols = map.GetLength(1);

        int m = 0;
        bool Conn(int id) => id == 1 || id == 2 || id == 7 || id == 3 || id == 4 || id == 8;

        if (y > 0 && Conn(map[y - 1, x])) m |= 1;
        if (x < cols - 1 && Conn(map[y, x + 1])) m |= 2;
        if (y < rows - 1 && Conn(map[y + 1, x])) m |= 4;
        if (x > 0 && Conn(map[y, x - 1])) m |= 8;
        bool u = (m & 1) != 0, r = (m & 2) != 0, d = (m & 4) != 0, l = (m & 8) != 0;

        float zRot;
        if (l && r && d) zRot = 0f;      // L + R + D
        else if (l && r && u) zRot = 180f; // L + R + U
        else if (r && u && d) zRot = 90f;  // R + U + D
        else zRot = 270f;                  // U + D + L

        Quaternion rot = Quaternion.Euler(0f, 0f, zRot);

        if (x >= cols / 2 && zRot != 90f && zRot != 270f) rot *= Quaternion.Euler(0f, 180f, 0f); // right half
        if (y >= rows / 2 && zRot != 0f && zRot != 180f) rot *= Quaternion.Euler(180f, 0f, 0f); // bottom half

        return rot;
    }


    // Gate: default 0° horizontal; rotate 90° if vertical is stronger.
    private static float GateRotationFromMask(int m)
    {
        int horiz = ((m & 2) != 0 ? 1 : 0) + ((m & 8) != 0 ? 1 : 0);
        int vert = ((m & 1) != 0 ? 1 : 0) + ((m & 4) != 0 ? 1 : 0);
        return (vert > horiz) ? 90f : 0f;
    }

    private static int PopCount4(int v)
    {
        int c = 0;
        if ((v & 1) != 0) c++;
        if ((v & 2) != 0) c++;
        if ((v & 4) != 0) c++;
        if ((v & 8) != 0) c++;
        return c;
    }

    private void FitCameraTo(int[,] map)
    {
        var cam = Camera.main;
        if (cam == null || !cam.orthographic) return;

        int rows = map.GetLength(0);
        int cols = map.GetLength(1);

        float width = cols * tileSize;
        float height = rows * tileSize;

        // Center point of the map based on top-left spawn origin
        Vector2 center = new Vector2(
            spawnOrigin.x + width * 0.5f,
            spawnOrigin.y - height * 0.5f
        );

        // Move camera to map center
        cam.transform.position = new Vector3(center.x, center.y, -10f);

        // Adjust zoom
        float halfW = width * 0.5f;
        float halfH = height * 0.5f;
        float sizeByHeight = halfH + cameraPadding;
        float sizeByWidth = (halfW + cameraPadding) / Mathf.Max(0.0001f, cam.aspect);
        cam.orthographicSize = Mathf.Max(sizeByHeight, sizeByWidth);
    }
}
