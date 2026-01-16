using System.Collections.Generic;
using System.Linq;
using Unity.InferenceEngine;
using UnityEngine;

public class YOLOPostNMSClassifier : BaseClassifier
{
    [Header("Model")]
    public ModelAsset modelAsset;

    [Header("Backend")]
    public BackendType backendType = BackendType.GPUCompute;

    [Header("Detection Settings")]
    [Range(0f, 1f)]
    public float confidenceThreshold = 0.5f;

    [Range(0f, 1f)]
    public float iouThreshold = 0.45f;

    public int INPUT_SIZE = 640;
    private Worker worker;
    private Model runtimeModel;

    // TACO class names (60 classes)
    private static readonly string[] TACOClasses = new string[]
    {
        "Aluminium foil", "Battery", "Aluminium blister pack", "Carded blister pack",
        "Other plastic bottle", "Clear plastic bottle", "Glass bottle", "Plastic bottle cap",
        "Metal bottle cap", "Broken glass", "Food Can", "Aerosol", "Drink can",
        "Toilet tube", "Other carton", "Egg carton", "Drink carton", "Corrugated carton",
        "Meal carton", "Pizza box", "Paper cup", "Disposable plastic cup", "Foam cup",
        "Glass cup", "Other plastic cup", "Food waste", "Glass jar", "Plastic lid",
        "Metal lid", "Other plastic", "Magazine paper", "Tissues", "Wrapping paper",
        "Normal paper", "Paper bag", "Plastified paper bag", "Plastic film",
        "Six pack rings", "Garbage bag", "Other plastic wrapper", "Single-use carrier bag",
        "Polypropylene bag", "Crisp packet", "Spread tub", "Tupperware", "Disposable food container",
        "Foam food container", "Other plastic container", "Plastic glooves", "Plastic utensils",
        "Pop tab", "Rope & strings", "Scrap metal", "Shoe", "Squeezable tube",
        "Plastic straw", "Paper straw", "Styrofoam piece", "Unlabeled litter", "Cigarette"
    };

    // Map TACO classes to Materials
    private static readonly Dictionary<string, Materials> ClassToMaterial = new Dictionary<string, Materials>
    {
        // METAL
        { "Aluminium foil", Materials.Metal },
        { "Aluminium blister pack", Materials.Metal },
        { "Metal bottle cap", Materials.Metal },
        { "Food Can", Materials.Metal },
        { "Aerosol", Materials.Metal },
        { "Drink can", Materials.Metal },
        { "Metal lid", Materials.Metal },
        { "Pop tab", Materials.Metal },
        { "Scrap metal", Materials.Metal },
        
        // GLASS
        { "Glass bottle", Materials.Glass },
        { "Broken glass", Materials.Glass },
        { "Glass cup", Materials.Glass },
        { "Glass jar", Materials.Glass },
        
        // PLASTIC
        { "Carded blister pack", Materials.Plastic },
        { "Other plastic bottle", Materials.Plastic },
        { "Clear plastic bottle", Materials.Plastic },
        { "Plastic bottle cap", Materials.Plastic },
        { "Disposable plastic cup", Materials.Plastic },
        { "Foam cup", Materials.Plastic },
        { "Other plastic cup", Materials.Plastic },
        { "Plastic lid", Materials.Plastic },
        { "Other plastic", Materials.Plastic },
        { "Plastic film", Materials.Plastic },
        { "Six pack rings", Materials.Plastic },
        { "Garbage bag", Materials.Plastic },
        { "Other plastic wrapper", Materials.Plastic },
        { "Single-use carrier bag", Materials.Plastic },
        { "Polypropylene bag", Materials.Plastic },
        { "Crisp packet", Materials.Plastic },
        { "Spread tub", Materials.Plastic },
        { "Tupperware", Materials.Plastic },
        { "Disposable food container", Materials.Plastic },
        { "Foam food container", Materials.Plastic },
        { "Other plastic container", Materials.Plastic },
        { "Plastic glooves", Materials.Plastic },
        { "Plastic utensils", Materials.Plastic },
        { "Squeezable tube", Materials.Plastic },
        { "Plastic straw", Materials.Plastic },
        { "Styrofoam piece", Materials.Plastic },
        
        // PAPER
        { "Toilet tube", Materials.Paper },
        { "Other carton", Materials.Paper },
        { "Egg carton", Materials.Paper },
        { "Drink carton", Materials.Paper },
        { "Corrugated carton", Materials.Paper },
        { "Meal carton", Materials.Paper },
        { "Pizza box", Materials.Paper },
        { "Paper cup", Materials.Paper },
        { "Magazine paper", Materials.Paper },
        { "Tissues", Materials.Paper },
        { "Wrapping paper", Materials.Paper },
        { "Normal paper", Materials.Paper },
        { "Paper bag", Materials.Paper },
        { "Plastified paper bag", Materials.Paper },
        { "Paper straw", Materials.Paper },
        
        // Battery - could be Metal or special e-waste category
        { "Battery", Materials.Metal },
        
        // Unknown/Non-recyclable
        { "Food waste", Materials.Unknown },
        { "Rope & strings", Materials.Unknown },
        { "Shoe", Materials.Unknown },
        { "Unlabeled litter", Materials.Unknown },
        { "Cigarette", Materials.Unknown },
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

        Debug.Log($"TACO YOLO Model loaded:  {modelAsset.name}");
        Debug.Log($"Backend: {backendType}");
        Debug.Log($"Detecting {TACOClasses.Length} trash categories");
    }

