using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Server;
using System.Diagnostics;
using System.Text.Json.Nodes;

namespace pskrmqtt2db.Services
{
    internal class PskrFeedService : IHostedService
    {
        private readonly ILogger<PskrFeedService> logger;
        private readonly ISpotRecorder spotRecorder;
        private readonly IManagedMqttClient mqttClient;

        public PskrFeedService(ILogger<PskrFeedService> logger, ISpotRecorder spotRecorder)
        {
            this.logger = logger;
            this.spotRecorder = spotRecorder;
            mqttClient = new MqttFactory().CreateManagedMqttClient();
            mqttClient.ApplicationMessageReceivedAsync += MqttClient_ApplicationMessageReceivedAsync;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var options = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(1))
                .WithClientOptions(
                    new MqttClientOptionsBuilder()
                        .WithClientId("pskrmqtt2db-" + Guid.NewGuid())
                        .WithTcpServer("mqtt.pskreporter.info")
                        .Build())
                .Build();
            await mqttClient.SubscribeAsync("pskr/filter/v2/#");
            await mqttClient.StartAsync(options);
        }

        private async Task MqttClient_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
        {
            //logger.LogInformation(arg.ApplicationMessage.Topic);
            // pskr/filter/v2/15m/FT8/JP7TAW/PA2JCB/QM09/JO21/339/263

            var s = arg.ApplicationMessage.ConvertPayloadToString();

            // {"sq":37386122735,"f":28076105,"md":"FT8","rp":-18,"t":1686662651,"sc":"AA1K","sl":"FM29GA","rc":"K0XMG","rl":"EM17GR","sa":291,"ra":291,"b":"10m"}
            // {"sq":39732280668,"f":10136719,"md":"FT8","rp":3,"t":1695669839,"sc":"IU2BYL","sl":"JN45PI","rc":"SP7IWA","rl":"KO02jd58","sa":248,"ra":269,"b":"30m"}

            var node = JsonNode.Parse(s);
            var time = node!["t"]!.GetValue<ulong>();
            var sequence = node!["sq"]!.GetValue<ulong>();
            var report = node!["rp"]?.GetValue<int?>();
            var senderLocator = node!["sl"]?.GetValue<string?>();
            var receiverLocator = node!["rl"]?.GetValue<string?>();
            var frequency = node!["f"]?.GetValue<ulong?>();

            var topicParts = arg.ApplicationMessage.Topic.Split('/');

            if (topicParts.Length != 11)
            {
                return;
            }

            var spot = new Models.Spot
            {
                Seq = sequence,
                Received = DateTime.UnixEpoch.AddSeconds(time),
                Band = topicParts[3],
                Mode = Normalise(topicParts[4]),
                SenderCall = Normalise(topicParts[5])?.Replace(".", "/"),
                ReceiverCall = Normalise(topicParts[6])?.Replace(".", "/"),
                SenderGridFull = Normalise(senderLocator),
                SenderGrid = Normalise(topicParts[7]),
                ReceiverGridFull = Normalise(receiverLocator),
                ReceiverGrid = Normalise(topicParts[8]),
                SenderEntity = int.Parse(topicParts[9]),
                ReceiverEntity = int.Parse(topicParts[10]),
                Report = report,
                Frequency = frequency
            };

            await spotRecorder.QueueForSave(spot);
        }

        private static string? Normalise(string? v) => v?.Trim()?.ToUpperInvariant();

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await mqttClient.StopAsync();
        }
    }
}
