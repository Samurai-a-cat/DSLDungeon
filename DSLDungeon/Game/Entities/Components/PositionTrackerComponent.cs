using DSLDungeon.Game.Grid;

namespace DSLDungeon.Game.Entities.Components;

/// <summary>
/// Компонент геометрии: трекает позицию, определяет бэкстаб и преимущество высоты.
/// Только данные/заглушки, логика импульса вынесена в MovementSystem -> CombatStateComponent.
/// </summary>
public class PositionTrackerComponent : EntityComponent
{
    private HexCoords _lastPosition;

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
        _lastPosition = newPosition;
    }
}
