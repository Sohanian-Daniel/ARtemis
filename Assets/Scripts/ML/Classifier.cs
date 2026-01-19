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

    [Header("Flood Fill Tuning")]
    [Range(0f, 1f)]
    public float minSeedConfidence = 0.6f;

    [Range(0f, 1f)]
    public float minNeighborConfidence = 0.2f;

    [Range(0f, 1f)]
    public float confidenceDropTolerance = 0.8f;

    [Range(1, 10)]
    public int floodFillRadius = 5;

    [Header("Visualization")]
    public bool generateMaskVisualization = true;
    public UnityEngine.UI.RawImage maskDisplay; // Optional:  Drag a UI RawImage here to auto-display

    // Public property to get the mask texture
    public Texture2D LastMaskTexture { get; private set; }

    private const int INPUT_SIZE = 256;

    private Worker worker;
    private Model runtimeModel;

    private readonly Materials[] ChannelToMaterial =
        {
        Materials.Unknown,
        Materials.Glass,
        Materials.Plastic,
        Materials.Paper,
        Materials.Metal,
        Materials.COUNT
    };

    private readonly Dictionary<Materials, Color> VisualizationColors =
    new Dictionary<Materials, Color>
    {
        { Materials.Unknown, Color.black },
        { Materials.Glass, Color.green },
        { Materials.Plastic, Color.cyan },
        { Materials.Paper, Color.yellow },
        { Materials.Metal, new Color(0.6f, 0.4f, 0.2f) },
        { Materials.COUNT, Color.magenta }
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

        var shape = outputTensor.shape; // (1, H, W, C)
        int height = shape[1];
        int width = shape[2];
        int channels = shape[3];

        float[] data = outputTensor.DownloadToArray();

        Materials[,] materialMap = new Materials[height, width];
        float[,] confidenceMap = new float[height, width];

        // --- Per-pixel argmax ---
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int baseIndex = (y * width + x) * channels;

                int bestChannel = 0;
                float bestValue = data[baseIndex];

                for (int c = 1; c < channels; c++)
                {
                    float v = data[baseIndex + c];
                    if (v > bestValue)
                    {
                        bestValue = v;
                        bestChannel = c;
                    }
                }

                materialMap[y, x] = ChannelToMaterial[bestChannel];
                confidenceMap[y, x] = bestValue;
            }
        }

        if (generateMaskVisualization)
        {
            CreateMaskVisualization(materialMap, confidenceMap, width, height);
        }

        // --- Connected components ---
        bool[,] visited = new bool[height, width];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (visited[y, x])
                    continue;

                Materials mat = materialMap[y, x];

                if (confidenceMap[y, x] < minSeedConfidence)
                    continue;

                List<Vector2Int> region = FloodFill(
                    materialMap,
                    confidenceMap,
                    visited,
                    x,
                    y,
                    mat,
                    confidenceMap[y, x],
                    width,
                    height
                );


                if (region.Count < 10)
                    continue;

                if (mat == Materials.Unknown || mat == Materials.COUNT)
                    continue;

                int minX = region.Min(p => p.x);
                int maxX = region.Max(p => p.x);
                int minY = region.Min(p => p.y);
                int maxY = region.Max(p => p.y);

                float avgConfidence = region.Average(p => confidenceMap[p.y, p.x]);

                // --- CENTER-BASED normalized bounding box ---
                float centerX = (minX + maxX + 1) * 0.5f / width;
                float centerY = (minY + maxY + 1) * 0.5f / height;
                float boxWidth = (maxX - minX + 1f) / width;
                float boxHeight = (maxY - minY + 1f) / height;

                results.Add(new ClassificationResult
                {
                    Material = mat,
                    Confidence = avgConfidence,
                    BoundingBox = new Rect(centerX, centerY, boxWidth, boxHeight),
                    ClassName = mat.ToString()
                });
            }
        }

        Debug.Log($"Found {results.Count} objects");
        return results;
    }

    private void CreateMaskVisualization(
    Materials[,] materialMap,
    float[,] confidenceMap,
    int width,
    int height)
    {
        if (LastMaskTexture != null)
            Destroy(LastMaskTexture);

        LastMaskTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Materials mat = materialMap[y, x];
                float conf = confidenceMap[y, x];

                Color baseColor = VisualizationColors.TryGetValue(mat, out var c) ? c : Color.black;

                pixels[y * width + x] = new Color(
                    baseColor.r,
                    baseColor.g,
                    baseColor.b,
                    conf // alpha = confidence
                );
            }
        }

        LastMaskTexture.SetPixels(pixels);
        LastMaskTexture.Apply();

        if (maskDisplay != null)
            maskDisplay.texture = LastMaskTexture;
    }

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

    private List<Vector2Int> FloodFill(Materials[,] materialMap, float[,] confidenceMap, bool[,] visited,
                                       int startX, int startY, Materials targetMaterial, float seedConfidence,
                                       int width, int height)
    {
        List<Vector2Int> region = new List<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        queue.Enqueue(new Vector2Int(startX, startY));
        visited[startY, startX] = true;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            region.Add(current);

            for (int oy = -floodFillRadius; oy <= floodFillRadius; oy++)
            {
                for (int ox = -floodFillRadius; ox <= floodFillRadius; ox++)
                {
                    if (ox == 0 && oy == 0)
                        continue;

                    int nx = current.x + ox;
                    int ny = current.y + oy;

                    if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                        continue;

                    if (visited[ny, nx])
                        continue;

                    // Strict material match
                    if (materialMap[ny, nx] != targetMaterial)
                        continue;

                    float neighborConf = confidenceMap[ny, nx];

                    // Confidence-aware expansion
                    if (neighborConf < minNeighborConfidence)
                        continue;

                    if (neighborConf < seedConfidence - confidenceDropTolerance)
                        continue;

                    visited[ny, nx] = true;
                    queue.Enqueue(new Vector2Int(nx, ny));
                }
            }
        }

        return region;
    }

}