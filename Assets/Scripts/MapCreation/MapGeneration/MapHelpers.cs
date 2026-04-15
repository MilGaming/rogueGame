using System;
using System.Collections.Generic;
using UnityEngine;

public static class MapHelpers
{
    public static readonly Dictionary<EnemyType, float> EnemyCosts = new()
    {
        { EnemyType.Melee, 0.5f },
        { EnemyType.Ranged, 1f },
        { EnemyType.Bomber, 0.5f },
        { EnemyType.Assassin, 1f },
        { EnemyType.Guardian, 2f }
    };
    public static readonly EnemyType[] EnemyTypes =
    (EnemyType[])Enum.GetValues(typeof(EnemyType));
    public static readonly LootType[] LootTypes =
    (LootType[])Enum.GetValues(typeof(LootType));
    public static readonly ObstacleType[] ObstacleTypes =
    (ObstacleType[])Enum.GetValues(typeof(ObstacleType));

    // Saving rooms multiple times would waste much space, so we change to indices for saving
    public static void PrepareMapForSave(Map map)
    {
        AssignRoomIndices(map);

        foreach (var connection in map.connections)
        {
            if (connection.roomA != null)
                connection.roomAIndex = connection.roomA.roomIndex;

            if (connection.roomB != null)
                connection.roomBIndex = connection.roomB.roomIndex;
        }

        map.startRoomIndex = map.startRoom != null ? map.startRoom.roomIndex : -1;
        map.endRoomIndex = map.endRoom != null ? map.endRoom.roomIndex : -1;

        map.mainPathRoomIndices.Clear();
        foreach (var room in map.mainPathRooms)
        {
            if (room != null)
                map.mainPathRoomIndices.Add(room.roomIndex);
        }
    }
    public static void RebuildAfterLoad(Map map)
    {
        foreach (var room in map.rooms)
        {
            room.tileSet = new HashSet<Vector2Int>();
            foreach (var tile in room.tiles)
                room.tileSet.Add(tile.pos);
        }

        foreach (var connection in map.connections)
        {
            connection.roomA = map.rooms[connection.roomAIndex];
            connection.roomB = map.rooms[connection.roomBIndex];
        }

        map.startRoom = map.startRoomIndex >= 0 ? map.rooms[map.startRoomIndex] : null;
        map.endRoom = map.endRoomIndex >= 0 ? map.rooms[map.endRoomIndex] : null;

        map.mainPathRooms = new List<Room>();
        foreach (int idx in map.mainPathRoomIndices)
        {
            map.mainPathRooms.Add(map.rooms[idx]);
        }
    }

    public static void AssignRoomIndices(Map map)
    {
        for (int i = 0; i < map.rooms.Count; i++)
        {
            map.rooms[i].roomIndex = i;
        }
    }
}


[Serializable]
public class Map
{
    public List<Room> rooms = new ();
    public List<RoomConnection> connections = new();
    [NonSerialized] public Room startRoom;
    [NonSerialized] public Room endRoom;
    [NonSerialized] public List<Room> mainPathRooms = new();

    // Indices for reduced json space
    public int startRoomIndex = -1;
    public int endRoomIndex = -1;
    public List<int> mainPathRoomIndices = new();

    // For map elites
    public float geoFitness = 0f;
    public float enemFitness = 0f;
    public float furnFitness = 0f;
    public float CombinedFitness => geoFitness + enemFitness + furnFitness;
    public Vector2Int geoBehavior = new Vector2Int();
    public Vector2Int furnBehavior = new Vector2Int();
    public Vector2Int enemyBehavior = new Vector2Int();
    public Map() { }
    public Map(Map other)
    {
        rooms = new List<Room>(other.rooms);
        connections = new List<RoomConnection>(other.connections);
        startRoom = other.startRoom;
        endRoom = other.endRoom;
        mainPathRooms = new List<Room>(other.mainPathRooms);
    }
    public Map Clone() => new Map(this);

    public int chunkCount()
    {
        int i = 0;

        foreach (Room room in rooms)
        {
            foreach (RoomChunk chunk in room.chunks)
            {
                i++;
            }
        }
        return i;
    }
    public int enemyCount()
    {
        int i = 0;

        foreach (Room room in rooms)
        {
            i += room.enemies.Count;
        }
        return i;
    }

