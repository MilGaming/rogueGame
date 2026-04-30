using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public class RandomMapGeneratorEliteStyle : MonoBehaviour
{

    [SerializeField]
    int RandomMapAmount = 50;

    protected Dictionary<Vector2, Map> geoArchive = new Dictionary<Vector2, Map>();
    protected Dictionary<(Vector2, Vector2), Map> furnArchive = new Dictionary<(Vector2, Vector2), Map>();
    protected Dictionary<(Vector2, Vector2, Vector2), Map> enemArchive = new Dictionary<(Vector2, Vector2, Vector2), Map>();

    [SerializeField] protected TrainingLogger trainingLogger;

   [ThreadStatic] private static System.Random _rng;
    private static System.Random Rng => _rng ??= new System.Random();
    void Start()
    {
        GenerateGeometry(RandomMapAmount);
        GenerateFurnishing(RandomMapAmount);
        GenerateEnemies(RandomMapAmount);
        MapJsonExporter.SaveMaps(enemArchive.Values.ToList(), "Random_MapsUpdated.json");
    }

    //uses same hiarchical apporach as MAP-elites, but just creates n amount of maps randomly for each layer, 
    //with no mutations and saving them all, regardless of behavior duplicates.

    void GenerateGeometry(int amount)
    {
        var safeArchive = new ConcurrentDictionary<Vector2, Map>();
        int completedIterations = 0;

        var options = new System.Threading.Tasks.ParallelOptions
        {
            MaxDegreeOfParallelism = System.Environment.ProcessorCount - 2 // leave some cores free
        };

        System.Threading.Tasks.Parallel.For(0, amount, options, i =>
        {
            var map = new Map();
            GeometryGenerator.CreateMapGeometry(map);
            GeometryGenerator.BuildRoomTopology(map);
            var (gFitness, gBehavior) = GeoFitAndBehav.GetGeoFitnessAndBehavior(map);
            map.geoBehavior = new Vector2Int(gBehavior, 0);
            map.geoFitness = gFitness;

            safeArchive.AddOrUpdate(
                map.geoBehavior,
                map,
                (key, existing) => map.CombinedFitness > existing.CombinedFitness ? map : existing
            );



            int completed = System.Threading.Interlocked.Increment(ref completedIterations);
            if (trainingLogger != null && completed % trainingLogger.LogEveryNIterations == 0)
                trainingLogger.LogGeometry(completed, new Dictionary<Vector2, Map>(safeArchive));
        });

        foreach (var kvp in safeArchive)
            geoArchive[kvp.Key] = kvp.Value;
    }

    void GenerateFurnishing(int amount)
    {
        int completedFurn = 0;
        var safeArchive = new ConcurrentDictionary<(Vector2, Vector2), Map>();

        var options = new System.Threading.Tasks.ParallelOptions
        {
            MaxDegreeOfParallelism = System.Environment.ProcessorCount - 2
        };

        System.Threading.Tasks.Parallel.For(0, amount, options, i =>
        {
            Map parent = SelectRandom(new Dictionary<Vector2, Map>(geoArchive));
            var child = parent.Clone();
            ObjectPlacementGenerator.CreateLootOnMap(child);
            ObjectPlacementGenerator.CreateObstaclesOnMap(child);
            var (gFitness, gBehavior) = GeoFitAndBehav.GetGeoFitnessAndBehavior(child);
            child.geoBehavior = new Vector2Int(gBehavior, 0);
            child.geoFitness = gFitness;
            var (lFitness, lBehavior) = FurnFitAndBehav.GetFurnFitnessAndBehavior(child);
            child.furnBehavior = new Vector2Int(lBehavior.lootDensity, lBehavior.obstacleDensity);
            child.furnFitness = lFitness;

            safeArchive.AddOrUpdate(
                (child.geoBehavior, child.furnBehavior),
                child,
                (key, existing) => child.CombinedFitness > existing.CombinedFitness ? child : existing
            );

            int completed = System.Threading.Interlocked.Increment(ref completedFurn);
            if (trainingLogger != null && completed % trainingLogger.LogEveryNIterations == 0)
                trainingLogger.LogFurnishing(completed, new Dictionary<(Vector2, Vector2), Map>(safeArchive));
        });

        foreach (var kvp in safeArchive)
            furnArchive[kvp.Key] = kvp.Value;
    }

    void GenerateEnemies(int amount)
    {
        int completedEnemies = 0;

        var safeArchive = new ConcurrentDictionary<(Vector2, Vector2, Vector2), Map>();

        var options = new System.Threading.Tasks.ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Max(1, System.Environment.ProcessorCount - 2)
        };

        System.Threading.Tasks.Parallel.For(0, amount, options, i =>
        {
            Map parent = SelectRandom(new Dictionary<(Vector2, Vector2), Map>(furnArchive));
            var child = parent.Clone();
            var (gFitness, gBehavior) = GeoFitAndBehav.GetGeoFitnessAndBehavior(child);
            child.geoBehavior = new Vector2Int(gBehavior, 0);
            child.geoFitness = gFitness;
            var (lFitness, lBehavior) = FurnFitAndBehav.GetFurnFitnessAndBehavior(child);
            child.furnBehavior = new Vector2Int(lBehavior.lootDensity, lBehavior.obstacleDensity);
            child.furnFitness = lFitness;
            ObjectPlacementGenerator.CreateEnemiesOnMap(child);
            var (eFitness, eBehavior) = EnemFitAndBehav.GetEnemyFitnessAndBehavior(child);
            child.enemyBehavior = new Vector2Int(eBehavior.enemyType, eBehavior.difficulty);
            child.enemFitness = eFitness;

            safeArchive.AddOrUpdate(
                (child.geoBehavior, child.furnBehavior, child.enemyBehavior),
                child,
                (key, existing) =>
                    child.CombinedFitness > existing.CombinedFitness
                        ? child
                        : existing
            );

            int completed = System.Threading.Interlocked.Increment(ref completedEnemies);

            if (trainingLogger != null &&
                completed % trainingLogger.LogEveryNIterations == 0)
            {
                trainingLogger.LogEnemies(
                    completed,
                    new Dictionary<(Vector2, Vector2, Vector2), Map>(safeArchive)
                );
            }
        });
        foreach (var kvp in safeArchive)
        {
            enemArchive[kvp.Key] = kvp.Value;
        }
    }


    protected static Map SelectRandom<TKey>(Dictionary<TKey, Map> archive)
    {
        var list = archive.Values.ToList();
        return list[Rng.Next(0, list.Count)];
    }



}
