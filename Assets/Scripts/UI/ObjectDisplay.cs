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

    public void Initialize(Materials material, float conf, Rect box)
    {
        materialType = material;
        confidence = conf;
        boundingBox = box;

        // Update visual representation based on material type and confidence
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        Color displayColor = GetMaterialColor(materialType);

        // Get the Canvas RectTransform (should be the root canvas)
        Canvas canvas = GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        Vector2 canvasSize = canvasRect.rect.size;

        // Resize and position the bounding box visual
        if (boxVisual != null)
        {
            RectTransform boxRect = boxVisual.GetComponent<RectTransform>();
            if (boxRect != null)
            {
                // Convert normalized coordinates to actual canvas coordinates
                float centerX = (boundingBox.x - 0.5f) * canvasSize.x;
                float centerY = (boundingBox.y - 0.5f) * canvasSize.y;
                float width = boundingBox.width * canvasSize.x;
                float height = boundingBox.height * canvasSize.y;

                // Set position and size relative to canvas
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

        // Update label text and position
        if (label != null)
        {
            label.color = displayColor;
            Color labelColor = label.color;
            labelColor.a = confidence;
            label.color = labelColor;

            label.text = $"{materialType} ({confidence * 100:F1}%)";

            // Set label position above the bounding box
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
            Materials.Paper => Color.yellow,
            Materials.Plastic => Color.blue,
            Materials.Glass => Color.cyan,
            _ => Color.gray,
        };
    }
}