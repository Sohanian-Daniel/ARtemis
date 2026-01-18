public class OnItemSelectedEvent : EventContext
{
    public ClassificationResult classificationResult;

    public OnItemSelectedEvent() { }

    public OnItemSelectedEvent(ClassificationResult res)
    {
        this.classificationResult = res;
    }
}