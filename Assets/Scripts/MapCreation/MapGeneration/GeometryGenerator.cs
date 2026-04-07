using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.PackageManager;
using UnityEngine;
using static UnityEditor.Recorder.OutputPath;

public static class GeometryGenerator
{
    // Create a fresh set of rooms and chunks for a map.
    public static Map CreateMapGeometry(Map map, int maxChunkSize = 15, int maxChunkAmount = 100)
    {
        map.rooms = new List<Room>();
        // How many chunks are we placing?
        int chunkAmount = UnityEngine.Random.Range(4, maxChunkAmount);

        for (int i = 0; i < chunkAmount; i++)
        {
            AddRandomRoomChunk(map, maxChunkSize);
        }

        return map;
    }

    // Randomly adds and removes chunks from a map
    public static Map MutateMapGeometry(Map map, float mutateSize = 0.2f, int maxChunkSize = 15)
    {
        // We add/remove an amount of chunks equal to 20% of existing
        int amountToMutate = (int)(map.chunkCount() * mutateSize);

        // Randomly either remove or add a chunk. Unless sizes too exterme.
        for (int i = 0; i < amountToMutate; i++) {
            bool addChunk = UnityEngine.Random.value < 0.5f;
            if (map.chunkCount() < 10) addChunk = true;
            else if (map.chunkCount() > 100) addChunk = false;

            if (addChunk)
            {
                AddRandomRoomChunk(map, maxChunkSize);
            }
            else {
                RemoveRandomRoomChunk(map);
            }
        }

        return map;

    }

    // Removes a random chunk
    public static void RemoveRandomRoomChunk(Map map)
    {
        if (map.rooms.Count == 0)
            return;

        // Pick random room
        Room room = map.rooms[Random.Range(0, map.rooms.Count)];

        if (room.chunks.Count == 0)
            return;

        // Pick random chunk
        int index = Random.Range(0, room.chunks.Count);
        room.chunks.RemoveAt(index);

        // If room becomes empty, remove it
        if (room.chunks.Count == 0)
        {
            map.rooms.Remove(room);
        }
    }

    // Makes a Random rect chunk and places it on the map, while ensuring grouping into rooms
    public static void AddRandomRoomChunk(Map map, int maxChunkSize) {
        // Make a random chunk of random size, shape and position
        Vector2Int chunkSize = new Vector2Int(
            UnityEngine.Random.Range(3, maxChunkSize),
            UnityEngine.Random.Range(3, maxChunkSize)
        );

        Vector2Int chunkPosition = new Vector2Int(
            UnityEngine.Random.Range(0, 100),
            UnityEngine.Random.Range(0, 100)
        );

        RoomChunk ourChunk = new RoomChunk
        {
            position = chunkPosition,
            size = chunkSize
        };

        // Find all rooms the chunk touches / overlaps
        var roomsWeAreIn = new List<Room>();
        foreach (var room in map.rooms)
        {
            foreach (var chunk in room.chunks)
            {
                if (ourChunk.Overlaps(chunk))
                {
                    roomsWeAreIn.Add(room);
                    break; //don't add same room multiple times
                }
            }
        }

        // If we are not in any rooms, become a new room
        if (roomsWeAreIn.Count == 0)
        {
            var newRoom = new Room(ourChunk.position);
            newRoom.chunks.Add(ourChunk);
            map.rooms.Add(newRoom);
        }
        else
        {
            // Merge all touched rooms into the first room
            Room mainRoom = roomsWeAreIn[0];
            mainRoom.chunks.Add(ourChunk);

            for (int r = 1; r < roomsWeAreIn.Count; r++)
            {
                Room otherRoom = roomsWeAreIn[r];

                foreach (var chunk in otherRoom.chunks)
                {
                    mainRoom.chunks.Add(chunk);
                }

                map.rooms.Remove(otherRoom);
            }
        }
    }

    // Adds main path, entry and exit tiles and order index to all rooms
    public static void BuildRoomTopology(Map map)
    {
        if (map.rooms == null || map.rooms.Count == 0)
            return;

        // 1. Find start + end (furthest pair)
        var (start, end) = FindFurthestRooms(map.rooms);

        map.startRoom = start;
        map.endRoom = end;

        // 2. Build main path (and mark entry/exit inline)
        var mainPath = BuildAndMarkMainPath(start, end, map.rooms);

        map.mainPathRooms = mainPath;

        // 3. Attach optional rooms
        AttachOptionalRooms(map.rooms, mainPath);
    }

