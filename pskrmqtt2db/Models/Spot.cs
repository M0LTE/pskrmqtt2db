namespace pskrmqtt2db.Models;

public class Spot
{
    public ulong Seq { get; set; }
    public DateTime Received { get; set; }
    public string Band { get; set; }
    public string Mode { get; set; }
    public string SenderCall { get; set; }
    public string ReceiverCall { get; set; }
    public string SenderGrid { get; set; }
    public string ReceiverGrid { get; set; }
    public int SenderEntity { get; set; }
    public int ReceiverEntity { get; set; }
}
