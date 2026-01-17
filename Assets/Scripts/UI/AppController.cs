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

    public Button deepScanButton;
    public RawImage frozenCameraImageUI;

    private Texture2D frozenCameraTexture;

    public Button finishScanButton;

    private Camera mainCamera;


    private void Awake()
    {
        if (deepScanButton == null)
        {
            Debug.LogWarning("Deep Scan button reference is missing!");
        }

        if (finishScanButton == null)
        {
            Debug.LogWarning("Finish Scan button reference is missing!");
        }

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        deepScanButton.onClick.AddListener(Deepscan);

        finishScanButton.onClick.AddListener(() =>
        {
            frozenCameraImageUI.gameObject.SetActive(false);
            finishScanButton.gameObject.SetActive(false);

            // Clear deep scan UI elements
            UpdateUI(new List<ClassificationResult>(), allowInteractions: false);

            // Resume regular fetching of results
            StartCoroutine(FetchClassifierResults());
        });

        mainCamera = Camera.main;
    }

    private void Start()
    {
        StartCoroutine(FetchClassifierResults());
    }

    private void Deepscan()
    {
        // Capture current camera frame
        Texture2D cameraTexture = CameraTextureProvider.Instance.GetTexture();

        if (cameraTexture != null && deepClassifier != null)
        {
            StopAllCoroutines(); // Stop regular fetching of results

            frozenCameraTexture = new Texture2D(cameraTexture.width, cameraTexture.height, TextureFormat.RGB24, false);
            frozenCameraTexture.SetPixels32(cameraTexture.GetPixels32());
            frozenCameraTexture.Apply();

            frozenCameraImageUI.texture = frozenCameraTexture;
            frozenCameraImageUI.gameObject.SetActive(true);

            // Reveal done button
            finishScanButton.gameObject.SetActive(true);

            List<ClassificationResult> results = deepClassifier.Classify(frozenCameraTexture);
            UpdateUI(results, allowInteractions: true); // Allow interactions in deep scan mode (to add to inventory)
        }
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
    }

    private IEnumerator FetchClassifierResults()
    {
        // Periodically run every fetchInterval seconds
        while (true)
        {
            // Get camera feed texture
            Texture2D cameraTexture = CameraTextureProvider.Instance.GetTexture();

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
