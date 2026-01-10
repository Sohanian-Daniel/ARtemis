using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    public static Dictionary<Type, IEvent> Events = new();

    // InitializeOnLoad
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        // Create a new GameObject to hold the EntityManager
        GameObject go = new("EventManager");
        go.AddComponent<EventManager>();
    }

    public static Event<T> GetEvent<T>() where T : EventContext
    {
        Type eventType = typeof(T);
        if (Events.TryGetValue(eventType, out IEvent eventHandler))
        {
            return (Event<T>)eventHandler;
        }

        Event<T> newEvent = new();
        Events.Add(eventType, newEvent);
        return newEvent;
    }

    public static IEvent GetEvent(Type eventContextType)
    {
        if (Events.TryGetValue(eventContextType, out IEvent eventHandler))
        {
            return eventHandler;
        }

        // Create an Event<eventType>
        var eventGenericType = typeof(Event<>).MakeGenericType(eventContextType);
        IEvent newEvent = (IEvent)Activator.CreateInstance(eventGenericType);
        Events.Add(eventContextType, newEvent);
        return newEvent;
    }

    public static void AddListener<T>(Action<T> listener, Priority priority = Priority.None) where T : EventContext
    {
        GetEvent<T>().AddListener(listener, priority);
    }

    public static void RemoveListener<T>(Action<T> listener) where T : EventContext
    {
        GetEvent<T>().RemoveListener(listener);
    }
}