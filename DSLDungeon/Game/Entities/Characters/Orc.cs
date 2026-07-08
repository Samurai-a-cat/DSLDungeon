using DSLDungeon.Game.Core;
using DSLDungeon.Game.Entities.Systems;
using DSLDungeon.Game.Grid;

namespace DSLDungeon.Game.Entities.Characters;

public class Orc : Actor
{
    public Orc(EntityId id, string name, HexCoords position, WorldState world, int maxHp) 
        : base(id, name, position, world)
    {
        InitializeHealth(maxHp);
    }
}