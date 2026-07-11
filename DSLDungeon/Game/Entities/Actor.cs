using DSLDungeon.Game.Core;
using DSLDungeon.Game.Core.Actions;
using DSLDungeon.Game.Grid;

namespace DSLDungeon.Game.Entities;

public class Actor : Entity
{
    public EventQueue Queue { get; } = new();

    public Actor(EntityId id, string name, HexCoords position) 
        : base(id, name, position)
    {
    }
}
