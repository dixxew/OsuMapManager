using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MapManager.GUI.Services;

/// Blocks AppInitializationService until the setup wizard completes (or is skipped).
/// Pre-signalled on normal launches where SetupCompleted is already true.
public class AppStartupGate
{
    private readonly TaskCompletionSource _tcs = new();
    private readonly ILogger<AppStartupGate> _logger;

    public AppStartupGate(AppSettings appSettings, ILogger<AppStartupGate> logger)
    {
        _logger = logger;
        if (appSettings.SetupCompleted)
        {
            _logger.LogInformation("Setup already completed — startup gate pre-signalled");
            _tcs.TrySetResult();
        }
        else
        {
            _logger.LogInformation("Setup not completed — startup gate waiting for wizard");
        }
    }

    public Task WhenReady => _tcs.Task;

    public void SignalReady()
    {
        _logger.LogInformation("Startup gate signalled ready");
        _tcs.TrySetResult();
    }
}
