using System.Collections.Generic;
using System.Linq;
using Unity.InferenceEngine;
using UnityEngine;

public class Classifier : BaseClassifier
{
    [Header("Model")]
    public ModelAsset modelAsset;

    [Header("Backend")]
    public BackendType backendType = BackendType.GPUCompute;

    [Header("Preprocessing Settings")]
    public bool normalizeInput = true;

    [Header("Color Matching Settings")]
    [Range(0f, 1f)]
    public float colorTolerance = 0.2f;

    [Range(0f, 1f)]
    public float minimumAlpha = 0.1f;

    [Header("Visualization")]
    public bool generateMaskVisualization = true;
    public UnityEngine.UI.RawImage maskDisplay; // Optional:  Drag a UI RawImage here to auto-display

    // Public property to get the mask texture
    public Texture2D LastMaskTexture { get; private set; }

    private const int INPUT_SIZE = 256;

    private Worker worker;
    private Model runtimeModel;

    private static readonly Dictionary<Materials, Color> MaterialColors = new Dictionary<Materials, Color>
    {
        { Materials. Plastic, Color.red },
        { Materials.Glass, Color. green },
        { Materials.Metal, Color.cyan }
    };

    private void Start()
    {
        if (modelAsset == null)
        {
            Debug.LogError("Model asset is not assigned!");
            return;
        }

        runtimeModel = ModelLoader.Load(modelAsset);
        worker = new Worker(runtimeModel, backendType);

        Debug.Log($"Model loaded:  {modelAsset.name}");
        Debug.Log($"Backend: {backendType}");
    }

    private void OnDestroy()
    {
        worker?.Dispose();

        if (LastMaskTexture != null)
        {
            Destroy(LastMaskTexture);
        }
    }

    public override List<ClassificationResult> Classify(Texture2D texture)
    {
        if (worker == null)
        {
            Debug.LogError("Worker not initialized!");
            return new List<ClassificationResult>();
        }

        Texture2D resizedTexture = ResizeTexture(texture, INPUT_SIZE, INPUT_SIZE);
        using Tensor<float> inputTensor = TextureToTensor(resizedTexture);

        if (resizedTexture != texture)
        {
            Destroy(resizedTexture);
        }

        worker.Schedule(inputTensor);
        Tensor<float> outputTensor = worker.PeekOutput() as Tensor<float>;

        List<ClassificationResult> results = ProcessSegmentationMask(outputTensor);

        outputTensor?.Dispose();

        return results;
    }

    private Tensor<float> TextureToTensor(Texture2D texture)
    {
        Color[] pixels = texture.GetPixels();
        TensorShape shape = new TensorShape(1, INPUT_SIZE, INPUT_SIZE, 3);

        float[] data = new float[INPUT_SIZE * INPUT_SIZE * 3];

        for (int y = 0; y < INPUT_SIZE; y++)
        {
            for (int x = 0; x < INPUT_SIZE; x++)
            {
                int pixelIndex = y * INPUT_SIZE + x;
                Color pixel = pixels[pixelIndex];
                int baseIndex = (y * INPUT_SIZE + x) * 3;

                if (normalizeInput)
                {
                    data[baseIndex + 0] = pixel.r;
                    data[baseIndex + 1] = pixel.g;
                    data[baseIndex + 2] = pixel.b;
                }
                else
                {
                    data[baseIndex + 0] = pixel.r * 255f;
                    data[baseIndex + 1] = pixel.g * 255f;
                    data[baseIndex + 2] = pixel.b * 255f;
                }
            }
        }

        return new Tensor<float>(shape, data);
    }

    private Texture2D ResizeTexture(Texture2D source, int targetWidth, int targetHeight)
    {
        if (source.width == targetWidth && source.height == targetHeight)
        {
            return source;
        }

        RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight);
        rt.filterMode = FilterMode.Bilinear;

        RenderTexture.active = rt;
        Graphics.Blit(source, rt);

