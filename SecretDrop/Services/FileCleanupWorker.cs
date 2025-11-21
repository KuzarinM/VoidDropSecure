using Microsoft.Extensions.Options;
using SecretDrop.Configurations;

namespace SecretDrop.Services
{
    public class FileCleanupWorker : BackgroundService
    {
        private readonly ISecretFileStore _store;
        private readonly AppOptions _options;
        private readonly ILogger<FileCleanupWorker> _logger;

        public FileCleanupWorker(ISecretFileStore store, IOptions<AppOptions> options, ILogger<FileCleanupWorker> logger)
        {
            _store = store;
            _options = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🧹 Cleanup Service started. Interval: {min} min", _options.CleanupIntervalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Ждем N минут
                    await Task.Delay(TimeSpan.FromMinutes(_options.CleanupIntervalMinutes), stoppingToken);

                    _logger.LogInformation("🧹 Running cleanup...");
                    int count = _store.DeleteExpired();
                    if (count > 0) _logger.LogInformation("🔥 Burned {count} expired files.", count);
                }
                catch (TaskCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during cleanup");
                }
            }
        }
    }
}