    public int lootCount()
    {
        int i = 0;

        foreach (Room room in rooms)
        {
            i += room.loot.Count;
        }
        return i;
    }

    public int obstacleCount()
    {
        int i = 0;

        foreach (Room room in rooms)
        {
            i += room.obstacles.Count;
        }
        return i;
    }

    public float TotalCorridorLength()
    {
        if (connections == null)
            return 0f;

        float total = 0f;

        foreach (var connection in connections)
        {
            total += connection.length;
        }

        return total;
    }

    public float TotalTileCount()
    {
        if (rooms == null)
            return 0f;

        float total = 0f;

        foreach (var room in rooms)
        {
            total += room.tiles.Count;
        }

        return total;
    }

    public List<GridEntry> GetAllEnemies()
    {
        var allEnemies = new List<GridEntry>();

        if (rooms == null)
            return allEnemies;

        foreach (var room in rooms)
        {
            if (room?.enemies == null)
                continue;

            allEnemies.AddRange(room.enemies);
        }

        return allEnemies;
    }
}
// The finished encounter rooms
[Serializable]
public class Room
{
    // index for cheaper saves
    public int roomIndex = -1;

    public List<RoomChunk> chunks = new();
    // for faster lookup
    [NonSerialized] public HashSet<Vector2Int> tileSet = new();
    public List<GridEntry> tiles = new();
    public List<GridEntry> enemies = new();
    public List<GridEntry> loot = new();
    public List<GridEntry> obstacles = new();
    public Vector2Int position;
    public bool onMainPath = false;
    public int orderIndex = 0;
    public float sizeModifier = 0f;
    public float orderModifier = 0f;
    public float enemyBudget = 0f;
    public float enemyBudgetUsed = 0f;
    public float lootBudget = 0f;
    public float lootBudgetUsed = 0f;
    public float obstacleBudget = 0f;
    public float obstacleBudgetUsed = 0f;

    public Vector2Int entryTile = new Vector2Int();
    public Vector2Int exitTile = new Vector2Int();

    public Room(Vector2Int pos)
    {
        chunks = new List<RoomChunk>();
        this.position = pos;
    }

    public float GetLootTypeShare(LootType type)
    {
        if (loot == null || loot.Count == 0)
            return 0f;

        int count = 0;

        foreach (var item in loot)
        {
            if (item.type == (int)type)
                count++;
        }

        return (float)count / loot.Count;
    }
}

// The squares the rooms are build from.
[Serializable]
public class RoomChunk
{
    public Vector2Int position;
    public Vector2Int size;

    public int XMin => position.x;
    public int YMin => position.y;
    public int XMax => position.x + size.x - 1;
    public int YMax => position.y + size.y - 1;

    public bool Overlaps(RoomChunk other, int clearance = 1)
    {
        bool xOverlap =
            (XMin - clearance) <= other.XMax &&
            (XMax + clearance) >= other.XMin;

        bool yOverlap =
            (YMin - clearance) <= other.YMax &&
            (YMax + clearance) >= other.YMin;

        return xOverlap && yOverlap;
    }
}

[Serializable]
public class RoomConnection
{
    [NonSerialized] public Room roomA;
    [NonSerialized] public Room roomB;

    public int roomAIndex;
    public int roomBIndex;

    public Vector2Int tileA;
    public Vector2Int tileB;

    public float length;
}

[Serializable]
public class GridEntry
{
    public Vector2Int pos;
    public int type;

    public GridEntry() { }

    public GridEntry(Vector2Int pos, int type)
    {
        this.pos = pos;
        this.type = type;
    }
}

public enum EnemyType
{
    Melee,
    Ranged,
    Bomber,
    Assassin,
    Guardian
}

public enum LootType
{
    Powerup,
    Health
}

public enum TerrainType
{
    Standard,
    Decor1,
    Decor2,
    Path
}

public enum ObstacleType
{
    Spike,
    Pillar
}

public enum WallType
{
    WallSouth = 0,
    WallEast = 1,
    WallNorth = 2,
    WallWest = 3,
    CornerSouth = 4,
    CornerEast = 5,
    CornerNorth = 6,
    CornerWest = 7
}

public enum MapStyle
{
    Ground = 0,
    Farm = 1,
    Forest = 2
}






