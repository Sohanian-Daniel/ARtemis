using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class AppController : MonoBehaviour
{
    public static AppController Instance;

    // Reference to the ML Classifier to be used throughout the app
    public BaseClassifier classifier;
    public BaseClassifier deepClassifier;

    public float fetchInterval = 1.0f; // Interval in seconds to fetch classifier results

    public GameObject objectDisplayPrefab;
    private List<GameObject> activeDisplays = new List<GameObject>();

    public RawImage frozenCameraImageUI;

    private Texture2D frozenCameraTexture;

    private Camera mainCamera;


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

        mainCamera = Camera.main;

        // Prefer a higher frame rate for smoother AR experience
        Application.targetFrameRate = 120;
    }

    private void Start()
    {
        StartClassification();
    }

    public void StartClassification()
    {
        StopAllCoroutines();

        mainCamera.enabled = true;

        // Clear deep scan UI elements
        UpdateUI(new List<ClassificationResult>(), allowInteractions: false);

        // Resume regular fetching of results
        StartCoroutine(FetchClassifierResults());
    }

    public void StopClassification()
    {
        mainCamera.enabled = false;

        UpdateUI(new List<ClassificationResult>(), allowInteractions: false);
        StopAllCoroutines();
    }

    public void Deepscan()
    {
        // Capture current camera frame
        Texture2D cameraTexture = ARFeedToRawImage.Instance.GetTexture2D();
        if (cameraTexture == null)
        {
            Debug.LogWarning("Camera texture is null, cannot perform deep scan.");
            return;
        }

        if (deepClassifier == null)
        {
            Debug.LogWarning("Deep classifier is not assigned.");
            return;
        }

        StopClassification();

        // Copy the camera texture to freeze the image
        Texture2D frozenCameraTexture = new Texture2D(cameraTexture.width, cameraTexture.height, TextureFormat.RGB24, false);
        frozenCameraTexture.SetPixels32(cameraTexture.GetPixels32());
        frozenCameraTexture.Apply();

        frozenCameraImageUI.texture = frozenCameraTexture;

        List<ClassificationResult> results = deepClassifier.Classify(frozenCameraTexture);
        UpdateUI(results, allowInteractions: true); // Allow interactions in deep scan mode (to add to inventory)
    }

    private void OnSelectItem(ObjectDisplay display, ClassificationResult result)
    {
        Debug.Log($"Selected item: {result.ClassName} with confidence {result.Confidence}");

        display.transform.DOScale(Vector3.zero, 0.5f);

        // Move to the inventory button position (for now top right corner)
        display.transform.DOMove(new Vector3(Screen.width - 50, Screen.height - 50, 0), 0.5f).SetEase(Ease.InQuad).OnComplete(() =>
        {
            activeDisplays.Remove(display.gameObject);
            Destroy(display.gameObject);
        });

        Inventory.Instance.AddItem(result);

        EventManager.GetEvent<OnItemSelectedEvent>().Invoke(new OnItemSelectedEvent(result));
    }

    private IEnumerator FetchClassifierResults()
    {
        // Periodically run every fetchInterval seconds
        while (true)
        {
            // Get camera feed texture
            Texture2D cameraTexture = ARFeedToRawImage.Instance.GetTexture2D();

            if (cameraTexture != null && classifier != null)
            {
                List<ClassificationResult> results = classifier.Classify(cameraTexture);

                UpdateUI(results);
            }

            yield return new WaitForSeconds(fetchInterval);
        }
    }

    private void UpdateUI(List<ClassificationResult> results, bool allowInteractions = false)
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

            Action onClickAction = null;
            if (allowInteractions)
            {
                onClickAction = () => OnSelectItem(display, result);
            }

            display.Initialize(result, onClickAction);

            activeDisplays.Add(displayObj);
        }
    }
}
