using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LogicCondition : AchievementCondition
{
    public enum LogicType
    {
        And,
        Or,
        Not
    }

    public LogicType logicType;

    [SerializeReference, SubclassSelector]
    public List<AchievementCondition> conditions = new();

    public override bool Evaluate(EventContext ctx)
    {
        switch (logicType)
        {
            case LogicType.And:
                foreach (var condition in conditions)
                {
                    if (!condition.Evaluate(ctx))
                        return false;
                }
                return true;
            case LogicType.Or:
                foreach (var condition in conditions)
                {
                    if (condition.Evaluate(ctx))
                        return true;
                }
                return false;
            case LogicType.Not:
                if (conditions.Count != 1)
                {
                    Debug.LogWarning("Not logic type should have exactly one condition.");
                    return false;
                }
                return !conditions[0].Evaluate(ctx);
            default:
                Debug.LogWarning("Unknown logic type.");
                return false;
        }
    }
}