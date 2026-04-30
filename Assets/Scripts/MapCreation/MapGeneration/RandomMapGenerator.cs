using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Text;

public class RandomMapGenerator : MonoBehaviour
{

    [SerializeField]
    int RandomMapAmount = 50;

    private List<Map> geoArchive = new List<Map>();
    private List<Map> furnArchive = new List<Map>();
    private List<Map> enemArchive = new List<Map>();
    void Start()
    {
        GenerateRandomMaps(RandomMapAmount);
    }

    //uses same hiarchical apporach as MAP-elites, but just creates n amount of maps randomly for each layer, 
    //with no mutations and saving them all, regardless of behavior duplicates.
    void GenerateRandomMaps(int amount)
    {
        for(int i = 0; i<amount; i++)
        {
            var map = new Map();
            GeometryGenerator.CreateMapGeometry(map);
            GeometryGenerator.BuildRoomTopology(map);
            var (gFitness, gBehavior) = GeoFitAndBehav.GetGeoFitnessAndBehavior(map);
            map.geoBehavior = new Vector2Int(gBehavior, 0);
            map.geoFitness = gFitness;
            ObjectPlacementGenerator.CreateLootOnMap(map);
            ObjectPlacementGenerator.CreateObstaclesOnMap(map);
            var (lFitness, lBehavior) = FurnFitAndBehav.GetFurnFitnessAndBehavior(map);
            map.furnBehavior = new Vector2Int(lBehavior.lootDensity, lBehavior.obstacleDensity);
            map.furnFitness = lFitness;
            ObjectPlacementGenerator.CreateEnemiesOnMap(map);
            var (eFitness, eBehavior) = EnemFitAndBehav.GetEnemyFitnessAndBehavior(map);
            map.enemyBehavior = new Vector2Int(eBehavior.enemyType, eBehavior.difficulty);
            map.enemFitness = eFitness;
            enemArchive.Add(map);
        }

        MapJsonExporter.SaveMaps(enemArchive, "Random_MapsUpdated.json");
    }
    



}
