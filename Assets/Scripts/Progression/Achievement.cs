using System;
using UnityEngine;
using UnityEngine.UIElements;

[System.Serializable]
public class Achievement
{
    public string name;
    public string description;
    public Sprite icon;

    public uint requiredProgress;
    public uint currentProgress => achievementData.currentProgress;

    public AchievementProgressData achievementData;

    [SerializeReference, SubclassSelector]
    public EventContext eventContext;

    [SerializeReference, SubclassSelector]
    public AchievementCondition achievementCondition;

    public bool isUnlocked = false;

    public void Subscribe()
    {
        if (currentProgress >= requiredProgress)
        {
            isUnlocked = true;
            return;
        }

        EventManager.GetEvent(eventContext.GetType()).AddListener(OnEventCallback, Priority.None);
    }

    public void Unsubscribe()
    {
        EventManager.GetEvent(eventContext.GetType()).RemoveListener(OnEventCallback, Priority.None);
    }

    public void OnEventCallback(EventContext ctx)
    {
        if (achievementCondition != null && !achievementCondition.Evaluate(ctx))
            return;

        Progress(1);
    }

    public void Progress(uint amount)
    {
        achievementData.currentProgress += amount;

        if (achievementData.currentProgress >= requiredProgress)
        {
            Unlock();
        }
    }

    public void Unlock()
    {
        if (isUnlocked)
            return;

        isUnlocked = true;
        EventManager.GetEvent<AchievementUnlockedEvent>().Invoke(new AchievementUnlockedEvent(this));
    }
}