using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

// Provides the AR Camera as a texture to be used for processing
[RequireComponent(typeof(ARCameraManager))]
public class CameraTextureProvider : MonoBehaviour
{
    public static CameraTextureProvider Instance;

    private ARCameraManager cameraManager;
    private Texture2D cameraTexture;

    void Awake()
    {
        cameraManager = GetComponent<ARCameraManager>();
        if (cameraManager == null)
        {
            Debug.LogError("ARCameraManager component is missing.");
        }
        else
        {
            Debug.Log("CameraTextureProvider::Initialized");
        }

        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public Texture2D GetTexture()
    {
        return cameraTexture;
    }

    private void OnEnable()
    {
        cameraManager.frameReceived += OnCameraFrameReceived;
    }

    private void OnDisable()
    {
        cameraManager.frameReceived -= OnCameraFrameReceived;
    }

    private void OnCameraFrameReceived(ARCameraFrameEventArgs args)
    {
        if (!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage cpuImage))
            return;

        // Convert the AR camera frame to a Texture2D
        UpdateTexture(ref cameraTexture, cpuImage);

        cpuImage.Dispose();
    }

    private void UpdateTexture(ref Texture2D texture, XRCpuImage cpuImage)
    {
        var conversionParams = new XRCpuImage.ConversionParams
        {
            inputRect = new RectInt(0, 0, cpuImage.width, cpuImage.height),
            outputDimensions = new Vector2Int(cpuImage.width, cpuImage.height),
            outputFormat = TextureFormat.RGBA32
        };

        int bufferSize = cpuImage.GetConvertedDataSize(conversionParams);

        // Allocate or resize texture buffer
        var buffer = new NativeArray<byte>(bufferSize, Allocator.Temp);

        cpuImage.Convert(conversionParams, buffer);

        // Create texture if necessary
        if (texture == null || texture.width != cpuImage.width || texture.height != cpuImage.height)
        {
            texture = new Texture2D(cpuImage.width, cpuImage.height, TextureFormat.RGBA32, false);
        }

        texture.LoadRawTextureData(buffer);
        texture.Apply();

        buffer.Dispose();
    }
}
