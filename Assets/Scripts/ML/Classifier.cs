using System.Collections.Generic;
using UnityEngine;

public enum Materials
{
    Unknown = 0,
    Paper = 1,
    Plastic = 2,
    Glass = 3,
    // etc
    COUNT
}

public struct ClassificationResult
{
    public Materials Material;
    public float Confidence;
    public Rect BoundingBox;
}

public class Classifier : MonoBehaviour
{
    public List<ClassificationResult> Classify(Texture2D texture)
    {
        // Run ML model on texture...
        // do work to store them into results
        // eventually return

        // Dummy random results for illustration
        List<ClassificationResult> results = new();
        int randomCount = Random.Range(1, 5);
        for (int i = 0; i < randomCount; i++)
        {
            ClassificationResult result = new ClassificationResult
            {
                Material = (Materials)Random.Range(1, (int)Materials.COUNT),
                Confidence = Random.Range(0.5f, 1.0f),
                BoundingBox = new Rect(Random.Range(0f, 0.8f), Random.Range(0f, 0.8f), Random.Range(0.1f, 0.2f), Random.Range(0.1f, 0.2f))
            };
            results.Add(result);
        }

        return results;
    }
}
