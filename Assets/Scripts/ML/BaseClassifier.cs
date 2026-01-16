using System.Collections.Generic;
using UnityEngine;

public enum Materials
{
    Unknown = 0,
    Paper = 1,
    Plastic = 2,
    Glass = 3,
    Metal = 4,
    COUNT
}

public struct Detection
{
    public float x;
    public float y;
    public float width;
    public float height;
    public int classId;
    public float confidence;
    public string className;
}

public struct ClassificationResult
{
    public Materials Material;
    public float Confidence;
    public Rect BoundingBox;
    public string ClassName;
}

public abstract class BaseClassifier : MonoBehaviour
{
    public abstract List<ClassificationResult> Classify(Texture2D texture);
}