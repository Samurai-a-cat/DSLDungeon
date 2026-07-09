using DSLDungeon.Game.Entities;

namespace DSLDungeon.Game.Core.Actions;

public class EventQueue
{
    private readonly List<IQueueEvent> _events = new();
    private const int MaxQueueCapacity = 10;

    public bool IsEmpty => _events.Count == 0;
    public int Count => _events.Count;

    public bool Enqueue(IQueueEvent ev, WorldState world)
    {
        if (_events.Count >= MaxQueueCapacity)
        {
            Console.WriteLine($"[EventQueue] Переполнена очередь для сущности {ev.Owner}!");
            EventPool.Return(ev);
            return false;
        }

        _events.Add(ev);
        SortEvents();
        
        // Сигнал событию зарегистрировать владельца в системе
        ev.OnEnqueue(world); 
        return true;
    }

    public IQueueEvent? GetActiveEvent()
    {
        return _events.Count > 0 ? _events[0] : null;
    }

    public void Clear()
    {
        for (int i = 0; i < _events.Count; i++)
        {
            _events[i].Status = EventStatus.Cancelled;
        }
    }

    public void ClearExcept(IQueueEvent keepEvent)
    {
        for (int i = 0; i < _events.Count; i++)
        {
            var ev = _events[i];
            if (ev != keepEvent)
            {
                ev.Status = EventStatus.Cancelled;
            }
        }
    }

    public void CleanUp(WorldState world)
    {
        for (int i = _events.Count - 1; i >= 0; i--)
        {
            var ev = _events[i];
            if (ev.Status == EventStatus.Completed)
            {
                ev.OnFinish(world); // <- Вызов при успешном завершении
                ev.OnCleanUp(world); 
                _events.RemoveAt(i);
                EventPool.Return(ev);
            }
            else if (ev.Status == EventStatus.Cancelled)
            {
                ev.OnCancel(world); // <- Вызов при отмене/прерывании
                ev.OnCleanUp(world); 
                _events.RemoveAt(i);
                EventPool.Return(ev);
            }
        }
    }

    private void SortEvents()
    {
        _events.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }
}