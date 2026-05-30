using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

/// <summary>
/// Manages the modding event system.
/// </summary>
public static class EventManager
{
    static Dictionary<Type, List<EventListener>> _listeners = new Dictionary<Type, List<EventListener>>();
    
    static Dictionary<Type, IEvent> _instances = new Dictionary<Type, IEvent>();

    //temporary.
    static EventManager()
    {
        ScanAssembly(Assembly.GetExecutingAssembly());
    }

    /// <summary>
    /// Scans an assembly for <see cref="IEvent"/>s.
    /// </summary>
    /// <param name="assembly"></param>
    public static void ScanAssembly(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes().Where(x => x.GetInterfaces().Contains(typeof(IEvent)) && !x.IsAbstract && !x.IsInterface))
        {
            Register(type);
        }
    }

    /// <summary>
    /// Returns a shared instance of the <see cref="IEvent"/> <typeparamref name="T"/>. This is done to save the <see cref="GC"/> time, thus increasing performance. You may ignore this method and simply create a new instance of an <see cref="IEvent"/>, but this will cost you in memory and performance. <b>Only one instance exists across this class, you should not use this method in async code as it may lead to strange behavior. Never assume the event you receive is the same as before.</b>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T GetInstance<T>() where T : IEvent, new()
    {
        if (_instances.ContainsKey(typeof(T)))
        {
            return (T)_instances[typeof(T)];
        }
        Register(typeof(T));
        return (T)_instances[typeof(T)];
    }

    /// <summary>
    /// Registers the event as something that exists.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static void Register<T>() where T : IEvent, new()
    {
        Register(typeof(T));
    }

    /// <summary>
    /// Registers the event as something that exists.
    /// </summary>
    public static void Register(Type type)
    {
        if (!_listeners.ContainsKey(type))
        {
            _listeners.Add(type, new());
        }
        if (!_instances.ContainsKey(type))
        {
            _instances.Add(type, (IEvent)Activator.CreateInstance(type));
        }
        Log.Print($"Register Event Type: {type.FullName}");
    }

    /// <summary>
    /// Subscribes to an event. Note that subclasses will also call the <param name="listener"></param>. For example, if Event B is a child of Event A, and you are listening to event A, if Event B is called, you will be notified. Keep in mind that the object which contains the delegate for your listener must remain in memory and is effectively, lifetime scoped. You may use a static method.
    /// </summary>
    /// <param name="listener"></param>
    /// <param name="cfg"></param>
    /// <typeparam name="T"></typeparam>
    public static void Subscribe<T>(Action<T> listener, EventSubscriberConfiguration cfg = null) where T : IEvent, new()
    {
        cfg ??= new EventSubscriberConfiguration();
        EventListener lstnr = new  EventListener();
        lstnr.EventType = typeof(T);
        lstnr.Method = listener.Method;
        lstnr.TargetObject = listener.Target;
        lstnr.SubscriberConfiguration = cfg;
        if (!_listeners.ContainsKey(lstnr.EventType))
        {
            Register(lstnr.EventType);
        }
        if (_listeners[lstnr.EventType].FirstOrDefault(x => x.Method == listener.Method) == null)
        {
            _listeners[lstnr.EventType].Add(lstnr);
        }
        // Essentially we pre-compute the parent-child relationship of the type when we subscribe, that way we just check on list instead of an unkown amount of lists when emitting the event. 
        if (!cfg.ExactEventOnly)
        {
            foreach (var evtType in _listeners.Keys.Where(x => lstnr.EventType.IsSubclassOf(x)))
            {
                if (_listeners[evtType].FirstOrDefault(x => x.Method == lstnr.Method) == default)
                {
                    _listeners[evtType].Add(lstnr);
                    //TODO: Use insert instead of sorting the entire list!
                    _listeners[evtType].Sort(
                        (x, y) =>
                            x.SubscriberConfiguration.Priority.CompareTo(y.SubscriberConfiguration.Priority));
                }
            }
        }
    }

    /// <summary>
    /// Removes a listener from the event system.
    /// </summary>
    /// <param name="listener"></param>
    /// <typeparam name="T"></typeparam>
    public static void Unsubscribe<T>(Action<T> listener) where T : IEvent, new()
    {
        Type eventType = typeof(T);
        if (!_listeners.ContainsKey(eventType))
        {
            Register(eventType);
            return;
        }
        EventListener l = _listeners[eventType].FirstOrDefault(x => x.Method == listener.Method && x.TargetObject == listener.Target);
        if (l != default)
        {
            _listeners[eventType].Remove(l);
        }
        // Essentially we pre-compute the parent-child relationship of the type when we subscribe, that way we just check on list instead of an unkown amount of lists when emitting the event. 
        foreach (var evtType in _listeners.Keys.Where(x => eventType.IsSubclassOf(x)))
        {
            EventListener lstnr = _listeners[evtType].FirstOrDefault(x => x.Method == listener.Method && x.TargetObject == listener.Target);
            if (lstnr == default)
            {
                return;
            }
            _listeners[evtType].Remove(lstnr);
        }
    }

    /// <summary>
    /// Emits an <see cref="IEvent"/>. Returns true by default. If the <param name="evt"></param> is an <see cref="ICancelableEvent"/>, the return will determine if the event should continue.
    /// </summary>
    /// <param name="evt"></param>
    /// <param name="src"></param>
    /// <returns></returns>
    public static bool Emit(IEvent evt, IEventSource src)
    {
        if (!_listeners.ContainsKey(evt.GetType()))
        {
            //No listeners, do not cancel.
            return true;
        }
        evt.Source = src;
        List<EventListener> listeners = _listeners[evt.GetType()];
        if (evt is ICancelableEvent cevt)
        {
            bool result = true;
            foreach (var listener in listeners)
            {
                listener.Method.Invoke(listener.TargetObject, [cevt]);
                result = !cevt.Canceled;
                if (cevt.Consumed)
                {
                    break;
                }
            }
            return result;
        }
        else
        {
            foreach (var listener in listeners)
            {
                listener.Method.Invoke(listener.TargetObject, [evt]);
                if (evt.Consumed)
                {
                    break;
                }
            }
        }
        //default result.
        return true;
    }
}

