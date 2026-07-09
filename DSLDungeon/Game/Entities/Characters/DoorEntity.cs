using DSLDungeon.Game.Core;
using DSLDungeon.Game.Grid;

namespace DSLDungeon.Game.Entities.Characters;

public class DoorEntity : Entity
{
    public DoorEntity(EntityId id, string name, HexCoords position, int maxHp) : base(id, name, position)
    {
        InitializeHealth(maxHp);
    }
}