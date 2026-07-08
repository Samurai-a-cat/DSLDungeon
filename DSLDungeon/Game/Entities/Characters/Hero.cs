using DSLDungeon.Game.Core;
using DSLDungeon.Game.Grid;

namespace DSLDungeon.Game.Entities.Characters;

public class Hero : Actor
{
    public Hero(EntityId id, string name, HexCoords position, WorldState world, int maxHp) 
        : base(id, name, position, world)
    {
        InitializeHealth(maxHp);
    }
}