using DSLDungeon.Game.Grid;

namespace DSLDungeon.Game.Entities.Components;

public class PositionTrackerComponent : EntityComponent
{
    public bool HasHeightAdvantageOver(Entity target)
    {
        return false;
    }

    public bool IsBackstab(Entity target, Entity attacker)
    {
        return false;
    }

    public void OnMoved(HexCoords newPosition)
    {
    }
}
