using System;

namespace DSLDungeon.Game.Core.Actions;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class PoolConfigAttribute : Attribute
{
    /// <summary>
    /// Количество объектов, создаваемых в пуле на старте (прогрев).
    /// Помогает избежать аллокаций и микро-фризов в первые секунды игры.
    /// </summary>
    public int PreloadCount { get; }

    public PoolConfigAttribute(int preloadCount = 0)
    {
        if (preloadCount < 0)
            throw new ArgumentException("PreloadCount не может быть отрицательным.", nameof(preloadCount));
        
        PreloadCount = preloadCount;
    }
}