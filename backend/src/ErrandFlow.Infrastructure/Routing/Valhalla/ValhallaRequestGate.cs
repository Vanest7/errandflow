using System.Diagnostics;
using ErrandFlow.Application.Planning.Exceptions;

namespace ErrandFlow.Infrastructure.Routing.Valhalla;

public sealed class ValhallaRequestGate : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private long? _lastRequestStartedAt;

    public async Task EnterAsync(CancellationToken cancellationToken)
    {
        if (!await _semaphore.WaitAsync(0, cancellationToken))
        {
            throw new TravelGraphUnavailableException();
        }

        try
        {
            if (_lastRequestStartedAt is long lastStartedAt)
            {
                var remaining = TimeSpan.FromSeconds(1)
                    - Stopwatch.GetElapsedTime(lastStartedAt);

                if (remaining > TimeSpan.Zero)
                {
                    await Task.Delay(remaining, cancellationToken);
                }
            }

            _lastRequestStartedAt = Stopwatch.GetTimestamp();
        }
        catch
        {
            _semaphore.Release();
            throw;
        }
    }

    public void Exit()
    {
        _semaphore.Release();
    }

    public void Dispose()
    {
        _semaphore.Dispose();
    }
}