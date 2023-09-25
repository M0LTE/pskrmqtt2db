using MaidenheadLib;

namespace pskrmqtt2db.Models;

public class Spot
{
    public ulong Seq { get; set; }
    public DateTime Received { get; set; }
    public string? Band { get; set; }
    public ulong? Frequency { get; set; }
    public string? Mode { get; set; }
    public string? SenderCall { get; set; }
    public string? ReceiverCall { get; set; }
    public string? SenderGrid { get; set; }
    public string? ReceiverGrid { get; set; }
    public string? SenderGridFull { get; set; }
    public string? ReceiverGridFull { get; set; }
    public int? SenderEntity { get; set; }
    public int? ReceiverEntity { get; set; }
    public int? Report { get; set; }

    public double? Distance
    {
        get
        {
            if (ReceiverGridFull == null || SenderGridFull == null)
            {
                return default;
            }

            try
            {
                var distance = MaidenheadLocator.Distance(ReceiverGridFull, SenderGridFull);
                return distance;
            }
            catch (Exception)
            {
                try
                {
                    var distance = MaidenheadLocator.Distance(ReceiverGrid, SenderGrid);
                    return distance;
                }
                catch (Exception)
                {
                    return default;
                }
            }
        }
    }
}
