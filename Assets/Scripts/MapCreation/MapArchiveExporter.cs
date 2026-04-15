using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class MapListWrapper
{
    public List<Map> maps = new();
}

public static class MapJsonSaveSystem
{
    public static void SaveMaps(List<Map> maps, string fileName = "maps.json")
    {
        if (maps == null)
        {
            Debug.LogError("SaveMaps failed: maps list is null.");
            return;
        }

        // Prepare each map before serializing
        foreach (var map in maps)
        {
            if (map != null)
                MapHelpers.PrepareMapForSave(map);
        }

        var wrapper = new MapListWrapper
        {
            maps = maps
        };

        string json = JsonUtility.ToJson(wrapper, true);
        string path = Path.Combine(Application.persistentDataPath, fileName);

        File.WriteAllText(path, json);

        Debug.Log($"Saved {maps.Count} maps to: {path}");
    }

    public static List<Map> LoadMaps(string fileName = "maps.json")
    {
        string path = Path.Combine(Application.persistentDataPath, fileName);

        if (!File.Exists(path))
        {
            Debug.LogWarning($"LoadMaps: file not found at {path}");
            return new List<Map>();
        }

        string json = File.ReadAllText(path);

        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogWarning("LoadMaps: file was empty.");
            return new List<Map>();
        }

        MapListWrapper wrapper = JsonUtility.FromJson<MapListWrapper>(json);

        if (wrapper == null || wrapper.maps == null)
        {
            Debug.LogWarning("LoadMaps: failed to deserialize maps.");
            return new List<Map>();
        }

        foreach (var map in wrapper.maps)
        {
            if (map != null)
                MapHelpers.RebuildAfterLoad(map);
        }

        Debug.Log($"Loaded {wrapper.maps.Count} maps from: {path}");
        return wrapper.maps;
    }
}