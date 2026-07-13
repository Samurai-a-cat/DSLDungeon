namespace DSLDungeon.Game.Core.Actions;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class PoolConfigAttribute : Attribute
{
    public int PreloadCount { get; }

    public PoolConfigAttribute(int preloadCount = 0)
    {
        if (preloadCount < 0)
            throw new ArgumentException("PreloadCount не может быть отрицательным.", nameof(preloadCount));

        PreloadCount = preloadCount;
    }
}
