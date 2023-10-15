using pskrmqtt2db.Models;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks.Dataflow;

namespace pskrmqtt2db.Services;

public interface ISpotRecorder
{
    Task QueueForSave(Spot spot);
}

internal class SpotRecorder : ISpotRecorder
{
    private readonly ILogger<SpotRecorder> logger;
    private readonly DbConnectionFactory dbConnectionFactory;
    private readonly BatchBlock<Spot> spotBatcher;
    private readonly ActionBlock<Spot[]> spotInserter;

    private int batchSize;

    public SpotRecorder(ILogger<SpotRecorder> logger, DbConnectionFactory dbConnectionFactory, IConfiguration config)
    {
        batchSize = config.GetValue<int>("BatchSize");
        spotInserter = new ActionBlock<Spot[]>(Save);
        spotBatcher = new(batchSize);
        spotBatcher.LinkTo(spotInserter);
        spotBatcher.Completion.ContinueWith(delegate { spotInserter.Complete(); });
        this.logger = logger;
        this.dbConnectionFactory = dbConnectionFactory;
    }

    public async Task QueueForSave(Spot spot)
    {
        var t1 = spotBatcher.SendAsync(spot);
        Write(spot);
        if (!await t1)
        {
            logger.LogWarning("Throwing away data...");
        }

        if (spotInserter.InputCount > batchSize)
        {
            logger.LogWarning("Falling behind...");
        }
    }

    const string rootPath = "/data/pskr";
    private static bool notExist = false;

    private static void Write(Spot spot)
    {
        if (notExist) return;

        if (!Directory.Exists(rootPath))
        {
            notExist = true;
            return;
        }

        if (spot == null || string.IsNullOrWhiteSpace(spot.Band) || string.IsNullOrWhiteSpace(spot.Mode))
        {
            return;
        }

        var path = Path.Combine(rootPath, $"{spot.Received.Year}-{spot.Received.Month:00}", spot.Received.Day.ToString("00"), spot.Band);

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        var spotJson = JsonSerializer.Serialize(spot, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        });

        if (Debugger.IsAttached && (spotJson.Contains('\r') || spotJson.Contains('\n')))
        {
            Debugger.Break();
        }

        File.AppendAllText(Path.Combine(path, spot.Mode) + ".jsonl", spotJson + "\n");
    }

    private async Task Save(Spot[] spots)
    {
        using var conn = await dbConnectionFactory.GetWriteConnection();

        try
        {
            var sw = Stopwatch.StartNew();
            await conn.BulkInsert(
                "pskr.spots",
                new[] { "seq", "senderCall", "receiverCall", "senderGrid", "receiverGrid", "senderGridFull", "receiverGridFull", "senderEntity", "receiverEntity", "received", "band", "frequency", "mode", "report", "distance" },
                spots);
            sw.Stop();
            logger.LogInformation($"Saved {spots.Length} spots in {sw.ElapsedMilliseconds:0}ms");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Caught while inserting to DB");
        }
    }
}
