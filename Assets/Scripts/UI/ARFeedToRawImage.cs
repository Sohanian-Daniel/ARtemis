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

        rt = new RenderTexture(Screen.width, Screen.height, 1);
        arCamera.targetTexture = rt;
        targetRawImage.texture = rt;
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