    private void OnDestroy()
    {
        worker?.Dispose();
    }

    public override List<ClassificationResult> Classify(Texture2D texture)
    {
        if (worker == null)
        {
            Debug.LogError("Worker not initialized!");
            return new List<ClassificationResult>();
        }

        // Step 1: Resize to 640x640
        Texture2D resized = ResizeTexture(texture, INPUT_SIZE, INPUT_SIZE);

        // Step 2: Normalize and format for model
        using Tensor<float> inputTensor = PreprocessImage(resized);

        if (resized != texture)
            Destroy(resized);

        // Step 3: Run inference
        worker.Schedule(inputTensor);
        var outputTensor = worker.PeekOutput() as Tensor<float>;

        // Step 4: Read output
        var shape = outputTensor.shape;
        float[] outputData = outputTensor.DownloadToArray();

        Debug.Log($"Raw output shape: {shape}");

        // Step 5: Parse detections
        List<Detection> detections = ParseOutput(outputData, shape);

        outputTensor?.Dispose();

        var results = new List<ClassificationResult>();
        // Step 7: Convert to ClassificationResult with Materials mapping
        foreach (var det in detections)
        {
            Materials material = Materials.Unknown;
            if (ClassToMaterial.TryGetValue(det.className, out Materials mappedMaterial))
            {
                material = mappedMaterial;
            }

            // det.* are in MODEL INPUT pixel space (INPUT_SIZE x INPUT_SIZE).
            // With letterbox preprocessing, undo padding + scale to get ORIGINAL image pixel space.
            float center_x_norm = det.x / INPUT_SIZE;
            float center_y_norm = det.y / INPUT_SIZE;
            float width_norm = det.width / INPUT_SIZE;
            float height_norm = det.height / INPUT_SIZE;

            // Store as center + dimensions
            Rect bbox = new Rect(center_x_norm, center_y_norm, width_norm, height_norm);

            results.Add(new ClassificationResult
            {
                Material = material,
                Confidence = det.confidence,
                BoundingBox = bbox,
                ClassName = det.className
            });
        }

        return results;
    }

    private Tensor<float> PreprocessImage(Texture2D texture)
    {
        Color[] pixels = texture.GetPixels();

        // YOLO expects [1, 3, 640, 640] - NCHW format
        TensorShape shape = new TensorShape(1, 3, INPUT_SIZE, INPUT_SIZE);
        float[] data = new float[3 * INPUT_SIZE * INPUT_SIZE];

        int channelSize = INPUT_SIZE * INPUT_SIZE;

        for (int y = 0; y < INPUT_SIZE; y++)
        {
            for (int x = 0; x < INPUT_SIZE; x++)
            {
                int pixelIndex = y * INPUT_SIZE + x;
                Color pixel = pixels[pixelIndex];

                // RGB, normalized to [0, 1]
                data[0 * channelSize + pixelIndex] = pixel.r;
                data[1 * channelSize + pixelIndex] = pixel.g;
                data[2 * channelSize + pixelIndex] = pixel.b;
            }
        }

        return new Tensor<float>(shape, data);
    }

    private List<Detection> ParseOutput(float[] output, TensorShape shape)
    {
        int n = shape[1];
        int cols = shape[2];

        if (cols != 6)
        {
            Debug.LogError($"Unexpected output shape: {shape}. Expected [1, N, 6].");
            return new List<Detection>();
        }

        List<Detection> detections = new List<Detection>(n);

        // Layout assumption: row-major [1, N, 6] => index = i * 6 + j
        // If you find coords are nonsense, the tensor may be transposed; tell me and I’ll adapt it.
        for (int i = 0; i < n; i++)
        {
            int baseIdx = i * 6;

            float x1 = output[baseIdx + 0];
            float y1 = output[baseIdx + 1];
            float x2 = output[baseIdx + 2];
            float y2 = output[baseIdx + 3];
            float conf = output[baseIdx + 4];
            int classId = Mathf.RoundToInt(output[baseIdx + 5]);

            // Some exports pad empty slots with 0; skip them quickly
            if (conf <= 0f)
                continue;

            if (conf < confidenceThreshold)
                continue;

            // Basic sanity: skip invalid boxes
            float w = x2 - x1;
            float h = y2 - y1;
            if (w <= 0f || h <= 0f)
                continue;

            // Convert corners -> center in MODEL INPUT pixel space (INPUT_SIZE x INPUT_SIZE)
            float cx = (x1 + x2) * 0.5f;
            float cy = (y1 + y2) * 0.5f;

            // Clamp class id
            if (classId < 0 || classId >= TACOClasses.Length)
                continue;

            detections.Add(new Detection
            {
                x = cx,
                y = cy,
                width = w,
                height = h,
                classId = classId,
                confidence = conf,
                className = TACOClasses[classId]
            });
        }

        return detections;
    }
    private Texture2D ResizeTexture(Texture2D source, int targetWidth, int targetHeight)
    {
        if (source.width == targetWidth && source.height == targetHeight)
            return source;

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

}