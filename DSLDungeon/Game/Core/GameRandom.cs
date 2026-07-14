namespace DSLDungeon.Game.Core;

public static class GameRandom
{
    private static readonly Random _instance = new(42);
    
    public static int Next() => _instance.Next();
    public static int Next(int max) => _instance.Next(max);
    public static int Next(int min, int max) => _instance.Next(min, max);
    public static double NextDouble() => _instance.NextDouble();
    public static bool Chance(float probability) => _instance.NextDouble() < probability;
}