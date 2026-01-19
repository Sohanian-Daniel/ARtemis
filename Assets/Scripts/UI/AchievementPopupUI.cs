using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AchievementPopupUI : MonoBehaviour
{
    public GameObject achievementPopupPrefab;
    public RectTransform popupOrigin;

    public List<AchievementUnlockedEvent> eventQueue = new();

    private void Start()
    {
        EventManager.GetEvent<AchievementUnlockedEvent>().AddListener(OnAchievementUnlocked);
    }

    public void OnAchievementUnlocked(AchievementUnlockedEvent evt)
    {
        if (eventQueue.Count == 0)
        {
            StartCoroutine(HandleEventQueue());
        }

        eventQueue.Add(evt);
    }

    private IEnumerator HandleEventQueue()
    {
        yield return null; // Wait for one frame to ensure all events are queued

        for (int i = 0; i < eventQueue.Count; i++)
        {
            DisplayAchievementPopup(eventQueue[i], eventQueue.Count - i);

            // Wait for 1 second before processing the next event
            yield return new WaitForSeconds(1f);
        }
    }

    public void DisplayAchievementPopup(AchievementUnlockedEvent evt, int count)
    {
        GameObject popupInstance = Instantiate(achievementPopupPrefab, popupOrigin);
        RectTransform rectTransform = popupInstance.GetComponent<RectTransform>();

        // Set anchor to be bottom center
        rectTransform.anchorMin = new Vector2(0.5f, 0f);
        rectTransform.anchorMax = new Vector2(0.5f, 0f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        // Set Pos X and Pos Y to 0
        rectTransform.anchoredPosition = Vector2.zero;

        AchievementUIDisplay display = popupInstance.GetComponent<AchievementUIDisplay>();
        display.Initialize(evt.achievementDefinition);

        // Calculate total displacement and total delay based on count so that they all have the same speed on screen
        // Base duration is 2 seconds, base displacement is 200 units
        float baseDuration = 2f;
        float baseDisplacement = 400f;

        // Calculated based on size of popup
        float additionalDisplacement = rectTransform.rect.height + 50f;
        float additionalDelay = additionalDisplacement / baseDisplacement * baseDuration;

        float totalDuration = baseDuration + count * additionalDelay; // Each additional popup adds 0.5s to the duration
        float totalDisplacement = baseDisplacement + count * additionalDisplacement; // Each additional popup adds 50 units to the displacement

        rectTransform.DOMoveY(rectTransform.position.y + totalDisplacement, totalDuration).SetEase(Ease.OutCubic);
        popupInstance.GetComponent<CanvasGroup>().DOFade(0f, 2f).SetEase(Ease.OutCubic).SetDelay(totalDuration).OnComplete(() =>
        {
            Destroy(popupInstance);
            eventQueue.Remove(evt);
        });
    }

}