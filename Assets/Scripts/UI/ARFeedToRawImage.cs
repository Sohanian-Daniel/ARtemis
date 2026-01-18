using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

public class ARFeedToRawImage : MonoBehaviour
{
    public static ARFeedToRawImage Instance;

    public Camera arCamera;      // your AR Camera
    public RawImage targetRawImage;

    private RenderTexture rt;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (arCamera == null)
        {
            arCamera = Camera.main;
        }

        rt = new RenderTexture((int) targetRawImage.rectTransform.rect.width, (int) targetRawImage.rectTransform.rect.height, 1);
        arCamera.targetTexture = rt;
        targetRawImage.texture = rt;
    }

    private Texture2D textureCache = null;

    public Texture2D GetTexture2D()
    {
        if (textureCache == null)
        {
            textureCache = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        }

        RenderTexture.active = rt;
        textureCache.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        textureCache.Apply();

        return textureCache;
    }

    void OnDestroy()
    {
        if (arCamera)
        {
            arCamera.targetTexture = null;
        }
        
        rt.Release();
    }
}
