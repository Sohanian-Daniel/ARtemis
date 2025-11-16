using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class AppController : MonoBehaviour
{
    public static AppController Instance;

    // Reference to the ML Classifier to be used throughout the app
    public Classifier classifier;

    public float fetchInterval = 1.0f; // Interval in seconds to fetch classifier results

    public GameObject objectDisplayPrefab;
    private List<GameObject> activeDisplays = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        StartCoroutine(FetchClassifierResults());
    }

    private IEnumerator FetchClassifierResults()
    {
        // Periodically run every fetchInterval seconds
        while (true)
        {
            // Get camera feed texture
            Texture2D cameraTexture = CameraTextureProvider.Instance.GetTexture();

            List<ClassificationResult> results = classifier.Classify(cameraTexture);

            UpdateUI(results);

            yield return new WaitForSeconds(fetchInterval);
        }
    }

    private void UpdateUI(List<ClassificationResult> results)
    {
        // Clear existing displays
        foreach (var display in activeDisplays)
        {
            Destroy(display);
        }
        activeDisplays.Clear();

        // Create new displays based on classification results
        foreach (var result in results)
        {
            GameObject displayObj = Instantiate(objectDisplayPrefab, this.transform);
            ObjectDisplay display = displayObj.GetComponent<ObjectDisplay>();

            display.Initialize(result.Material, result.Confidence, result.BoundingBox);

            activeDisplays.Add(displayObj);
        }
    }
}
