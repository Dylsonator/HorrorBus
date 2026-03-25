using System.Collections.Generic;
using UnityEngine;

public sealed class NameGenerator : MonoBehaviour
{
    [SerializeField]
    private string[] firstNames =
    {
        "Alex","Sam","Jamie","Taylor","Jordan","Casey","Morgan","Riley","Avery","Charlie",
        "Kai","Noah","Mia","Liam","Ethan","Zoe","Ella","Oscar","Freya","Holly","Aleksander",
        "Dylan"
    };

    [SerializeField]
    private string[] lastNames =
    {
        "Smith","Jones","Brown","Taylor","Williams","Davies","Evans","Wilson","Thomas","Johnson",
        "Roberts","Walker","Wright","Thompson","White","Hall","Green","King","Baker","Clarke",
        "Fliski", "Mikolajczyk"

    };

    [Header("Anomaly behaviour")]
    [SerializeField, Range(0f, 1f)] private float anomalyCopyHumanNameChance = 0.35f;
    [SerializeField] private int recentHumanNameMemory = 12;

    private readonly List<string> recentHumanNames = new();

    public string GenerateName(bool isAnomaly)
    {
        // Anomaly may copy a recent human name
        if (isAnomaly && recentHumanNames.Count > 0 && Random.value < anomalyCopyHumanNameChance)
        {
            return recentHumanNames[Random.Range(0, recentHumanNames.Count)];
        }

        string name = $"{firstNames[Random.Range(0, firstNames.Length)]} {lastNames[Random.Range(0, lastNames.Length)]}";

        if (!isAnomaly)
            RememberHumanName(name);

        return name;
    }

    private void RememberHumanName(string name)
    {
        recentHumanNames.Add(name);
        while (recentHumanNames.Count > recentHumanNameMemory)
            recentHumanNames.RemoveAt(0);
    }
}
