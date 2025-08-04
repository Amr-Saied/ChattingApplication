using ChattingApplicationProject.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChattingApplicationProject.Services
{
    public class BackgroundCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BackgroundCleanupService> _logger;
        private readonly TimeSpan _period = TimeSpan.FromHours(24); // Run every 24 hours

        public BackgroundCleanupService(
            IServiceProvider serviceProvider,
            ILogger<BackgroundCleanupService> logger
        )
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PerformCleanupAsync();
                    _logger.LogInformation("Background cleanup completed successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during background cleanup");
                }

                // Wait for the next cleanup cycle
                await Task.Delay(_period, stoppingToken);
            }
        }

        private async Task PerformCleanupAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var cleanupService = scope.ServiceProvider.GetRequiredService<IUserCleanupService>();

            var deletedUsers = await cleanupService.CleanupExpiredUnconfirmedUsersAsync();
            var clearedTokens = await cleanupService.CleanupExpiredPasswordResetTokensAsync();

            if (deletedUsers > 0 || clearedTokens > 0)
            {
                _logger.LogInformation(
                    $"Cleanup completed: {deletedUsers} users deleted, {clearedTokens} tokens cleared"
                );
            }
        }
    }
}
