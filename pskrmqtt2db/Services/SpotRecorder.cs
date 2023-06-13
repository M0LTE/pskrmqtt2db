using Dapper;
using MaidenheadLib;
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
    private readonly BatchBlock<Spot> spotBatcher;
    private readonly ActionBlock<Spot[]> spotInserter;

    public SpotRecorder(ILogger<SpotRecorder> logger, DbConnectionFactory dbConnectionFactory, IConfiguration config)
    {
        spotInserter = new ActionBlock<Spot[]>(Save);
        spotBatcher = new(config.GetValue<int>("BatchSize"));
        spotBatcher.LinkTo(spotInserter);
        spotBatcher.Completion.ContinueWith(delegate { spotInserter.Complete(); });
        this.logger = logger;
        this.dbConnectionFactory = dbConnectionFactory;
    }

    public async Task QueueForSave(Spot spot)
    {
        await Aggregate(spot);

        await spotBatcher.SendAsync(spot);
    }

    private async Task Aggregate(Spot spot)
    {
        if (spot.Mode != "FT8") return;
        if (spot.SenderGrid == spot.ReceiverGrid) return;
        var distance = MaidenheadLocator.Distance(spot.ReceiverGrid, spot.SenderGrid);

        using var conn = await dbConnectionFactory.GetWriteConnection();
        await conn.ExecuteAsync("INSERT INTO pskr.distances (timestamp, band, grid, distance) VALUES (@timestamp, @band, @grid1, @distance), (@timestamp, @band, @grid2, @distance);", new { timestamp = spot.Received, band = spot.Band, grid1 = spot.SenderGrid, grid2 = spot.ReceiverGrid, distance });

        // for both grids: 
        // band, grid, distance


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