    public static List<Room> BuildAndMarkMainPath(Room start, Room end, List<Room> rooms)
    {
        var path = new List<Room>();
        var visited = new HashSet<Room>();

        Room current = start;
        visited.Add(current);
        path.Add(current);

        // start entry
        current.entryTile = current.GetAnyTile();
        current.onMainPath = true;
        current.orderIndex = 1;

        int order = 1;

        while (current != end)
        {
            Room bestNext = null;
            int bestScore = int.MaxValue;
            Vector2Int bestExit = default;
            Vector2Int bestEntry = default;

            foreach (var candidate in rooms)
            {
                // Been here before
                if (visited.Contains(candidate))
                    continue;
                // Distance to candidate
                var (stepDist, exitTile, entryTile) = FindClosestTiles(current, candidate);
                // Distance from candidate to end
                var (endDist, _, _) = FindClosestTiles(candidate, end);

                // Score is combination of length to new room, and the amount gained towards end, here counting double 
                int score = endDist + stepDist * 2;

                if (score < bestScore)
                {
                    bestScore = score;
                    bestNext = candidate;
                    bestExit = exitTile;   // from current -> next
                    bestEntry = entryTile; // into next
                }
            }

            if (bestNext == null)
                break;

            // mark exit of current
            current.exitTile = bestExit;

            // move forward
            current = bestNext;
            visited.Add(current);
            path.Add(current);

            // mark entry of new room
            current.entryTile = bestEntry;

            order++;
            current.orderIndex = order;
            current.onMainPath = true;
        }

        // final room exit
        current.exitTile = current.GetAnyTile();

        return path;
    }

    public static void AttachOptionalRooms(List<Room> allRooms, List<Room> mainPath)
    {
        var mainSet = new HashSet<Room>(mainPath);

        foreach (var room in allRooms)
        {
            if (mainSet.Contains(room))
                continue;

            Room bestMain = null;
            int bestDist = int.MaxValue;
            Vector2Int bestEntry = default;

            foreach (var main in mainPath)
            {
                var (dist, entry, _) = FindClosestTiles(room, main);

                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestMain = main;
                    bestEntry = entry;
                }
            }

            room.entryTile = bestEntry;
            room.exitTile = FindFurthestTileInRoom(room, bestEntry);
        }
    }


    public static (int dist, Vector2Int aTile, Vector2Int bTile) FindClosestTiles(Room a, Room b)
    {
        int bestDist = int.MaxValue;
        Vector2Int bestA = default;
        Vector2Int bestB = default;

        foreach (var ca in a.chunks)
        {
            for (int ax = ca.XMin; ax <= ca.XMax; ax++)
            {
                for (int ay = ca.YMin; ay <= ca.YMax; ay++)
                {
                    var ta = new Vector2Int(ax, ay);

                    foreach (var cb in b.chunks)
                    {
                        for (int bx = cb.XMin; bx <= cb.XMax; bx++)
                        {
                            for (int by = cb.YMin; by <= cb.YMax; by++)
                            {
                                var tb = new Vector2Int(bx, by);

                                int d = Mathf.Abs(ta.x - tb.x) + Mathf.Abs(ta.y - tb.y);

                                if (d < bestDist)
                                {
                                    bestDist = d;
                                    bestA = ta;
                                    bestB = tb;
                                }
                            }
                        }
                    }
                }
            }
        }
        return (bestDist, bestA, bestB);
    }

    public static (Room a, Room b) FindFurthestRooms(List<Room> rooms)
    {
        int bestDist = -1;
        Room bestA = null;
        Room bestB = null;

        for (int i = 0; i < rooms.Count; i++)
        {
            for (int j = i + 1; j < rooms.Count; j++)
            {
                var (dist, _, _) = FindClosestTiles(rooms[i], rooms[j]);

                if (dist > bestDist)
                {
                    bestDist = dist;
                    bestA = rooms[i];
                    bestB = rooms[j];
                }
            }
        }

        return (bestA, bestB);
    }

}
