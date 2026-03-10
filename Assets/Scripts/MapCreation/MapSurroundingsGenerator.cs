using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapSurroundingsGenerator : MonoBehaviour
{
    [Header("Tilemaps")]
    [SerializeField] private Tilemap tilemapBase;
    [SerializeField] private Tilemap tilemapDecour;

    [Header("Forest Tiles")]
    [SerializeField] private TileBase[] tilesForestBase;
    [SerializeField] private TileBase[] tilesForestDecour;

    [Header("Tree Prefabs")]
    [SerializeField] private List<GameObject> treePrefabs = new List<GameObject>();

    [Header("Settings")]
    [SerializeField] private int padding = 8;
    [SerializeField, Range(0, 100)] private int decorChance = 15;
    [SerializeField, Range(0, 100)] private int treeChance = 10;
    [SerializeField] private int minTreeDistanceFromMap = 5;

    private readonly List<GameObject> spawnedTrees = new List<GameObject>();


    public void ClearSurroundings()
    {
        foreach (GameObject tree in spawnedTrees)
        {
            if (tree != null)
                Destroy(tree);
        }

        spawnedTrees.Clear();
    }

    public void GenerateSurroundings(int[,] map)
    {
        if (map == null)
            return;

        if (tilemapBase == null || tilemapDecour == null)
            return;

        if (tilesForestBase == null || tilesForestBase.Length == 0)
            return;

        ClearSurroundings();

        if (!TryGetUsedBounds(map, out int minX, out int maxX, out int minY, out int maxY))
            return;

        for (int x = minX - padding; x <= maxX + padding; x++)
        {
            for (int y = minY - padding; y <= maxY + padding; y++)
            {
                // Skip only actual map cells that are used
                if (x >= 0 && x < map.GetLength(0) &&
                    y >= 0 && y < map.GetLength(1) &&
                    map[x, y] != 0)
                {
                    continue;
                }

                Vector3Int cell = new Vector3Int(x, y, 0);

                // Do not overwrite already placed tiles
                if (tilemapBase.GetTile(cell) != null)
                    continue;

                // Place forest ground
                TileBase baseTile = tilesForestBase[Random.Range(0, tilesForestBase.Length)];
                tilemapBase.SetTile(cell, baseTile);

                // Place forest decor
                if (tilesForestDecour != null &&
                    tilesForestDecour.Length > 0 &&
                    Random.Range(0, 100) < decorChance)
                {
                    TileBase decorTile = tilesForestDecour[Random.Range(0, tilesForestDecour.Length)];
                    tilemapDecour.SetTile(cell, decorTile);
                }

                // Place trees
                int distance = GetDistanceFromUsedArea(x, y, minX, maxX, minY, maxY);

                if (distance >= minTreeDistanceFromMap &&
                    treePrefabs != null &&
                    treePrefabs.Count > 0 &&
                    Random.Range(0, 100) < treeChance)
                {
                    Vector3 worldPos = tilemapBase.GetCellCenterWorld(cell);
                    GameObject prefab = treePrefabs[Random.Range(0, treePrefabs.Count)];
                    GameObject tree = Instantiate(prefab, worldPos, Quaternion.identity, transform);
                    spawnedTrees.Add(tree);
                }
            }
        }
    }

    private bool TryGetUsedBounds(int[,] map, out int minX, out int maxX, out int minY, out int maxY)
    {
        minX = int.MaxValue;
        maxX = int.MinValue;
        minY = int.MaxValue;
        maxY = int.MinValue;

        bool foundAny = false;

        for (int x = 0; x < map.GetLength(0); x++)
        {
            for (int y = 0; y < map.GetLength(1); y++)
            {
                if (map[x, y] == 0)
                    continue;

                foundAny = true;

                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
        }

        return foundAny;
    }

    private int GetDistanceFromUsedArea(int x, int y, int minX, int maxX, int minY, int maxY)
    {
        int dx = 0;
        if (x < minX) dx = minX - x;
        else if (x > maxX) dx = x - maxX;

        int dy = 0;
        if (y < minY) dy = minY - y;
        else if (y > maxY) dy = y - maxY;

        return Mathf.Max(dx, dy);
    }
}