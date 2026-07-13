using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DSLDungeon.Game.Core.Time;

public sealed class TimeSystem
{
    private long _lastTimestamp;
    private double _maxDeltaBarrier = 0.1;
    private readonly double _ticksToSeconds = 1.0 / Stopwatch.Frequency;

    private readonly TimeChannel[] _channels = new TimeChannel[16];
    private int _channelCount;

    public double MaxDeltaBarrier
    {
        get => _maxDeltaBarrier;
        set => _maxDeltaBarrier = value;
    }

    public TimeSystem()
    {
        Initialize();
    }

    public void Initialize()
    {
        _lastTimestamp = Stopwatch.GetTimestamp();
    }

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update()
    {
        long currentTimestamp = Stopwatch.GetTimestamp();
        long elapsedTicks = currentTimestamp - _lastTimestamp;
        _lastTimestamp = currentTimestamp;

        double rawDelta = elapsedTicks * _ticksToSeconds;

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