        Texture2D result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGB24, false);
        result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        result.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        return result;
    }

    private List<ClassificationResult> ProcessSegmentationMask(Tensor<float> outputTensor)
    {
        List<ClassificationResult> results = new List<ClassificationResult>();

        var shape = outputTensor.shape;
        Debug.Log($"Output shape: {shape}");

        int height = shape[1];
        int width = shape[2];
        int channels = shape[3];

        float[] maskData = outputTensor.DownloadToArray();

        Color[,] maskColors = new Color[height, width];
        Materials[,] materialMap = new Materials[height, width];

        // Create visualization texture if enabled
        if (generateMaskVisualization)
        {
            CreateMaskVisualization(maskData, width, height, channels);
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int baseIndex = (y * width + x) * channels;

                float r = maskData[baseIndex + 0];
                float g = maskData[baseIndex + 1];
                float b = maskData[baseIndex + 2];
                float a = channels > 3 ? maskData[baseIndex + 3] : 1.0f;

                Color pixelColor = new Color(r, g, b, a);
                maskColors[y, x] = pixelColor;

                if (pixelColor.a >= minimumAlpha)
                {
                    materialMap[y, x] = GetMaterialFromColor(pixelColor);
                }
                else
                {
                    materialMap[y, x] = Materials.Unknown;
                }
            }
        }

        // Find connected components
        bool[,] visited = new bool[height, width];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (!visited[y, x] && materialMap[y, x] != Materials.Unknown)
                {
                    Materials material = materialMap[y, x];
                    List<Vector2Int> region = FloodFill(materialMap, visited, x, y, material, width, height);

                    if (region.Count > 10)
                    {
                        int minX = region.Min(p => p.x);
                        int maxX = region.Max(p => p.x);
                        int minY = region.Min(p => p.y);
                        int maxY = region.Max(p => p.y);

                        // Output bounding box as normalized coordinates with Center origin
                        Rect boundingBox = new Rect
                        {
                            x = 1 - ((minX + maxX) / 2f / width),
                            y = (minY + maxY) / 2f / height,
                            width = (maxX - minX + 1) / (float)width,
                            height = (maxY - minY + 1) / (float)height
                        };

                        float averageAlpha = 0f;
                        foreach (var pixel in region)
                        {
                            averageAlpha += maskColors[pixel.y, pixel.x].a;
                        }
                        averageAlpha /= region.Count;

                        float totalPixelsInBox = (maxX - minX + 1) * (maxY - minY + 1);
                        float coverage = region.Count / totalPixelsInBox;
                        float confidence = (averageAlpha + coverage) / 2f;

                        results.Add(new ClassificationResult
                        {
                            Material = material,
                            Confidence = confidence,
                            BoundingBox = boundingBox,
                            ClassName = material.ToString()
                        });
                    }
                }
            }
        }

        Debug.Log($"Found {results.Count} objects");
        return results;
    }

    // NEW: Create a texture from the raw mask output
    private void CreateMaskVisualization(float[] maskData, int width, int height, int channels)
    {
        // Destroy old texture if it exists
        if (LastMaskTexture != null)
        {
            Destroy(LastMaskTexture);
        }

        // Create new texture
        LastMaskTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int pixelIndex = y * width + x;
                int baseIndex = pixelIndex * channels;

                float r = maskData[baseIndex + 0];
                float g = maskData[baseIndex + 1];
                float b = maskData[baseIndex + 2];
                float a = channels > 3 ? maskData[baseIndex + 3] : 1.0f;

                // Clamp values to [0, 1] range
                r = Mathf.Clamp01(r);
                g = Mathf.Clamp01(g);
                b = Mathf.Clamp01(b);
                a = Mathf.Clamp01(a);

                pixels[pixelIndex] = new Color(r, g, b, a);
            }
        }

        LastMaskTexture.SetPixels(pixels);
        LastMaskTexture.Apply();

        // Auto-update UI if assigned
        if (maskDisplay != null)
        {
            maskDisplay.texture = LastMaskTexture;
        }

        Debug.Log($"Created mask visualization: {width}x{height}");
    }

    // NEW: Save mask to file
    public void SaveMaskToFile(string filename = "mask_output.png")
    {
        if (LastMaskTexture == null)
        {
            Debug.LogWarning("No mask texture to save!");
            return;
        }

        byte[] bytes = LastMaskTexture.EncodeToPNG();
        string path = System.IO.Path.Combine(Application.persistentDataPath, filename);
        System.IO.File.WriteAllBytes(path, bytes);
        Debug.Log($"Saved mask to:  {path}");
    }

    private Materials GetMaterialFromColor(Color color)
    {
        foreach (var kvp in MaterialColors)
        {
            if (IsColorMatch(color, kvp.Value, colorTolerance))
            {
                return kvp.Key;
            }
        }
        return Materials.Unknown;
    }

    private bool IsColorMatch(Color color1, Color color2, float tolerance)
    {
        float distance = Mathf.Sqrt(
            Mathf.Pow(color1.r - color2.r, 2) +
            Mathf.Pow(color1.g - color2.g, 2) +
            Mathf.Pow(color1.b - color2.b, 2)
        );

        float normalizedDistance = distance / Mathf.Sqrt(3f);
        return normalizedDistance <= tolerance;
    }

    private List<Vector2Int> FloodFill(Materials[,] materialMap, bool[,] visited, int startX, int startY, Materials targetMaterial, int width, int height)
    {
        List<Vector2Int> region = new List<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(new Vector2Int(startX, startY));
        visited[startY, startX] = true;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            region.Add(current);

            int[] dx = { -1, 1, 0, 0 };
            int[] dy = { 0, 0, -1, 1 };

            for (int i = 0; i < 4; i++)
            {
                int nx = current.x + dx[i];
                int ny = current.y + dy[i];

                if (nx >= 0 && nx < width && ny >= 0 && ny < height &&
                    !visited[ny, nx] && materialMap[ny, nx] == targetMaterial)
                {
                    visited[ny, nx] = true;
                    queue.Enqueue(new Vector2Int(nx, ny));
                }
            }
        }

        return region;
    }
}