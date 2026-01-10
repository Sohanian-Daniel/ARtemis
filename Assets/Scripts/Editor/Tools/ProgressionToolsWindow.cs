using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ProgressionToolsWindow : EditorWindow
{
    public string SavePath;

    [MenuItem("Tools/Progression")]
    public static void ShowWindow()
    {
        GetWindow<ProgressionToolsWindow>("Progression");
    }

    private void OnEnable()
    {
        // Set the window size
        Vector2 windowSize = new Vector2(300, 100);
        minSize = windowSize;
        maxSize = windowSize;

        SavePath = Application.persistentDataPath + "/" + ProgressionManager.saveFileName;
    }

    private void OnGUI()
    {
        // Button to reset all progression data
        if (GUILayout.Button("Reset All Progression Data"))
        {
            ResetAllProgressionData();
        }

        // Button to open the save file
        if (GUILayout.Button("Open Save File Location"))
        {
            OpenSaveFile();
        }

        // Button to unlock all enhancements
        if (GUILayout.Button("Unlock All Enhancements"))
        {
            UnlockAll();
        }

    }

    private void ResetAllProgressionData()
    {
        if (System.IO.File.Exists(SavePath))
        {
            System.IO.File.Delete(SavePath);
            Debug.Log("Progression data has been reset.");
        }
        else
        {
            Debug.Log("No progression data found to reset.");
        }
    }

    private void OpenSaveFile()
    {
        if (System.IO.File.Exists(SavePath))
        {
            System.Diagnostics.Process.Start(SavePath);
        }
        else
        {
            Debug.LogWarning("No save file found.");
        }
    }

    private void UnlockAll()
    {
        //ResetAllProgressionData();

        //string json = JsonConvert.SerializeObject(ProgressionManager.ProgressionData, Formatting.Indented);

        //System.IO.File.WriteAllText(SavePath, json);
    }

}