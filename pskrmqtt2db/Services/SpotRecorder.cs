using pskrmqtt2db.Models;
using System.Diagnostics;
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
    private readonly BatchBlock<Spot> spotBatcher = new(1000);
    private ActionBlock<Spot[]> spotInserter;

    public SpotRecorder(ILogger<SpotRecorder> logger, DbConnectionFactory dbConnectionFactory)
    {
        spotInserter = new ActionBlock<Spot[]>(Save);
        spotBatcher.LinkTo(spotInserter);
        spotBatcher.Completion.ContinueWith(delegate { spotInserter.Complete(); });
        this.logger = logger;
        this.dbConnectionFactory = dbConnectionFactory;
    }

    public async Task QueueForSave(Spot spot)
    {
        if (!await spotBatcher.SendAsync(spot))
        {
            Debugger.Break();
        }
    }

    private async Task Save(Spot[] spots)
    {
        using var conn = await dbConnectionFactory.GetWriteConnection();

        try
        {
            var sw = Stopwatch.StartNew();
            await conn.BulkInsert(
                "pskr.spots",
                new[] { "seq", "senderCall", "receiverCall", "senderGrid", "receiverGrid", "senderEntity", "receiverEntity", "received", "band", "mode" },
                spots);
            sw.Stop();
            logger.LogInformation($"Saved {spots.Length} spots in {sw.ElapsedMilliseconds:0}ms");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Caught while inserting to DB");
        }

        /*await conn.ExecuteAsync("INSERT INTO pskr.spots (seq, senderCall, receiverCall, senderGrid, receiverGrid, senderEntity, receiverEntity, received, band, mode) " +
            "VALUES (@seq, @senderCall, @receiverCall, @senderGrid, @receiverGrid, @senderEntity, @receiverEntity, @received, @band, @mode);", spots);*/
    }
}
