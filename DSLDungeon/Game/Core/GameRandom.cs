namespace DSLDungeon.Game.Core;

/// <summary>
/// Глобальный PRNG для игровой логики.
/// Использует Random.Shared (thread-safe, Zero-Allocation в .NET 6+).
/// </summary>
public static class GameRandom
{
    /// <summary>
    /// Точка входа для инициализации из GameService.
    /// На данном этапе ничего не делает — Random.Shared не требует сида.
    /// Оставлен для единообразия DI-жизненного цикла.
    /// </summary>
    public static void Initialize() { }

    public static int Next() => Random.Shared.Next();
    public static int Next(int max) => Random.Shared.Next(max);
    public static int Next(int min, int max) => Random.Shared.Next(min, max);
    public static double NextDouble() => Random.Shared.NextDouble();
    public static float NextFloat() => (float)Random.Shared.NextDouble();
    public static float NextFloat(float min, float max) => min + NextFloat() * (max - min);
    public static bool Chance(float probability) => NextDouble() < probability;

    /// <summary><br/>
    /// Генерирует значение вокруг <paramref name="value"/> с разбросом ±<paramref name="offset"/>.<br/>
    /// Форма распределения задаётся параметром <paramref name="shape"/> в диапазоне [-1, 1]:<br/>
    /// <br/>
    ///   -1  → U-образное (крайние значения наиболее вероятны, центр — редок)<br/>
    ///    0  → Uniform (все значения равновероятны)<br/>
    ///   +1  → Gaussian (колокол, среднее значение наиболее вероятно)<br/>
    /// <br/>
    /// Промежуточные значения дают плавную интерполяцию между режимами.<br/>
    /// Результат всегда жёстко клампится в [value - offset, value + offset].<br/>
    /// </summary><br/>
    /// <param name="value">Центр распределения (базовый урон).</param><br/>
    /// <param name="offset">Половина ширины диапазона (разброс).</param><br/>
    /// <param name="shape">Форма: [-1..1]. По умолчанию 1 (гаусс).</param><br/>
    public static float GausRandom(float value, float offset, float shape)
    {
        if (offset <= 0f) return value;
        shape = Math.Clamp(shape, -1f, 1f);

        // Базовый uniform в [-1, 1] — основа для всех режимов
        double uniformSample = Random.Shared.NextDouble() * 2.0 - 1.0;

        double sample;

        if (shape > 0f)
        {
            // Гаусс через Box-Muller (настоящее нормальное распределение)
            double u1 = Random.Shared.NextDouble();
            double u2 = Random.Shared.NextDouble();
            double gaussian = Math.Sqrt(-2.0 * Math.Log(1.0 - u1)) 
                            * Math.Sin(2.0 * Math.PI * u2);
            // stddev=1 → нормализуем в [-1, 1]: ~99.7% попаданий (3σ)
            gaussian = Math.Clamp(gaussian / 3.0, -1.0, 1.0);

            // Плавная интерполяция: shape=0 → uniform, shape=1 → gaussian
            sample = (1.0 - shape) * uniformSample + shape * gaussian;
        }
        else if (shape < 0f)
        {
            // U-образное через power-трансформацию.
            // Для |u| ∈ [0,1] и α<1: |u|^α концентрирует значения к 1 (к краям [-1, 1]).
            double absU = Math.Abs(uniformSample);
            double signU = uniformSample >= 0 ? 1.0 : -1.0;
            // shape=0 → exponent=1.0 (uniform), shape=-1 → exponent=0.3 (сильное U)
            double exponent = 1.0 - Math.Abs(shape) * 0.7;
            sample = signU * Math.Pow(absU, exponent);
        }
        else
        {
            // Чистый uniform
            sample = uniformSample;
        }

        double final = value + sample * offset;
        return (float)Math.Clamp(final, value - offset, value + offset);
    }
    
        
    /// <summary>
    /// Перегрузка с дефолтным гауссом (shape = 1).
    /// </summary>
    public static float GausRandom(float value, float offset) 
        => GausRandom(value, offset, 1f);
}