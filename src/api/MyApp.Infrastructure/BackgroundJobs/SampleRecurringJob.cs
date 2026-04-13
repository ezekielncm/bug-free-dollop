using Hangfire;
using Microsoft.Extensions.Logging;

namespace MyApp.Infrastructure.BackgroundJobs;

/// <summary>
/// Sample recurring background job - extend with your own jobs.
/// </summary>
public class SampleRecurringJob(ILogger<SampleRecurringJob> logger)
{
    [AutomaticRetry(Attempts = 3)]
    public Task ExecuteAsync()
    {
        logger.LogInformation("SampleRecurringJob executed at {Time}", DateTime.UtcNow);
        return Task.CompletedTask;
    }
}
