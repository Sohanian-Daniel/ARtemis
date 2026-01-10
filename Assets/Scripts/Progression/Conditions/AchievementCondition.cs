using UnityEngine;

[System.Serializable]
public abstract class AchievementCondition
{
    public AchievementCondition() { }

    public abstract bool Evaluate(EventContext ctx);
}