namespace pskrmqtt2db.Models;

public class Spot
{
    public required ulong Seq { get; set; }
    public required DateTime Received { get; set; }
    public required string Band { get; set; }
    public required string Mode { get; set; }
    public required string SenderCall { get; set; }
    public required string ReceiverCall { get; set; }
    public required string SenderGrid { get; set; }
    public required string ReceiverGrid { get; set; }
    public required int SenderEntity { get; set; }
    public required int ReceiverEntity { get; set; }
}
