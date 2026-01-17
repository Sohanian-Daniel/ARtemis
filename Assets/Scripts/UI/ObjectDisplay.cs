using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ObjectDisplay : MonoBehaviour
{
    public Materials materialType;
    public float confidence;
    public Rect boundingBox;

    public TextMeshProUGUI label;
    public GameObject boxVisual; // Visual representation of the bounding box (1x1 unit square)
    public Button clickButton;

    public void Initialize(ClassificationResult result, Action onClick = null)
    {
        materialType = result.Material;
        confidence = result.Confidence;
        boundingBox = result.BoundingBox;
        label.text = result.ClassName + $" ({confidence:P1})";

        if (onClick != null && clickButton != null)
        {
            clickButton.onClick.AddListener(() => onClick());
        }

        // Update visual representation based on material type and confidence
        UpdateDisplay();
    }

    private void OnDisable()
    {
        if (clickButton != null)
        {
            clickButton.onClick.RemoveAllListeners();
        }
    }

    private void UpdateDisplay()
    {
        Color displayColor = GetMaterialColor(materialType);

        Canvas canvas = GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        Vector2 displaySize = ARFeedToRawImage.Instance.targetRawImage.rectTransform.rect.size;

        if (boxVisual != null)
        {
            if (boxVisual.TryGetComponent<RectTransform>(out var boxRect))
            {
                //float centerX = (boundingBox.x - 0.5f) * canvasSize.x;
                //float centerY = (boundingBox.y - 0.5f) * canvasSize.y;
                //float width = boundingBox.width * canvasSize.x;
                //float height = boundingBox.height * canvasSize.y;

                // For YOLO Classifier
                float centerX = (boundingBox.x - 0.5f) * displaySize.x;
                float centerY = (boundingBox.y - 0.5f) * displaySize.y;
                float width = boundingBox.width * displaySize.x;
                float height = boundingBox.height * displaySize.y;

                boxRect.anchoredPosition = new Vector2(centerX, centerY);
                boxRect.sizeDelta = new Vector2(width, height);
            }

            if (boxVisual.TryGetComponent<Image>(out var boxImage))
            {
                boxImage.color = displayColor;
                Color colorWithAlpha = boxImage.color;
                colorWithAlpha.a = confidence;
                boxImage.color = colorWithAlpha;
            }
        }

        if (label != null)
        {
            label.color = displayColor;
            Color labelColor = label.color;
            labelColor.a = Mathf.Lerp(0.5f, 1.0f, confidence);
            label.color = labelColor;

            RectTransform labelRect = label.GetComponent<RectTransform>();
            if (labelRect != null && boxVisual != null)
            {
                RectTransform boxRect = boxVisual.GetComponent<RectTransform>();
                labelRect.anchoredPosition = new Vector2(
                    boxRect.anchoredPosition.x,
                    boxRect.anchoredPosition.y + (boxRect.sizeDelta.y * 0.5f) + 20f
                );
            }
        }
    }

    private Color GetMaterialColor(Materials material)
    {
        return material switch
        {
            Materials.Plastic => Color.red,
            Materials.Glass => Color.green,
            Materials.Metal => Color.cyan,
            Materials.Paper => Color.yellow,
            _ => Color.greenYellow,
        };
    }
}