/// <summary>
/// Determines the structure for the event listener.
/// </summary>
public class EventListener
{
    /// <summary>
    /// Saved subscriber configuration
    /// </summary>
    public EventSubscriberConfiguration SubscriberConfiguration { get; set; }
    
    /// <summary>
    /// Target method
    /// </summary>
    public MethodInfo Method { get; set; }
    
    /// <summary>
    /// Target object (instance of the object to which the method is tied to.)
    /// </summary>
    public object TargetObject { get; set; }
    
    /// <summary>
    /// Type of the <see cref="IEvent"/> that is being listened to.
    /// </summary>
    public Type EventType { get; set; }
}

/// <summary>
/// Configuration class for event subscribers.
/// </summary>
public class EventSubscriberConfiguration
{
    /// <summary>
    /// Defines the event listener priority. Higher levels go first. Order within levels is random.
    /// </summary>
    public Priority Priority { get; set; } = Priority.Normal;

    /// <summary>
    /// Determines if subclasses of the <see cref="IEvent"/> you are listening for should also trigger your listener.
    /// </summary>
    public bool ExactEventOnly { get; set; } = false;
}

/// <summary>
/// Event listener priority.
/// </summary>
public enum Priority
{
    /// <summary>
    /// Lower than normal priority, perfect for logging events that fire, or general monitoring.
    /// </summary>
    Low = -1,
    /// <summary>
    /// Normal priority.
    /// </summary>
    Normal = 0,
    /// <summary>
    /// Higher priority, intended for overrides. Your mod should use this level only if needed. Mods that only use this level are considered bad practice.
    /// </summary>
    High = 1,
}