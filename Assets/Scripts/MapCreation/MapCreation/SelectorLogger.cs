using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public class SelectorLogger : MonoBehaviour
{
    [Header("Logging")]
    [SerializeField] private bool enableLogging = true;
    [SerializeField] private int logEveryNIterations = 5000;
    [SerializeField] private string fileName = "training_log.csv";

    [Header("Behavior Bin Counts")]
    [SerializeField] private int geoBehaviorBins = 10;

    [SerializeField] private int furnLootBins = 3;
    [SerializeField] private int furnObstacleBins = 3;

    [SerializeField] private int enemyTypeBins = 126;
    [SerializeField] private int enemyDifficultyBins = 3;

    [SerializeField] private float fitnessThreshold = 0.8f;

    private string filePath;

    public int LogEveryNIterations => logEveryNIterations;

    private void Awake()
    {
        filePath = Path.Combine(Application.dataPath, fileName);
        InitializeFile();
    }

    private void InitializeFile()
    {
        if (!enableLogging) return;

        var sb = new StringBuilder();
        sb.AppendLine(
            "EnemyComposition, EnemyCompositionBehavior, DifficultyBehavior, DifferenceToOtherLevels, OpenessBehavior, LootDensityBehavior, ObstacleDensityBehavior, DifferenceToOtherLevels"
        );

        File.WriteAllText(filePath, sb.ToString());
        Debug.Log($"Training log initialized: {filePath}");
    }

    public void LogEnemyComp(Map map, float[] enemyComp, float difference)
    {
        if (!enableLogging) return;

        string comp = "";
        for (int i=0; i<enemyComp.Length; i++)
        {
            comp += enemyComp[i].ToString();
        }
        AppendEnemyRow(
            comp,
            map.enemyBehavior.x,
            map.enemyBehavior.y,
            difference,
            map.geoBehavior.x,
            map.furnBehavior.x,
            map.furnBehavior.y
        );
    }

    public void LogExplorationParameters(Map map, float difference)
    {
        if (!enableLogging) return;

        AppendExplorationRow(
            " ",
            map.enemyBehavior.x,
            map.enemyBehavior.y,
            map.geoBehavior.x,
            map.furnBehavior.x,
            map.furnBehavior.y,
            difference
        );
    }


    private bool ShouldLog(int iteration)
    {
        return iteration > 0 && iteration % logEveryNIterations == 0;
    }

    // Assumes bins are 1-based: 1..binCount
    private List<int> GetMissingBins(List<int> filledBins, int binCount)
    {
        HashSet<int> filledSet = new HashSet<int>(filledBins);

        return Enumerable.Range(0, binCount)
            .Where(bin => !filledSet.Contains(bin))
            .ToList();
    }

    private string ToPipeSeparated(List<int> values)
    {
        return string.Join("|", values);
    }

    public void AddExploreNotation()
    {
        var sb = new StringBuilder();
        sb.AppendLine(
            "OpenessBehavior, LootDensityBehavior, ObstacleDensityBehavior, DifferenceToOtherLevels"
        );
        File.AppendAllText(filePath, sb.ToString());
    }

    private void AppendEnemyRow(
        string enemyComp,
        int compBehavior,
        int difficultyBehavior,
        float difference,
        int geoBehavior,
        int lootDensityBehavior,
        int obstacleDensityBehavior
        )
    {
        string line =
            $"{enemyComp},{compBehavior},{difficultyBehavior}," +
            $"{difference},{geoBehavior},{lootDensityBehavior},{obstacleDensityBehavior}, {" "}";
            

        File.AppendAllText(filePath, line + Environment.NewLine);
    }

    private void AppendExplorationRow(
        string enemyComp,
        int compBehavior,
        int difficultyBehavior,
        int geoBehavior,
        int lootDensityBehavior,
        int obstacleDensityBehavior,
        float difference
        )
    {
        string line =
            $"{enemyComp},{compBehavior},{difficultyBehavior},{" "}" +
            $"{geoBehavior},{lootDensityBehavior},{obstacleDensityBehavior}," +
            $"{difference}";
            

        File.AppendAllText(filePath, line + Environment.NewLine);
    }
}