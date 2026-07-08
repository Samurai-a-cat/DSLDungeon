using DSLDungeon.Game.Core;
using DSLDungeon.Game.Core.Actions;
using DSLDungeon.Game.Entities.Items;
using DSLDungeon.Game.Grid;

namespace DSLDungeon.Game.Entities;

public abstract class Actor : Entity
{
    public EventQueue Queue { get; }
    
    public InventorySystem? Inventory { get; protected set; }
    
    protected Actor(EntityId id, string name, HexCoords position, WorldState world) 
        : base(id, name, position)
    {
        Queue = new EventQueue();
        Inventory = new InventorySystem(world);
    }
}