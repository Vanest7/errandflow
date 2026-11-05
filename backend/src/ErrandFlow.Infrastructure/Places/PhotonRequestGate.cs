using ErrandFlow.Application.Places.Exceptions;

namespace ErrandFlow.Infrastructure.Places;

// Shared by all typed clients in this process. Public Photon has no guaranteed quota.
public sealed class PhotonRequestGate : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private DateTimeOffset _nextRequestAt = DateTimeOffset.MinValue;

    public async Task EnterAsync(CancellationToken cancellationToken)
    {
        if (!await _semaphore.WaitAsync(0, cancellationToken))
        {
            throw new PlaceSearchUnavailableException();
        }

        try
        {
            var delay = _nextRequestAt - DateTimeOffset.UtcNow;
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, cancellationToken);
            }
            _nextRequestAt = DateTimeOffset.UtcNow.AddSeconds(1);
        }
        catch
        {
            _semaphore.Release();
            throw;
        }
    }

    public void Exit() => _semaphore.Release();
    public void Dispose() => _semaphore.Dispose();
}
