using pskrmqtt2db.Models;
using pskrmqtt2db.Services;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddHostedService<PskrFeedService>();
        services.AddHostedService<DbPurgeService>();
        services.AddSingleton<ISpotRecorder, SpotRecorder>();
        services.Configure<DbConfig>(context.Configuration.GetSection("Database"));
        services.AddSingleton<DbConnectionFactory>();
    })
    .ConfigureLogging(builder => {
        builder.AddSimpleConsole(options =>
        {
            options.IncludeScopes = false;
            options.UseUtcTimestamp = true;
            options.SingleLine = true;
            options.TimestampFormat = "HH:mm:ss ";
        });
    })
    .Build();

host.Run();
