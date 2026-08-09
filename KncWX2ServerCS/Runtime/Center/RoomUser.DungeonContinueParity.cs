namespace KncWX2Server.Runtime.Center;

using System.Diagnostics;

public sealed partial class RoomUser
{
    private static readonly TimeSpan DefaultEndPlayDelay = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan CashContinueTimeout = TimeSpan.FromSeconds(11);

    private Stopwatch? _delayPacketTimer;
    private Stopwatch? _cashContinueTimer;
    private TimeSpan _delayPacketTime;

    public double DelayPacketSeconds => _delayPacketTime.TotalSeconds;

    public void ReserveEndPlay()
    {
        EndPlayFlag = true;
        _delayPacketTime = DefaultEndPlayDelay;
        CashContinueReady = false;
        RestartDelayPacketTimer();
    }

    public bool CheckEndPlay()
    {
        if (!EndPlayFlag)
        {
            return false;
        }

        if (CashContinueReady)
        {
            return GetCashContinueElapsed() >= CashContinueTimeout;
        }

        return GetDelayPacketElapsed() >= _delayPacketTime;
    }

    public void StopDungeonContinueTime(bool stop)
    {
        if (stop)
        {
            _delayPacketTime -= GetDelayPacketElapsed();
            CashContinueReady = true;
            _cashContinueTimer = Stopwatch.StartNew();
            return;
        }

        CashContinueReady = false;
        RestartDelayPacketTimer();
    }

    private void RestartDelayPacketTimer() => _delayPacketTimer = Stopwatch.StartNew();

    private TimeSpan GetDelayPacketElapsed() => _delayPacketTimer?.Elapsed ?? TimeSpan.Zero;

    private TimeSpan GetCashContinueElapsed() => _cashContinueTimer?.Elapsed ?? TimeSpan.Zero;
}