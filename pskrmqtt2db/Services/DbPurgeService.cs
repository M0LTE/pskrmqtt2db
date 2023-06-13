using Dapper;

namespace pskrmqtt2db.Services;

internal class DbPurgeService : IHostedService, IDisposable
{
    private readonly ILogger<DbPurgeService> logger;
    private readonly DbConnectionFactory dbConnectionFactory;
    private Timer? _timer = null;

    public DbPurgeService(ILogger<DbPurgeService> logger, DbConnectionFactory dbConnectionFactory)
    {
        this.logger = logger;
        this.dbConnectionFactory = dbConnectionFactory;
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(60));

        return Task.CompletedTask;
    }

    private async void DoWork(object? state)
    {
        return;

        using var conn = await dbConnectionFactory.GetWriteConnection();

        var rows = await conn.ExecuteAsync("DELETE FROM pskr.spots WHERE received < @oneWeekAgo;", new { oneWeekAgo = DateTime.UtcNow.AddMinutes(-5) });

        logger.LogInformation("Deleted {rows} aged rows", rows);
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
