namespace KncWX2Server.Runtime.Center;

using System.Diagnostics;

public sealed partial class RoomUser
{
    private const double DefaultEndPlayDelaySeconds = 15.0;
    private const double CashContinueTimeoutSeconds = 11.0;

    private Stopwatch? _delayPacketTimer;
    private Stopwatch? _cashContinueTimer;
    private double _delayPacketSeconds;

    public double DelayPacketSeconds => _delayPacketSeconds;

    public void ReserveEndPlay()
    {
        EndPlayFlag = true;
        _delayPacketSeconds = DefaultEndPlayDelaySeconds;
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
            return GetCashContinueElapsedSeconds() >= CashContinueTimeoutSeconds;
        }

        return GetDelayPacketElapsedSeconds() >= _delayPacketSeconds;
    }

    public void StopDungeonContinueTime(bool stop)
    {
        if (stop)
        {
            _delayPacketSeconds -= GetDelayPacketElapsedSeconds();
            CashContinueReady = true;
            _cashContinueTimer = Stopwatch.StartNew();
            return;
        }

        CashContinueReady = false;
        RestartDelayPacketTimer();
    }

    private void RestartDelayPacketTimer() => _delayPacketTimer = Stopwatch.StartNew();

    private double GetDelayPacketElapsedSeconds() =>
        _delayPacketTimer?.Elapsed.TotalSeconds ?? 0.0;

    private double GetCashContinueElapsedSeconds() =>
        _cashContinueTimer?.Elapsed.TotalSeconds ?? 0.0;
}
