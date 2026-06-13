using System.Threading.Tasks;

namespace MapManager.GUI.Services;

/// Blocks AppInitializationService until the setup wizard completes (or is skipped).
/// Pre-signalled on normal launches where SetupCompleted is already true.
public class AppStartupGate
{
    private readonly TaskCompletionSource _tcs = new();

    public AppStartupGate(AppSettings appSettings)
    {
        if (appSettings.SetupCompleted)
            _tcs.TrySetResult();
    }

    public Task WhenReady => _tcs.Task;

    public void SignalReady() => _tcs.TrySetResult();
}
