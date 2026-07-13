using DSLDungeon.Game.Core;
using DSLDungeon.Game.Entities.Components;
using DSLDungeon.Game.Entities.Systems;
using DSLDungeon.Game.Grid;

namespace DSLDungeon.Game.Entities;

public class Entity
{
    public EntityId Id { get; }
    public string Name { get; set; }
    public HexCoords Position { get; set; }

    private readonly Dictionary<Type, EntityComponent> _components = new();

    public Entity(EntityId id, string name, HexCoords position)
    {
        Id = id;
        Name = name;
        Position = position;
    }

    public T GetComponent<T>() where T : EntityComponent
    {
        if  (_components.TryGetValue(typeof(T) , out var component))
        {
            return (T)component;
        }
        return null;
    }

    public bool HasComponent<T>() where T : EntityComponent => _components.ContainsKey(typeof(T));

    public T AddComponent<T>(T component) where T : EntityComponent
    {
        _components[typeof(T)] = component;
        component.OnAttached(this);
        return component;
    }

    public bool RemoveComponent<T>() where T : EntityComponent
    {
        if (_components.Remove(typeof(T), out var comp))
        {
            comp.OnDetached();
            return true;
        }
        return false;
    }

    public void UpdateComponents(float deltaTime)
    {
        foreach (var comp in _components.Values)
        {
            comp.OnUpdate(deltaTime);
        }
    }
}
