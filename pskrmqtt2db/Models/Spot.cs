using MaidenheadLib;
using System.Text.Json.Serialization;

namespace pskrmqtt2db.Models;

public class Spot
{
    [JsonPropertyName("sq")]
    public ulong Seq { get; set; }

    [JsonPropertyName("t")]
    public DateTime Received { get; set; }

    [JsonPropertyName("b")]
    public string? Band { get; set; }

    [JsonPropertyName("f")]
    public ulong? Frequency { get; set; }

    [JsonPropertyName("md")]
    public string? Mode { get; set; }

    [JsonPropertyName("sc")]
    public string? SenderCall { get; set; }

    [JsonPropertyName("rc")]
    public string? ReceiverCall { get; set; }

    [JsonPropertyName("sl")]
    public string? SenderGrid { get; set; }

    [JsonPropertyName("rl")]
    public string? ReceiverGrid { get; set; }

    [JsonPropertyName("sf")]
    public string? SenderGridFull { get; set; }

    [JsonPropertyName("rf")]
    public string? ReceiverGridFull { get; set; }

    [JsonPropertyName("se")]
    public int? SenderEntity { get; set; }

    [JsonPropertyName("re")]
    public int? ReceiverEntity { get; set; }

    [JsonPropertyName("rp")]
    public int? Report { get; set; }

    [JsonPropertyName("d")]
    public int? Distance
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
                return (int)distance;
            }
            catch (Exception)
            {
                try
                {
                    var distance = MaidenheadLocator.Distance(ReceiverGrid, SenderGrid);
                    return (int)distance;
                }
                catch (Exception)
                {
                    return default;
                }
            }
        }
    }
}
