using System;
using System.Collections.Generic;

public delegate void EntityEvent(EventContext ctx);

public enum Priority : int
{
    First,
    None,
    Result,
    COUNT
}

public interface IEvent
{
    void AddListener(Action<EventContext> listener, Priority priority = Priority.None);
    void RemoveListener(Action<EventContext> listener);
    void RemoveListener(Action<EventContext> listener, Priority priority);
    void ClearListeners();
    void Invoke(EventContext eventContext, bool invokeGlobal = true);
    void InvokeExcluding(EventContext eventContext, Priority excludedPriority, bool invokeGlobal = true);
    void InvokeOnly(EventContext eventContext, Priority includedPriority, bool invokeGlobal = true);
}

public class Event<T> : IEvent where T : EventContext
{
    public class EventHandle
    {
        public Action<T> Handler { get; }
        public int Priority { get; }

        public EventHandle(Action<T> handler, int priority)
        {
            Handler = handler;
            Priority = priority;
        }
    }

    /// <summary>
    /// We need to store the events sorted by priority while having good lookup time,
    /// we can use a dictionary of lists to store the events, but since priorities are
    /// simple integers, we can use an array instead.
    /// </summary>
    private readonly List<Action<T>>[] events = new List<Action<T>>[(int)Priority.COUNT];

    public Event()
    {
        for (int i = 0; i < (int)Priority.COUNT; i++)
        {
            events[i] = new List<Action<T>>();
        }
    }

    public void AddListener(Action<T> listener, Priority priority = Priority.None)
    {
        events[(int)priority].Add(listener);
    }

    public void RemoveListener(Action<T> listener)
    {
        for (int i = 0; i < (int)Priority.COUNT; i++)
        {
            if (events[i].Remove(listener))
                return;
        }
    }

    public void RemoveListener(Action<T> listener, Priority priority)
    {
        events[(int)priority].Remove(listener);
    }

    public void ClearListeners()
    {
        for (int i = 0; i < (int)Priority.COUNT; i++)
        {
            events[i].Clear();
        }
    }

    // Add operator overloads for += and -=
    public static Event<T> operator +(Event<T> eventObj, Action<T> listener)
    {
        eventObj.AddListener(listener);
        return eventObj;
    }

    public static Event<T> operator +(Event<T> eventObj, (Action<T>, Priority) listener)
    {
        eventObj.AddListener(listener.Item1, listener.Item2);
        return eventObj;
    }

    public static Event<T> operator -(Event<T> eventObj, Action<T> listener)
    {
        eventObj.RemoveListener(listener);
        return eventObj;
    }

    public static Event<T> operator -(Event<T> eventObj, (Action<T>, Priority) listener)
    {
        eventObj.RemoveListener(listener.Item1, listener.Item2);
        return eventObj;
    }

    // Invokes
    public void Invoke(T eventContext, bool invokeGlobal = false)
    {
        for (int i = 0; i < (int)Priority.COUNT; i++)
        {
            var length = events[i].Count;
            if (length == 0)
                continue;

            var span = events[i];
            for (int j = 0; j < length; j++)
            {
                span[j](eventContext);
            }
        }

        // Invoke global event
        if (invokeGlobal)
        {
            EventManager.GetEvent<T>().Invoke(eventContext, false);
        }
    }

    public void InvokeExcluding(T eventContext, Priority excludedPriority, bool invokeGlobal = false)
    {
        for (int i = 0; i < (int)Priority.COUNT; i++)
        {
            if (i == (int)excludedPriority)
                continue;

            var length = events[i].Count;
            if (events[i].Count == 0)
                continue;

            var span = events[i];
            for (int j = 0; j < length; j++)
            {
                span[j](eventContext);
            }
        }

        // Invoke global event
        if (invokeGlobal)
        {
            EventManager.GetEvent<T>().InvokeExcluding(eventContext, excludedPriority, false);
        }
    }

    public void InvokeOnly(T eventContext, Priority includedPriority, bool invokeGlobal = false)
    {
        var length = events[(int)includedPriority].Count;
        if (length == 0)
            return;

        var span = events[(int)includedPriority];
        for (int j = 0; j < length; j++)
        {
            span[j](eventContext);
        }

        // Invoke global event
        if (invokeGlobal)
        {
            EventManager.GetEvent<T>().InvokeExcluding(eventContext, includedPriority, false);
        }
    }

    // Interface implementation
    void IEvent.AddListener(Action<EventContext> listener, Priority priority)
    {
        AddListener(listener, priority);
    }

    void IEvent.RemoveListener(Action<EventContext> listener)
    {
        RemoveListener(listener);
    }

    void IEvent.RemoveListener(Action<EventContext> listener, Priority priority)
    {
        RemoveListener(listener, priority);
    }

    void IEvent.ClearListeners()
    {
        ClearListeners();
    }

    void IEvent.Invoke(EventContext eventContext, bool invokeGlobal)
    {
        Invoke((T)eventContext, invokeGlobal);
    }

    void IEvent.InvokeExcluding(EventContext eventContext, Priority excludedPriority, bool invokeGlobal)
    {
        InvokeExcluding((T)eventContext, excludedPriority, invokeGlobal);
    }

    void IEvent.InvokeOnly(EventContext eventContext, Priority includedPriority, bool invokeGlobal)
    {
        InvokeOnly((T)eventContext, includedPriority, invokeGlobal);
    }
}