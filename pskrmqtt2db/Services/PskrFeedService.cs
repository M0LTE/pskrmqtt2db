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
                        .WithClientId("pskrmqtt2db")
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

            var n = JsonNode.Parse(s);
            var t = n!["t"]!.GetValue<ulong>();
            var sq = n!["sq"]!.GetValue<ulong>();

            var received = DateTime.UnixEpoch.AddSeconds(t);

            var parts = arg.ApplicationMessage.Topic.Split('/');

            if (parts.Length != 11)
            {
                Debugger.Break();
            }

            if (parts.Any(p => p.Length == 0))
            {
                Debugger.Break();
            }

            var spot = new Models.Spot
            {
                Seq = sq,
                Received = received,
                Band = Normalise(parts[3]),
                Mode = Normalise(parts[4]),
                SenderCall = Normalise(parts[5]).Replace(".", "/"),
                ReceiverCall = Normalise(parts[6]).Replace(".", "/"),
                SenderGrid = Normalise(parts[7]),
                ReceiverGrid = Normalise(parts[8]),
                SenderEntity = int.Parse(parts[9]),
                ReceiverEntity = int.Parse(parts[10])
            };

            await spotRecorder.QueueForSave(spot);
        }

        private static string Normalise(string v) => v.Trim().ToUpperInvariant();

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await mqttClient.StopAsync();
        }
    }
}
