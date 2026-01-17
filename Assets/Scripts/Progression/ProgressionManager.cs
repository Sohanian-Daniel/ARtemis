using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AchievementProgressData
{
    public uint currentProgress;
}

[Serializable]
public class ProgressionData
{
    public Dictionary<string, AchievementProgressData> achievementData = new();
}

public class ProgressionManager : MonoBehaviour
{
    public static ProgressionManager Instance;

    public static string saveFileName = "save.art";
    public static string savePath;

    public static ProgressionData progressionData;

    public List<Achievement> achievements = new();

    private void Awake()
    {
        savePath = Application.persistentDataPath + "/" + saveFileName;

        if (Instance == null)
        {
            Instance = this;
            LoadProgression();
        }
        else
        {
            // If there is already an instance of this class, destroy this one
            Destroy(gameObject);
        }

        Debug.Log("ProgressionManager Awake");
    }

    public void LoadProgression()
    {
        if (System.IO.File.Exists(savePath))
        {
            string json = System.IO.File.ReadAllText(savePath);

            // Use Newtonsoft.Json to deserialize the JSON string into a ProgressionData object
            progressionData = JsonConvert.DeserializeObject<ProgressionData>(json);

            // Update Achievement data with the loaded data
            LoadAchievementData();
        }
        else
        {
            progressionData = new ProgressionData();

            SaveProgression();
        }
    }

    private void LoadAchievementData()
    {
        foreach (var achievement in achievements)
        {
            // If the advancement data does not exist, skip it
            if (!progressionData.achievementData.ContainsKey(achievement.name))
            {
                continue;
            }

            // Load the data for the advancement
            achievement.achievementData = progressionData.achievementData[achievement.name];
        }
    }

    public static void SaveProgression()
    {
        foreach (var achievement in Instance.achievements)
        {
            var data = achievement.achievementData;
            if (progressionData.achievementData.ContainsKey(achievement.name))
            {
                progressionData.achievementData[achievement.name] = data;
            }
            else
            {
                progressionData.achievementData.Add(achievement.name, data);
            }
        }

        string json = JsonConvert.SerializeObject(progressionData, Formatting.Indented);

        System.IO.File.WriteAllText(savePath, json);
    }

    private void OnDestroy()
    {
        SaveProgression();
    }
}