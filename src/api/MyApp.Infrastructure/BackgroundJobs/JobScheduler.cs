using Hangfire;

namespace MyApp.Infrastructure.BackgroundJobs;

public static class JobScheduler
{
    public static void ConfigureRecurringJobs()
    {
        RecurringJob.AddOrUpdate<SampleRecurringJob>(
            "sample-recurring-job",
            job => job.ExecuteAsync(),
            Cron.Minutely);
    }
}
