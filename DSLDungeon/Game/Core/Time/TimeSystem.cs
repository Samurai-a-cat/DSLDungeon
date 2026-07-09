using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DSLDungeon.Game.Core.Time;

/// <summary>
/// Система управления игровым временем. Контролирует каналы и рассчитывает глобальный DeltaTime.
/// </summary>
public sealed class TimeSystem
{
    private long _lastTimestamp;
    private double _maxDeltaBarrier = 0.1; // По умолчанию 100 мс
    private readonly double _ticksToSeconds = 1.0 / Stopwatch.Frequency;

    // Ограничиваем количество каналов фиксированным массивом для избежания аллокаций
    private readonly TimeChannel[] _channels = new TimeChannel[16];
    private int _channelCount;

    /// <summary>
    /// Максимальный порог для одного кадра (в секундах). 
    /// Предотвращает скачки физики при потере фокуса вкладки браузера.
    /// </summary>
    public double MaxDeltaBarrier
    {
        get => _maxDeltaBarrier;
        set => _maxDeltaBarrier = value;
    }

    public TimeSystem()
    {
        // Первичная фиксация времени при создании системы
        Initialize();
    }

    /// <summary>
    /// Инициализирует или сбрасывает точку отсчета времени.
    /// </summary>
    public void Initialize()
    {
        _lastTimestamp = Stopwatch.GetTimestamp();
    }

    /// <summary>
    /// Создает и регистрирует новый канал времени.
    /// </summary>
    public TimeChannel CreateChannel()
    {
        if (_channelCount >= _channels.Length)
        {
            throw new InvalidOperationException("Достигнут лимит каналов времени для этой сессии.");
        }

        var channel = new TimeChannel();
        _channels[_channelCount++] = channel;
        return channel;
    }

    /// <summary>
    /// Обновляет состояние всех зарегистрированных каналов времени.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update()
    {
        long currentTimestamp = Stopwatch.GetTimestamp();
        long elapsedTicks = currentTimestamp - _lastTimestamp;
        _lastTimestamp = currentTimestamp;

        double rawDelta = elapsedTicks * _ticksToSeconds;

        // Защита от больших скачков времени
        if (rawDelta > _maxDeltaBarrier)
        {
            rawDelta = _maxDeltaBarrier;
        }

        float rawDeltaFloat = (float)rawDelta;

        int count = _channelCount;
        TimeChannel[] channels = _channels;
        for (int i = 0; i < count; i++)
        {
            channels[i].Update(rawDeltaFloat);
        }
    }
}