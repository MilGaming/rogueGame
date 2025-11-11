using System.Collections.Generic;
using UnityEngine;

public class MapElite : MonoBehaviour
{
    [Header("References")]
    [SerializeField] MapGeneratorElites mapGeneratorPrefab; // uses your working elites generator
    [SerializeField] MapInstantiator mapInstantiator;       // your ORIGINAL instantiator (unchanged)

    [Header("MAP-Elites Parameters")]
    [SerializeField] int totalIterations = 50;       // I
    [SerializeField] int initialRandomSolutions = 20; // G

    private List<MapCandidate> archive = new List<MapCandidate>();

    void Start()
    {
        RunMapElites();
    }

    void RunMapElites()
    {
        archive.Clear();

        for (int iter = 1; iter <= totalIterations; iter++)
        {
            MapCandidate candidate;

            if (iter <= initialRandomSolutions)
            {
                // x' <- random_solution()
                candidate = GenerateRandomCandidate();
            }
            else
            {
                // x  <- random_selection(X)
                MapCandidate parent = SelectParent();

                // x' <- random_variation(x)
                candidate = MutateFunction(parent);
            }

            // b' <- feature_descriptor(x')
            Vector2 behavior = FeatureDescriptorFunction(candidate);

            // p' <- performance(x')
            candidate.fitness = EvaluateFitnessFunction(candidate, behavior);

            // store candidate (later this is where binning/replacement would go)
            InsertIntoArchive(candidate, behavior);

            Debug.Log($"Iteration {iter}/{totalIterations} - fitness: {candidate.fitness}, enemies: {behavior.x}, furnishing: {behavior.y}");
        }

        // Only here do we actually instantiate the map visually
        if (archive.Count > 0)
        {
            MapCandidate best = archive[0];
            for (int i = 1; i < archive.Count; i++)
            {
                if (archive[i].fitness > best.fitness)
                    best = archive[i];
            }

            Debug.Log("Spawning best map via original MapInstantiator.");
            mapInstantiator.makeMap(best.mapData);
        }
    }

    MapCandidate GenerateRandomCandidate()
    {
        // Instantiate a temporary generator that only builds mapArray (no visuals)
        MapGeneratorElites generator = Instantiate(mapGeneratorPrefab);
        generator.GenerateFullMap(); // your existing method on MapGeneratorElites
        MapCandidate candidate = new MapCandidate(generator.mapArray);
        Destroy(generator.gameObject);
        return candidate;
    }

    MapCandidate SelectParent()
    {
        if (archive.Count == 0)
            return GenerateRandomCandidate();

        int index = Random.Range(0, archive.Count);
        return archive[index];
    }

    Vector2 FeatureDescriptorFunction(MapCandidate candidate)
    {
        int enemyCount = 0;
        int furnishingCount = 0;

        int width = candidate.mapData.GetLength(0);
        int height = candidate.mapData.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int val = candidate.mapData[x, y];

                // according to your MapInstantiator:
                // 3–5 = furnishing, 6–7 = enemies
                if (val >= 6 && val <= 7)
                    enemyCount++;
                else if (val >= 3 && val <= 5)
                    furnishingCount++;
            }
        }

        return new Vector2(enemyCount, furnishingCount);
    }

    MapCandidate MutateFunction(MapCandidate parent)
    {
        // Mutate map layout here
        // Mutate enemies here
        // Mutate furnishing here

        // For now, just create a new random candidate as a placeholder
        return GenerateRandomCandidate();
    }

    float EvaluateFitnessFunction(MapCandidate candidate, Vector2 behavior)
    {
        // Simple placeholder: prefer more enemies, a bit of furnishing
        float enemyCount = behavior.x;
        float furnishingCount = behavior.y;
        return enemyCount + furnishingCount * 0.25f;
    }

    void InsertIntoArchive(MapCandidate candidate, Vector2 behavior)
    {
        // Later: use `behavior` to choose bins and keep only best per bin
        archive.Add(candidate);
    }
}

public class MapCandidate
{
    public int[,] mapData;
    public float fitness;

    public MapCandidate(int[,] map)
    {
        mapData = map;
        fitness = 0f;
    }
}
